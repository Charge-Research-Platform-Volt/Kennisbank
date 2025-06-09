

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
    /// </summary>
    /// <param name="id">The unique identifier for the resource being processed.</param>
    /// <param name="fileType">The type of file being processed (e.g., PDF, DOCX).</param>
    /// <param name="chunk">An initial chunk of text to include in the processing.</param>
    /// <param name="fileStream">A stream representing the file to be processed.</param>
    /// 
    /// <returns>A task representing the asynchronous operation of processing the document.</returns>
    /// <remarks>
    /// The pipeline consists of the following steps:
    /// 1. Extract text from the document
    /// 2. Chunk the extracted text into smaller segments
    /// 3. Generate vector embeddings for each chunk
    /// 4. Store the chunks and their embeddings in the vector database
    /// 
    /// If no text is extracted from the document, the process will terminate early.
    /// </remarks>
    public async Task MainPipline(Guid id, string chunk, string? fileType, Stream? fileStream = null)
    {
        _logger.Information("Main RAG pipeline started for resource ID: {Id}", id);

        //
        if (fileStream != null)
        {
            AnalyzeDocumentOptions options = new AnalyzeDocumentOptions(
                modelId: "prebuilt-layout",
                bytesSource: BinaryData.FromStream(fileStream)
            )
            {
                OutputContentFormat = DocumentContentFormat.Markdown,
            };

            // -- Analyze the document using Azure Document Intelligence
            Operation<AnalyzeResult> operation = await _ragSystem.DocumentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, options);
            // print the content of the document
            AnalyzeResult result = operation.Value;
            _logger.Information("Document analysis completed for resource ID: {Id}", id);
            _logger.Information("Document content: {Content}", result.Content);
        }


        //
        List<string> chunks = [$"{chunk}",]; // Include metadata in the first chunk

        // -- Extract text from the document
        if (!string.IsNullOrWhiteSpace(fileType))
        {
            string extractedText = await _ragSystem.Toolbox.ExtractTextAsync(fileType, id, _blobService);

            if (string.IsNullOrEmpty(extractedText))
            {
                _logger.Warning("No text extracted from the document");
                return;
            }

            // -- Chunk the extracted text
            chunks.AddRange(_ragSystem.Toolbox.SplitTextIntoChunks(extractedText, false));
        }


        // -- Generate points (vector embeddings) and store them in the vector database
        _logger.Information("Generating vector embeddings and storing chunks for resource ID: {Id}", id);
        await _ragSystem.CreatePoints(id, chunks);


        // -- Generate AI tags for the resource
        _logger.Information("Generating AI tags for resource ID: {Id}", id);
        await GenerateTagsAsync(id.ToString());
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
            new SystemChatMessage("You are a helpful AI assistant that generates a concise and descriptive title for a chat based on the provided question. Return the title as a JSON object with a single field 'Title'."),
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

