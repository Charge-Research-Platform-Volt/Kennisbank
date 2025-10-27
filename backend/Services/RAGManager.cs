

using System.Text;
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
using Docker.DotNet.Models;


namespace KnowledgeBank.Services;

public class RAGManager
{
    private readonly Serilog.ILogger _logger;
    private readonly RAGSystem _ragSystem;
    private readonly ResourceManager _resourceManager;
    private readonly IAzureBlobService _blobService;


    public RAGManager(ResourceManager resourceManager, RAGSystem ragSystem, IAzureBlobService blobService)
    {
        _logger = Log.ForContext<RAGManager>();
        _ragSystem = ragSystem;
        _resourceManager = resourceManager;
        _blobService = blobService;
    }



    /// <summary>
    /// Processes a document through the RAG (Retrieval-Augmented Generation) pipeline.
    /// This method orchestrates the complete document processing workflow from text extraction
    /// to vector storage and AI tag generation.
    /// </summary>
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
    /// <summary>
    /// Builds a rich metadata chunk that combines structured metadata fields for better retrieval.
    /// </summary>
    /// <param name="id">The resource ID</param>
    /// <param name="basicMetadata">Basic metadata string (title, description, etc.)</param>
    /// <returns>An enhanced metadata string optimized for semantic search</returns>
    private async Task<string> BuildRichMetadataChunk(Guid id, string basicMetadata)
    {
        try
        {
            // Fetch additional metadata from database
            var resource = await _resourceManager.GetResourceAsync(r => r.Id == id, includeProperties: "Authors,Tags,Organisations,Regions");

            if (resource == null)
            {
                _logger.Warning("Resource {Id} not found for metadata enrichment. Using basic metadata.", id);
                return basicMetadata;
            }

            var metadataBuilder = new StringBuilder();
            metadataBuilder.AppendLine("=== DOCUMENT METADATA ===");
            metadataBuilder.AppendLine(basicMetadata);
            metadataBuilder.AppendLine();

            // Add structured fields that help with retrieval
            if (!string.IsNullOrEmpty(resource.Title))
                metadataBuilder.AppendLine($"Title: {resource.Title}");

            if (!string.IsNullOrEmpty(resource.Description))
                metadataBuilder.AppendLine($"Description: {resource.Description}");

            if (resource.PublicationDate != default(DateTime))
                metadataBuilder.AppendLine($"Publication Date: {resource.PublicationDate:yyyy-MM-dd}");

            if (!string.IsNullOrEmpty(resource.LanguageCode))
                metadataBuilder.AppendLine($"Language: {resource.LanguageCode}");

            if (!string.IsNullOrEmpty(resource.License))
                metadataBuilder.AppendLine($"License: {resource.License}");

            // Add semantic context
            metadataBuilder.AppendLine();
            metadataBuilder.AppendLine("=== SEMANTIC CONTEXT ===");
            metadataBuilder.AppendLine($"Resource Type: {resource.TypeId}");
            metadataBuilder.AppendLine($"This document is about: {resource.Title}");

            if (!string.IsNullOrEmpty(resource.Description))
                metadataBuilder.AppendLine($"Summary: {resource.Description}");

            if (!string.IsNullOrEmpty(resource.Note))
                metadataBuilder.AppendLine($"Note: {resource.Note}");

            _logger.Information("Built rich metadata chunk for resource {Id}", id);
            return metadataBuilder.ToString();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to build rich metadata for resource {Id}. Using basic metadata.", id);
            return basicMetadata;
        }
    }

    public async Task MainPipeline(Guid id, string chunk, string? fileType = null, Stream? fileStream = null)
    {
        _logger.Information("Main RAG pipeline started for resource ID: {Id}", id);

        try
        {
            // Build a rich metadata chunk for better retrieval
            string richMetadata = await BuildRichMetadataChunk(id, chunk);

            // Initialize chunks collection with the enhanced metadata chunk
            List<string> chunks = [richMetadata];

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


    #region Metadata Updates

    /// <summary>
    /// Updates the metadata point in Qdrant when resource metadata changes.
    /// This rebuilds the rich metadata chunk and updates the vector embedding.
    /// </summary>
    /// <param name="id">The resource ID to update metadata for</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> UpdateResourceMetadataAsync(Guid id)
    {
        try
        {
            _logger.Information("Updating metadata for resource {ResourceId}", id);

            // Build fresh rich metadata from current database state
            string richMetadata = await BuildRichMetadataChunk(id, string.Empty);

            // Update the vector database
            bool success = await _ragSystem.UpdateMetadataPointAsync(id.ToString(), richMetadata);

            if (success)
            {
                _logger.Information("Successfully updated metadata for resource {ResourceId}", id);
            }
            else
            {
                _logger.Warning("Failed to update metadata for resource {ResourceId}", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating metadata for resource {ResourceId}", id);
            return false;
        }
    }

    #endregion


    #region Query Enhancement

    /// <summary>
    /// Enhances a user query by expanding it with synonyms and related terms for better retrieval.
    /// </summary>
    /// <param name="originalQuery">The original user query</param>
    /// <returns>An enhanced query string that may retrieve more relevant results</returns>
    /// <remarks>
    /// This method uses the LLM to:
    /// - Identify key concepts in the query
    /// - Add relevant synonyms and related terms
    /// - Maintain the original intent while broadening the search scope
    /// - Keep the query concise and focused
    /// </remarks>
    public async Task<string> EnhanceQueryAsync(string originalQuery)
    {
        if (string.IsNullOrWhiteSpace(originalQuery))
            return originalQuery;

        // Don't enhance very short queries (they're usually specific enough)
        if (originalQuery.Length < 10)
            return originalQuery;

        try
        {
            _logger.Information("Enhancing query: {Query}", originalQuery);

            string promptTemplate = @"You are a search query enhancement assistant. Your task is to improve the given search query to retrieve more relevant results from a knowledge base.

Original Query: {{query}}

Instructions:
1. Keep the core intent of the original query
2. Add 2-3 relevant synonyms or related terms
3. Make it concise (max 2 sentences)
4. Focus on semantic meaning, not just keywords

Enhanced Query:";

            var templateData = new { query = originalQuery };
            var template = Handlebars.Compile(promptTemplate);
            string prompt = template(templateData);

            List<ChatMessage> messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful search query enhancement assistant. Provide only the enhanced query, nothing else."),
                new UserChatMessage(prompt)
            };

            ClientResult<ChatCompletion> response = await _ragSystem.ChatClient.CompleteChatAsync(messages);
            string enhancedQuery = response.Value.Content[0].Text.Trim();

            // Validate enhanced query isn't too different or too long
            if (enhancedQuery.Length > originalQuery.Length * 3 || enhancedQuery.Length > 500)
            {
                _logger.Warning("Enhanced query too long, using original");
                return originalQuery;
            }

            _logger.Information("Enhanced query: {EnhancedQuery}", enhancedQuery);
            return enhancedQuery;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to enhance query, using original");
            return originalQuery;
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
    
    #region Metadata Extraction
    
    /// <summary>
    /// Extracts metadata from document text using LLM analysis
    /// </summary>
    /// <param name="text">The full text extracted from the document</param>
    /// <param name="fileName">The original filename for context</param>
    /// <returns>Extracted metadata or null if extraction fails</returns>
    public async Task<ExtractedMetadata?> ExtractMetadataAsync(string text, string fileName) 
    {
        try 
        {
            _logger.Information("Starting metadata extraction for file: {FileName}", fileName);

            // Step 1: Truncate text if too long (since LLMs have context limits)
            // Take the first 8000 characters, which is usually enough for the title, authors, abstract
            string textToAnalyze = text.Length > 8000 ? text.Substring(0, 8000) : text;

            _logger.Information("Analyzing {Length} characters of text", textToAnalyze.Length);

            // Step 2: Create prompt for LLM
            string prompt = $@"
                You are a metadata extraction assistant. Analyze the following document text and extract sturctured metadata.
                
                Filename: {fileName}
                
                Document text:
                {textToAnalyze}
                
                Extract the following information in JSON format:
                {{
                    ""title"": ""The document title"",
                    ""abstract"": ""The abstract of the paper when it is a scientific paper, else leave empty"",
                    ""description"": ""A complete and consise description of the document (50-300 words)"",
                    ""publicationDate"": ""YYYY-MM-DD format or null"",
                    ""languageCode"": ""ISO 639-1 two-letter code (e.g., 'en', 'nl', 'fr', etc.)"",
                    ""authors"": [""Array of author names""],
                    ""publicationCode"": ""DOI, ISBN, arXiv ID, etc. or null"",
                    ""tags"": [""Array of categorization tags like 'research paper', 'technical report', 'computer science', etc.""]
                }}
                
                Rules:
                - If a field cannot be determined, use null or empty array
                - Language code must be 2 letters lowercase (From the ISO 639-1 list)
                - Publication date must be in YYYY-MM-DD format
                - Note which type of publication code it is before the actual publication code
                - Return ONLY valid JSON, no additional text or explanation
            ";

            // Step 3: Call the LLM with JSON mode
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful assistant that extracts metadata from documents. Always respond with valid JSON only."),
                new UserChatMessage(prompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            _logger.Information("Calling LLM for metadata extraction");
            var response = await _ragSystem.ChatClient.CompleteChatAsync(messages, chatOptions);

            var jsonContent = response.Value.Content[0].Text;
            _logger.Information("Received LLM response: {Length} characters", jsonContent.Length);

            // Step 4: Parse JSON response
            var metadata = JsonSerializer.Deserialize<ExtractedMetadata>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (metadata == null) 
            {
                _logger.Warning("Failed to deserialize metadata from LLM response");
                return null;
            }
            
            // Step 5: Validate and clean the data
            // Ensure language code is valid (2 letters, lowercase)
            if (!string.IsNullOrEmpty(metadata.LanguageCode)) 
            {
                metadata.LanguageCode = metadata.LanguageCode.ToLowerInvariant();
                if (metadata.LanguageCode.Length != 2) 
                {
                    _logger.Warning("Invalid language code: {Code}, setting to null", metadata.LanguageCode);
                    metadata.LanguageCode = null;
                }
            }

            // Trim whitespace from strings
            metadata.Title = metadata.Title?.Trim();
            metadata.Abstract = metadata.Abstract?.Trim();
            metadata.Description = metadata.Description?.Trim();
            metadata.PublicationCode = metadata.PublicationCode?.Trim();

            // Remove empty strings from arrays
            metadata.Authors = metadata.Authors?.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).ToList() ?? new List<string>();
            metadata.Tags = metadata.Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList() ?? new List<string>();

            _logger.Information("Metadata extraction completed successfully. Title: {Title}, Authors: {AuthorCount}, Tags: {TagCount}", metadata.Title, metadata.Authors.Count, metadata.Tags.Count);

            return metadata;
        }
        catch (Exception e) 
        {
            _logger.Error(e, "Error extracting metadata from document");
            return null;
        }
    }
    
    #endregion
    
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


