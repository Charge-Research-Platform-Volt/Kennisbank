

using System.Text.Json;
using HandlebarsDotNet;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using OpenAI.Chat;
using Serilog;
using static Qdrant.Client.Grpc.Conditions;
using Azure.AI.DocumentIntelligence;
using Azure;
using System.ClientModel;


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

            // * STEP 1: Document Text Extraction
            // Extract text from the document using Azure Document Intelligence service
            _logger.Information("Extracting text from the document for resource ID: {Id}", id);

            if (fileStream != null)
            {
                // Configure Azure Document Intelligence options
                AnalyzeDocumentOptions options = new AnalyzeDocumentOptions(
                    modelId: "prebuilt-layout",
                    bytesSource: BinaryData.FromStream(fileStream))
                {
                    OutputContentFormat = DocumentContentFormat.Text
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

                // * STEP 2: Text Chunking
                // Split the extracted text into smaller chunks suitable for embedding generation
                chunks.AddRange(_ragSystem.Toolbox.SplitTextIntoChunks(extractedText, logChunks: false, markdownSplit: false));

                _logger.Information("Successfully extracted and chunked text into {ChunkCount} segments for resource ID: {Id}", chunks.Count, id);
            }


            // * STEP 3 & 4: Vector Embedding Generation and Storage
            // Generate embeddings for each chunk and store them in the vector database
            _logger.Information("Generating vector embeddings and storing {ChunkCount} chunks for resource ID: {Id}", chunks.Count, id);
            await _ragSystem.CreatePoints(id, chunks);


            // * STEP 5: AI Tag Generation
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



    #region GenerateTagsAsync

    /// <summary>
    /// Generates AI-powered tags for a resource by analyzing its content chunks using vector search and natural language processing.
    /// </summary>
    /// <param name="id">The unique identifier of the resource for which to generate tags.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of generated tags in lowercase format.</returns>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Queries the vector database in batches to retrieve content chunks associated with the resource
    /// 2. Uses AI chat completion with structured JSON output to extract relevant tags from the content
    /// 3. Accumulates unique tags across all content chunks, normalizing them to lowercase
    /// 4. Persists the generated tags to the database within a transaction
    /// 5. Returns the complete list of generated tags
    /// 
    /// The method uses pagination to process large datasets efficiently and ensures data consistency
    /// through database transactions with proper rollback handling on errors.
    /// </remarks>
    /// <exception cref="Exception">Thrown when an error occurs during database operations while saving the generated tags.</exception>
    public async Task<List<string>> GenerateTagsAsync(string id)
    {
        _logger.Information("Generating new AI tags for resource {ResourceId}", id);

        HashSet<string> uniqueTags = new HashSet<string>();
        ChatCompletionOptions options = new ChatCompletionOptions()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("TagsExtraction", BinaryData.FromString(Prompts.TagsOutputJsonSchema))
        };

        await ProcessDocumentChunksAsync(id, uniqueTags, options);
        List<string> generatedTags = uniqueTags.ToList();

        if (generatedTags.Count > 0) await SaveTagsToResourceAsync(id, generatedTags);

        _logger.Information("Tags generated successfully for resource {ResourceId}. Total tags: {TagCount}", id, generatedTags.Count);
        return generatedTags;
    }



    /// <summary>
    /// Processes document chunks in batches to extract and collect unique tags from a specific document resource.
    /// </summary>
    /// <param name="id">The resource identifier used to filter document chunks in the collection.</param>
    /// <param name="uniqueTags">A collection of unique tags that will be populated with extracted tags from the document chunks.</param>
    /// <param name="options">Chat completion options used for tag extraction processing.</param>
    /// <returns>A task representing the asynchronous operation of processing all document chunks for the specified resource.</returns>
    private async Task ProcessDocumentChunksAsync(string id, HashSet<string> uniqueTags, ChatCompletionOptions options)
    {
        ulong offset = 0;
        const ulong batchSize = 1000;

        while (true)
        {
            var results = await _ragSystem.QdrantClient.QueryAsync(
                _ragSystem.CollectionName,
                filter: MatchKeyword("resourceId", id),
                limit: batchSize,
                offset: offset
            );

            // If no results are returned, break the loop
            if (results.Count == 0) break;

            TagsExtraction? extractedTags = await ExtractTagsFromChunksAsync(results, uniqueTags, options);
            if (extractedTags != null) AddTagsToCollection(extractedTags, uniqueTags);

            offset += batchSize;
        }
    }



    /// <summary>
    /// Extracts tags from document chunks using AI completion and returns them as a structured object.
    /// </summary>
    /// <param name="results">A read-only list of scored points containing document chunks from vector search results.</param>
    /// <param name="existingTags">A set of tags that have already been generated to provide context to the AI model.</param>
    /// <param name="options">Configuration options for the chat completion request.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="TagsExtraction"/> object
    /// with extracted tags, or null if deserialization fails.
    /// </returns>
    private async Task<TagsExtraction?> ExtractTagsFromChunksAsync(IReadOnlyList<Qdrant.Client.Grpc.ScoredPoint> results, HashSet<string> existingTags, ChatCompletionOptions options)
    {
        try
        {
            var templateData = new
            {
                tags = existingTags.Count == 0 ? "No tags generated yet." : string.Join(", ", existingTags),
                content = results.Select(item =>
                {
                    var payload = CustomPayload.FromPayload(item.Payload);
                    return new { text = payload.ChunkText };
                }).ToList()
            };
            string prompt = Prompts.TagsTemplate(templateData);

            // Prepare chat messages with the system prompt and user query
            List<ChatMessage> messages = new List<ChatMessage>
                {
                    new SystemChatMessage(Prompts.SystemPromptGenerateTags),
                    new UserChatMessage(prompt)
                };

            // Get a completion with structured output
            ClientResult<ChatCompletion> response = await _ragSystem.ChatClient.CompleteChatAsync(messages, options);
            string jsonOutput = response.Value.Content[0].Text;

            return JsonSerializer.Deserialize<TagsExtraction>(jsonOutput);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to extract tags from chunks");
            return null;
        }
    }



    /// <summary>
    /// Adds tags from a TagsExtraction object to a collection of unique tags.
    /// </summary>
    /// <param name="tagsExtraction">The TagsExtraction object containing tags to be added. Can be null.</param>
    /// <param name="uniqueTags">The HashSet collection to store unique tags. Tags are normalized to lowercase and trimmed.</param>
    private void AddTagsToCollection(TagsExtraction? tagsExtraction, HashSet<string> uniqueTags)
    {
        if (tagsExtraction?.Tags == null) return;

        // Add each tag to the unique set.
        foreach (string tag in tagsExtraction.Tags)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                // Normalize the tag by trimming whitespace and converting to lowercase 
                uniqueTags.Add(tag.Trim().ToLowerInvariant());
            }
        }
    }



    /// <summary>
    /// Saves AI-generated tags to a resource in the database within a transaction.
    /// </summary>
    /// <param name="id">The unique identifier of the resource to update.</param>
    /// <param name="tags">The list of AI-generated tags to save to the resource.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown when an error occurs during the database transaction or update operation.</exception>
    private async Task SaveTagsToResourceAsync(string id, List<string> tags)
    {
        try
        {
            await _resourceManager.BeginTransaction();

            string tagsJson = JsonSerializer.Serialize(tags);
            await _resourceManager.UpdateResourceAsync(Guid.Parse(id), r => r.AiGeneratedTags, tagsJson);
            await _resourceManager.Commit();

            _logger.Information("AI-generated tags saved to resource {ResourceId}", id);
        }
        catch (Exception ex)
        {
            await _resourceManager.Rollback();
            _logger.Error(ex, "An error occurred while saving AI-generated tags to resource {ResourceId}", id);
            throw;
        }
    }

    #endregion


    #region GenerateChatTitleAsync

    /// <summary>
    /// Generates a descriptive title for a chat conversation based on the provided query.
    /// </summary>
    /// <param name="query">The user query or message content to generate a title from.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a string 
    /// representing the generated chat title, or "Untitled Chat" if title generation fails.
    /// </returns>
    /// <remarks>
    /// This method uses AI chat completion with structured JSON output to generate meaningful 
    /// titles from user queries. It utilizes predefined prompts and JSON schema validation 
    /// to ensure consistent output format.
    /// </remarks>
    /// <exception cref="Exception">
    /// May throw exceptions related to AI service communication or JSON deserialization failures.
    /// </exception>
    public async Task<string> GenerateChatTitleAsync(string query)
    {
        _logger.Information("Generating chat title");

        try
        {
            var templateData = new { query };
            string promptContent = Prompts.ChatTitleTemplate(templateData);

            ChatCompletionOptions options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("TitleExtraction", BinaryData.FromString(Prompts.ChatTitleOutputJsonSchema))
            };

            List<ChatMessage> messages = new List<ChatMessage>
            {
                new SystemChatMessage(Prompts.SystemPromptGenerateTitle),
                new UserChatMessage(promptContent)
            };

            ClientResult<ChatCompletion> response = await _ragSystem.ChatClient.CompleteChatAsync(messages, options);
            string jsonResponse = response.Value.Content[0].Text;
            TitleGeneration? titleGeneration = JsonSerializer.Deserialize<TitleGeneration>(jsonResponse);

            if (titleGeneration?.Title == null)
            {
                _logger.Warning("Failed to generate chat title from response: {Response}", jsonResponse);
                return "Untitled Chat";
            }

            _logger.Information("Successfully generated chat title");
            return titleGeneration.Title;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error occurred while generating chat title");
            return "Untitled Chat";
        }
    }

    #endregion
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


