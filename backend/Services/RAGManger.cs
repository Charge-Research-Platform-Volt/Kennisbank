

using System.Text.Json;
using HandlebarsDotNet;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using OpenAI.Chat;
using Serilog;
using static Qdrant.Client.Grpc.Conditions;
using Azure.AI.DocumentIntelligence;
using Azure;


namespace KnowledgeBank.Services;

public class RAGManger
{
    private readonly Serilog.ILogger _logger;
    private readonly RAGSystem _ragSystem;
    private readonly ResourceManager _resourceManager;
    private readonly IAzureBlobService _blobService;


    public RAGManger(ResourceManager resourceManager, RAGSystem ragSystem, IAzureBlobService blobService)
    {
        _logger = Log.ForContext<RAGManger>();
        _ragSystem = ragSystem;
        _resourceManager = resourceManager;
        _blobService = blobService;
    }



    /// <summary>
    /// Processes a document through the RAG (Retrieval-Augmented Generation) pipeline.
    /// This method orchestrates the complete document processing workflow from text extraction
    /// to vector storage and AI tag generation.
    /// </summary>
    /// <param name="id">The unique identifier for the resource being processed.</param>
    /// <param name="fileType">The type of file being processed. Currently unused but reserved for future implementations.</param>
    /// <param name="chunk">An initial chunk of text (typically metadata) to include as the first chunk in processing.</param>
    /// <param name="fileStream">A stream representing the file to be processed. If null, only the initial chunk will be processed.</param>
    /// <returns>A task representing the asynchronous operation of processing the document.</returns>
    /// <remarks>
    /// The RAG pipeline consists of the following sequential steps:
    /// 1. Text Extraction: Extract text from the document using Azure Document Intelligence with markdown formatting
    /// 2. Text Chunking: Split the extracted text into smaller, manageable segments for embedding generation
    /// 3. Vector Embedding Generation: Create vector embeddings for each text chunk using the configured embedding model
    /// 4. Vector Storage: Store the chunks and their corresponding embeddings in the Qdrant vector database
    /// 5. AI Tag Generation: Generate contextual tags for the document using AI analysis of the processed chunks
    /// 
    /// If no text is extracted from the document (empty or corrupted file), the process will terminate early
    /// to prevent unnecessary processing. The method uses Azure Document Intelligence's prebuilt-layout model
    /// with markdown output format for optimal text structure preservation.
    /// </remarks>
    public async Task MainPipline(Guid id, string chunk, string? fileType = null, Stream? fileStream = null)
    {
        _logger.Information("Main RAG pipeline started for resource ID: {Id}", id);

        try
        {
            // Initialize chunks collection with the provided metadata chunk
            List<string> chunks = [$"{chunk}",];

            // STEP 1: Document Text Extraction
            // Extract text from the document using Azure Document Intelligence service
            _logger.Information("Extracting text from the document for resource ID: {Id}", id);

            if (fileStream != null)
            {
                // Configure Azure Document Intelligence options
                AnalyzeDocumentOptions options = new AnalyzeDocumentOptions(
                    modelId: "prebuilt-layout",
                    bytesSource: BinaryData.FromStream(fileStream))
                {
                    OutputContentFormat = DocumentContentFormat.Markdown
                };

                // Analyze the document and wait for completion
                Operation<AnalyzeResult> operation = await _ragSystem.DocumentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, options);

                string extractedText = operation.Value.Content;

                // Validate that text extraction was successful
                if (string.IsNullOrEmpty(extractedText))
                {
                    _logger.Warning("No text extracted from the document");
                    return;
                }

                // STEP 2: Text Chunking
                // Split the extracted text into smaller chunks suitable for embedding generation
                chunks.AddRange(_ragSystem.Toolbox.SplitTextIntoChunks(extractedText, false, markdownSplit: true));

                _logger.Information("Successfully extracted and chunked text into {ChunkCount} segments for resource ID: {Id}", chunks.Count, id);
            }


            // STEP 3 & 4: Vector Embedding Generation and Storage
            // Generate embeddings for each chunk and store them in the vector database
            _logger.Information("Generating vector embeddings and storing {ChunkCount} chunks for resource ID: {Id}", chunks.Count, id);
            await _ragSystem.CreatePoints(id, chunks);


            // STEP 5: AI Tag Generation
            // Generate contextual tags based on the processed document content
            _logger.Information("Initiating AI tag generation for resource ID: {Id}", id);
            await GenerateTagsAsync(id.ToString());

            _logger.Information("RAG pipeline completed successfully for resource ID: {Id}", id);
        }
        catch (Exception ex)
        {
            // Todo: Add a way to notify the user that the pipeline failed, with some options to retry.

            _logger.Error(ex, "An error occurred while processing the document for resource ID: {Id}. Pipeline execution failed.", id);
            throw;
        }

        //---------------------------------------------------------------------------------------------------
        // LEGACY CODE: Alternative text extraction using PdfPig library
        // Note: The following code is commented out as it uses PdfPig library which is not currently in use.
        // This approach was replaced by Azure Document Intelligence for better accuracy and format support.
        // Keeping this code for reference in case we need to fall back to PdfPig or support additional formats.
        //---------------------------------------------------------------------------------------------------
        // 
        // -- Extract text from the document using PdfPig library (DEPRECATED)
        // if (!string.IsNullOrWhiteSpace(fileType))
        // {
        //     string extractedText = await _ragSystem.Toolbox.ExtractTextAsync(fileType, id, _blobService);
        //
        //     if (string.IsNullOrEmpty(extractedText))
        //     {
        //         _logger.Warning("No text extracted from the document");
        //         return;
        //     }
        //
        //     // -- Chunk the extracted text using the legacy approach
        //     chunks.AddRange(_ragSystem.Toolbox.SplitTextIntoChunks(extractedText, false));
        // }
    }


    public async Task<List<string>> GenerateTagsAsync(string id)
    {
        // If no existing tags, proceed to generate new tags
        _logger.Information("Generating new AI tags");

        ulong offset = 0;
        ulong limit = 20;
        HashSet<string> uniqueTags = [];


        ChatCompletionOptions options = new ChatCompletionOptions()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("TagsExtraction", BinaryData.FromString(Prompts.TagsOutputJsonSchema))
        };

        while (true)
        {
            // Perform a vector search to retrieve results
            var results = await _ragSystem.QdrantClient.QueryAsync(
                RAGSystem.COLLECTION_NAME,
                filter: MatchKeyword("resourceId", id),
                limit: limit,
                offset: offset
            );

            // If no results are returned, break the loop
            if (results.Count == 0) break;

            // Prepare the data for the template
            var data = new
            {
                tags = uniqueTags.Count == 0 ? "No tags generated yet." : string.Join(", ", uniqueTags),
                content = results.Select(item =>
                {
                    var payload = CustomPayload.FromPayload(item.Payload);
                    return new { text = payload.ChunkText };
                }).ToList()
            };

            string result = Prompts.TagsTemplate(data);

            List<ChatMessage> chat =
            [
                new SystemChatMessage("You are a helpful AI assistant that extracts tags from a document and returns them as a JSON"),
                new UserChatMessage(result)
            ];

            // Get a completion with structured output
            var response = await _ragSystem.ChatClient.CompleteChatAsync(chat, options);

            var jsonOutput = response.Value.Content[0].Text;
            var tagsExtraction = JsonSerializer.Deserialize<TagsExtraction>(jsonOutput);
            if (tagsExtraction == null || tagsExtraction.Tags == null) continue;

            _logger.Information("Extracted tags: {Tags}", string.Join(", ", tagsExtraction.Tags));

            // Add the tags to the unique set
            foreach (var tag in tagsExtraction.Tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    // Normalize the tag by trimming whitespace and converting to lowercase
                    uniqueTags.Add(tag.Trim().ToLowerInvariant());
                }
            }

            // Increment the offset for the next batch
            offset += limit;
        }


        try
        {
            // Convert to list and save to database
            var generatedTagsList = uniqueTags.ToList();

            if (generatedTagsList.Count > 0)
            {
                await _resourceManager.BeginTransaction();

                // Serialize the tags to JSON and save to the resource
                var tagsJson = JsonSerializer.Serialize(generatedTagsList);
                await _resourceManager.UpdateResourceAsync(Guid.Parse(id), r => r.AiGeneratedTags, tagsJson);
                await _resourceManager.Commit();

                _logger.Information("AI-generated tags saved to resource {ResourceId}", id);
            }

            _logger.Information("Tags generated successfully");
        }
        catch (Exception)
        {
            await _resourceManager.Rollback();
            _logger.Error("An error occurred while saving AI-generated tags to the resource {ResourceId}", id);
        }

        return [.. uniqueTags];
    }



    public async Task<string> GenerateChatTitleAsync(string query)
    {
        _logger.Information("Generating chat title");

        // Prepare the data for the template
        var data = new { query };
        string result = Prompts.ChatTitleTemplate(data);


        ChatCompletionOptions options = new ChatCompletionOptions()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("TitleExtraction", BinaryData.FromString(Prompts.ChatTitleOutputJsonSchema))
        };


        List<ChatMessage> chat =
        [
            new SystemChatMessage(Prompts.SystemPromptGenerateTitle),
            new UserChatMessage(result)
        ];

        // Get a completion with structured output
        var response = await _ragSystem.ChatClient.CompleteChatAsync(chat, options);

        var jsonOutput = response.Value.Content[0].Text;
        var tagsExtraction = JsonSerializer.Deserialize<TitleGeneration>(jsonOutput);
        if (tagsExtraction == null || tagsExtraction.Title == null)
        {
            _logger.Error("Failed to generate chat title from the response: {Response}", jsonOutput);
            return "Untitled Chat";
        }

        _logger.Information("Generated chat title successfully");
        // Return the generated title
        return tagsExtraction.Title;
    }
}

