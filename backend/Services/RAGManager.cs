

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
using KnowledgeBank.Services.Search.Models;
using KnowledgeBank.Services.Search;
using System.Threading.Tasks;


namespace KnowledgeBank.Services;

public class RAGManager(ResourceManager resourceManager, RAGSystem ragSystem, HybridSearchService searchService)
{
    private readonly Serilog.ILogger logger = Log.ForContext<RAGManager>();



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
            // Fetch additional metadata from database with nested entities
            var resource = await resourceManager.GetResourceAsync(r => r.Id == id, includeProperties: new[]
            {
                "ResourceAuthorRelations.Author",
                "ResourceTagRelations.Tag",
                "ResourceOrganisationRelations.Organisation",
                "ResourceRegionRelations.Region"
            });

            if (resource == null)
            {
                logger.Warning("Resource {Id} not found for metadata enrichment. Using basic metadata.", id);
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

            if (resource.PublicationDate.HasValue)
                metadataBuilder.AppendLine($"Publication Date: {resource.PublicationDate.Value:yyyy-MM-dd}");

            if (!string.IsNullOrEmpty(resource.LanguageCode))
                metadataBuilder.AppendLine($"Language: {resource.LanguageCode}");

            if (!string.IsNullOrEmpty(resource.License))
                metadataBuilder.AppendLine($"License: {resource.License}");

            // Add authors
            if (resource.ResourceAuthorRelations?.Any() == true)
            {
                var authorNames = resource.ResourceAuthorRelations
                    .Where(r => r.Author != null)
                    .Select(r => r.Author!.Name)
                    .ToList();
                if (authorNames.Any())
                    metadataBuilder.AppendLine($"Authors: {string.Join(", ", authorNames)}");
            }

            // Add tags
            if (resource.ResourceTagRelations?.Any() == true)
            {
                var tagNames = resource.ResourceTagRelations
                    .Where(r => r.Tag != null)
                    .Select(r => r.Tag!.Name)
                    .ToList();
                if (tagNames.Any())
                    metadataBuilder.AppendLine($"Tags: {string.Join(", ", tagNames)}");
            }

            // Add organizations
            if (resource.ResourceOrganisationRelations?.Any() == true)
            {
                var orgNames = resource.ResourceOrganisationRelations
                    .Where(r => r.Organisation != null)
                    .Select(r => r.Organisation!.Name)
                    .ToList();
                if (orgNames.Any())
                    metadataBuilder.AppendLine($"Organizations: {string.Join(", ", orgNames)}");
            }

            // Add regions
            if (resource.ResourceRegionRelations?.Any() == true)
            {
                var regionNames = resource.ResourceRegionRelations
                    .Where(r => r.Region != null)
                    .Select(r => r.Region!.Name)
                    .ToList();
                if (regionNames.Any())
                    metadataBuilder.AppendLine($"Regions: {string.Join(", ", regionNames)}");
            }

            // Add semantic context
            metadataBuilder.AppendLine();
            metadataBuilder.AppendLine("=== SEMANTIC CONTEXT ===");
            metadataBuilder.AppendLine($"Resource Type: {resource.TypeId}");
            metadataBuilder.AppendLine($"This document is about: {resource.Title}");

            if (!string.IsNullOrEmpty(resource.Description))
                metadataBuilder.AppendLine($"Summary: {resource.Description}");

            if (!string.IsNullOrEmpty(resource.Note))
                metadataBuilder.AppendLine($"Note: {resource.Note}");

            logger.Information("Built rich metadata chunk for resource {Id}", id);
            return metadataBuilder.ToString();
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to build rich metadata for resource {Id}. Using basic metadata.", id);
            return basicMetadata;
        }
    }

    public async Task MainPipeline(Guid id, string chunk, string? fileType = null, Stream? fileStream = null)
    {
        logger.Information("Main RAG pipeline started for resource ID: {Id}", id);

        try
        {
            // Build a rich metadata chunk for better retrieval
            string richMetadata = await BuildRichMetadataChunk(id, chunk);

            // Initialize chunks collection with the enhanced metadata chunk
            List<string> chunks = [richMetadata];

            // * STEP 1: Document Text Extraction
            // Extract text from the document using Azure Document Intelligence service
            logger.Information("Extracting text from the document for resource ID: {Id}", id);

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
                Operation<AnalyzeResult> operation = await ragSystem.DocumentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, options);

                string extractedText = operation.Value.Content;

                // Validate that text extraction was successful
                if (string.IsNullOrEmpty(extractedText))
                {
                    logger.Warning("No text extracted from the document");
                    return;
                }

                // * STEP 2: Text Chunking
                // Split the extracted text into smaller chunks suitable for embedding generation
                chunks.AddRange(ragSystem.Toolbox.SplitTextIntoChunks(extractedText, logChunks: false, markdownSplit: false));

                logger.Information("Successfully extracted and chunked text into {ChunkCount} segments for resource ID: {Id}", chunks.Count, id);
            }


            // * STEP 3 & 4: Vector Embedding Generation and Storage
            // Generate embeddings for each chunk and store them in the vector database
            logger.Information("Generating vector embeddings and storing {ChunkCount} chunks for resource ID: {Id}", chunks.Count, id);
            await ragSystem.CreatePoints(id, chunks);


            // * STEP 5: AI Tag Generation
            // Generate contextual tags based on the processed document content
            logger.Information("Initiating AI tag generation for resource ID: {Id}", id);
            await GenerateTagsAsync(id.ToString());

            logger.Information("RAG pipeline completed successfully for resource ID: {Id}", id);
        }
        catch (Exception ex)
        {
            // Todo: Add a way to notify the user that the pipeline failed, with some options to retry.

            logger.Error(ex, "An error occurred while processing the document for resource ID: {Id}. Pipeline execution failed.", id);
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
        logger.Information("Generating new AI tags for resource {ResourceId}", id);

        HashSet<string> uniqueTags = new HashSet<string>();
        ChatCompletionOptions options = new ChatCompletionOptions()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("TagsExtraction", BinaryData.FromString(Prompts.TagsOutputJsonSchema))
        };

        await ProcessDocumentChunksAsync(id, uniqueTags, options);
        List<string> generatedTags = uniqueTags.ToList();

        if (generatedTags.Count > 0) await SaveTagsToResourceAsync(id, generatedTags);

        logger.Information("Tags generated successfully for resource {ResourceId}. Total tags: {TagCount}", id, generatedTags.Count);
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
            var results = await ragSystem.QdrantClient.QueryAsync(
                ragSystem.CollectionName,
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
            ClientResult<ChatCompletion> response = await ragSystem.ChatClient.CompleteChatAsync(messages, options);
            string jsonOutput = response.Value.Content[0].Text;

            return JsonSerializer.Deserialize<TagsExtraction>(jsonOutput);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to extract tags from chunks");
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
            await resourceManager.BeginTransaction();

            string tagsJson = JsonSerializer.Serialize(tags);
            await resourceManager.UpdateResourceAsync(Guid.Parse(id), r => r.AiGeneratedTags, tagsJson);
            await resourceManager.Commit();

            logger.Information("AI-generated tags saved to resource {ResourceId}", id);
        }
        catch (Exception ex)
        {
            await resourceManager.Rollback();
            logger.Error(ex, "An error occurred while saving AI-generated tags to resource {ResourceId}", id);
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
            logger.Information("Updating metadata for resource {ResourceId}", id);

            // Build fresh rich metadata from current database state
            string richMetadata = await BuildRichMetadataChunk(id, string.Empty);

            // Update the vector database
            bool success = await ragSystem.UpdateMetadataPointAsync(id.ToString(), richMetadata);

            if (success)
            {
                logger.Information("Successfully updated metadata for resource {ResourceId}", id);
            }
            else
            {
                logger.Warning("Failed to update metadata for resource {ResourceId}", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error updating metadata for resource {ResourceId}", id);
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
            logger.Information("Enhancing query: {Query}", originalQuery);

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

            ClientResult<ChatCompletion> response = await ragSystem.ChatClient.CompleteChatAsync(messages);
            string enhancedQuery = response.Value.Content[0].Text.Trim();

            // Validate enhanced query isn't too different or too long
            if (enhancedQuery.Length > originalQuery.Length * 3 || enhancedQuery.Length > 500)
            {
                logger.Warning("Enhanced query too long, using original");
                return originalQuery;
            }

            logger.Information("Enhanced query: {EnhancedQuery}", enhancedQuery);
            return enhancedQuery;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to enhance query, using original");
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
        logger.Information("Generating chat title");

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

            ClientResult<ChatCompletion> response = await ragSystem.ChatClient.CompleteChatAsync(messages, options);
            string jsonResponse = response.Value.Content[0].Text;
            TitleGeneration? titleGeneration = JsonSerializer.Deserialize<TitleGeneration>(jsonResponse);

            if (titleGeneration?.Title == null)
            {
                logger.Warning("Failed to generate chat title from response: {Response}", jsonResponse);
                return "Untitled Chat";
            }

            logger.Information("Successfully generated chat title");
            return titleGeneration.Title;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error occurred while generating chat title");
            return "Untitled Chat";
        }
    }

    #endregion

    #region Metadata Extraction

    /// <summary>
    /// Extracts metadata from document text using LLM analysis
    /// </summary>
    /// <param name="text">The full text extracted from the document</param>
    /// <param name="fileNameOrUrl">The original filename or url for context</param>
    /// <returns>Extracted metadata or null if extraction fails</returns>
    public async Task<ExtractedMetadata?> ExtractMetadataFromFileAsync(string text, string fileNameOrUrl)
    {
        logger.Information("Starting metadata extraction for: {FileNameOrUrl}", fileNameOrUrl);

        // Step 1: Smart sampling for long documents
        // For academic papers, we need both the beginning (metadata, abstract, authors)
        // and the end (references, acknowledgments) to extract all related persons / organisations
        string textToAnalyze = trimText(text);

        logger.Information("Analyzing {Length} characters of text", textToAnalyze.Length);

        // Step 2: Create prompt for LLM
        string prompt = $@"
            You are a metadata extraction assistant. Analyze the following document text and extract structured metadata.

            Filename/URL: {fileNameOrUrl}

            Document text:
            {textToAnalyze}

            Extract the following information in JSON format:
            {{
                ""title"": ""The document title"",
                ""abstract"": ""The abstract of the paper when it is a scientific paper, else leave empty"",
                ""description"": ""A complete and concise description of the document (50-300 words)"",
                ""publicationDate"": ""YYYY, YYYY-MM, or YYYY-MM-DD format (use most specific format available, or null)"",
                ""languageCode"": ""ISO 639-1 two-letter code (e.g., 'en', 'nl', 'fr', etc.)"",
                ""authors"": [{{""name"": ""Author name"", ""type"": ""person or organisation""}}],
                ""organisations"": [""Array of organization names mentioned in the document, EXCLUDING any organizations that are authors""],
                ""relatedPersons"": [""Array of person names related to this document who are NOT authors (e.g., people mentioned, cited, or acknowledged)""],
                ""publicationCode"": ""DOI, ISBN, arXiv ID, etc. or null"",
                ""tags"": [""Array of descriptive tags like 'Research Paper', 'Technical Report', 'Computer Science', etc.""]
            }}

            Rules:
            - If a field cannot be determined, use null or empty array
            - Language code must be 2 letters lowercase (From the ISO 639-1 list)
            - Publication date can be partial: YYYY (year only), YYYY-MM (year and month), or YYYY-MM-DD (full date). Use the most specific format you can determine from the document.
            - Note which type of publication code it is before the actual publication code
            - Make sure tags are capitalized, so they look good
            - **DEDUPLICATION RULE**: Each entity (person/organization) must appear ONLY ONCE per list
                * EXAMPLES (do NOT include these fictional names): If document has ""F. Lastname"" and ""Full Lastname"", include ONLY ""Full Lastname""
                * If the same name appears multiple times in document, include it ONLY ONCE in output
                * EXAMPLES (fictional): If document has ""Company"" and ""Company Inc."", include ONLY ""Company Inc.""
                * Always prefer the most complete version when you encounter variations of the same entity
                * Check each name before adding - if it's already in the list (even with slight variation), don't add it again
            - **CRITICAL NAME FORMATTING RULE**: All person names MUST follow the format: Given name(s) FIRST, Family name LAST
                * FORMATTING EXAMPLES (do NOT include these fictional names in your output):
                - If document shows ""Lastname, A.B."" → reformat to ""A.B. Lastname""
                - If document shows ""Doe, Jane"" → reformat to ""Jane Doe""
                * NEVER preserve comma-separated ""Last, First"" format from citations or references
                * Always reorder names from the actual document to: [Given name] [Family name]
                * IMPORTANT: Only extract names that actually appear in the document text, not from these examples
            - When the full name is available use that instead of just the first letters (e.g., if document has both ""F. Lastname"" and ""Full Lastname"", prefer ""Full Lastname"")

            Important distinctions:
            - AUTHORS: Who wrote/created this document. Can be individual persons OR organizations.
                * Step 1: Look for author attribution (bylines, ""by"", ""door"", etc.)
                * Step 2: Determine the type:
                - If it's a named individual (e.g., ""Door John Doe"") → add that person to AUTHORS
                - If it indicates the organization's own staff (e.g., ""by our newsroom"", ""by our editorial team"", ""door onze nieuwsredactie"", ""by staff"") → the organization itself is the author
                - If NO author attribution is found → leave AUTHORS empty
                * Step 3: When organizational authorship is indicated (""our newsroom"", ""onze redactie"", etc.):
                - Look for the publisher/organization name in the document (check headers, footers, logos, or prominent mentions)
                - Add that organization name to AUTHORS
                * CRITICAL: ""our""/""onze"" = the publishing organization. Find that organization's name in the document and use it as the author.

            - ORGANISATIONS: Organizations associated with this document.
                * Include publishers and source organizations
                * Include other organizations mentioned or discussed in the content
                * Note: An organization can appear in BOTH authors (if they wrote it) AND organisations (if they published it)
                * Leave empty if none are mentioned

            - RELATEDPERSONS: Individual people mentioned in the document who are NOT authors.
                * Examples: People cited, mentioned in acknowledgments, or discussed in the content
                * Do NOT include the document's author(s) here
                * Leave empty if no one is mentioned

            - Return ONLY valid JSON, no additional text or explanation
        ";

        // Step 3: Call the LLM with JSON mode
        TempExtractedMetadata? tempMetadata = await extractMetadataWithLLM(text, prompt);

        if (tempMetadata == null)
        {
            logger.Warning("Failed to deserialize metadata from LLM response");
            return null;
        }

        // Step 5: Validate and clean the data
        return await validateAndProcessMetadata(tempMetadata);
    }

    /// <summary>
    /// Omits the middle of very long documents to make it fit in the context of the LLM.
    /// We keep the start and end, because these often contain abstracts, authors and other metadata.
    /// </summary>
    /// <param name="text">The text to be trimmed</param>
    /// <returns>The trimmed text</returns>
    private string trimText(string text)
    {
        const int firstChars = 50000;
        const int lastChars = 30000;
        const int maxTotal = firstChars + lastChars;

        // Don't trim if it fits
        if (text.Length <= maxTotal)
            return text;

        // Extract beginning and ending
        string beginning = text.Substring(0, firstChars);
        string ending = text.Substring(text.Length - lastChars);

        // Return beginning + ending with indication that text was omitted for the LLM
        return beginning + "\n\n[...middle section omitted...]\n\n" + ending;
    }

    private readonly static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true};
    private async Task<TempExtractedMetadata?> extractMetadataWithLLM(string text, string prompt) 
    {
        // Setup messages
        List<ChatMessage> messages =
        [
            new SystemChatMessage("You are a helpful assistant that extracts metadata from the given text. Always respond with valid JSON only."),
            new UserChatMessage(prompt)
        ];

        // Setup options
        ChatCompletionOptions chatOptions = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };
        
        // Run chat
        logger.Information("Calling LLM for metadata extraction");
        ClientResult<ChatCompletion> response = await ragSystem.ChatClient.CompleteChatAsync(messages, chatOptions);

        // Extract and parse JSON
        string jsonContent = response.Value.Content[0].Text;
        logger.Information("Received LLM response: {Length} characters", jsonContent.Length);
        TempExtractedMetadata? tempMetadata = JsonSerializer.Deserialize<TempExtractedMetadata>(jsonContent, jsonSerializerOptions);

        return tempMetadata;
    }
    
    private async Task<ExtractedMetadata?> validateAndProcessMetadata(TempExtractedMetadata tempMetadata) 
    {
        try 
        {
            ExtractedMetadata metadata = new();
        
            // Ensure language code is valid (2 letters, lowercase)
            if (!string.IsNullOrEmpty(tempMetadata.LanguageCode)) 
            {
                metadata.LanguageCode = tempMetadata.LanguageCode.ToLowerInvariant();
                if (metadata.LanguageCode.Length != 2)
                {
                    logger.Warning("Invalid language code: {Code}, setting to null", metadata.LanguageCode);
                    metadata.LanguageCode = null;
                }
            }
            
            // Trim whitespace from strings
            metadata.Title = tempMetadata.Title?.Trim();
            metadata.Abstract = tempMetadata.Abstract?.Trim();
            metadata.Description = tempMetadata.Description?.Trim();
            metadata.PublicationCode = tempMetadata.PublicationCode?.Trim();
            
            // Parse publication date and determine precision
            if (!string.IsNullOrWhiteSpace(tempMetadata.PublicationDate))
            {
                var dateStr = tempMetadata.PublicationDate.Trim();
                var parts = dateStr.Split('-');

                try
                {
                    if (parts.Length == 1 && parts[0].Length == 4)
                    {
                        // Year only (e.g., "2020")
                        int year = int.Parse(parts[0]);
                        metadata.PublicationDate = new DateTime(year, 1, 1);
                        metadata.PublicationDatePrecision = PublicationDatePrecision.Year;
                        logger.Information("Parsed publication date with year precision: {Year}", year);
                    }
                    else if (parts.Length == 2 && parts[0].Length == 4 && parts[1].Length <= 2)
                    {
                        // Year and month (e.g., "2020-05")
                        int year = int.Parse(parts[0]);
                        int month = int.Parse(parts[1]);
                        metadata.PublicationDate = new DateTime(year, month, 1);
                        metadata.PublicationDatePrecision = PublicationDatePrecision.Month;
                        logger.Information("Parsed publication date with month precision: {Year}-{Month}", year, month);
                    }
                    else if (parts.Length == 3 && parts[0].Length == 4 && parts[1].Length <= 2 && parts[2].Length <= 2)
                    {
                        // Full date (e.g., "2020-05-15")
                        int year = int.Parse(parts[0]);
                        int month = int.Parse(parts[1]);
                        int day = int.Parse(parts[2]);
                        metadata.PublicationDate = new DateTime(year, month, day);
                        metadata.PublicationDatePrecision = PublicationDatePrecision.Day;
                        logger.Information("Parsed publication date with day precision: {Year}-{Month}-{Day}", year, month, day);
                    }
                    else
                    {
                        logger.Warning("Invalid publication date format: {DateStr}, setting to null", dateStr);
                        metadata.PublicationDate = null;
                        metadata.PublicationDatePrecision = null;
                    }
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Failed to parse publication date: {DateStr}, setting to null", dateStr);
                    metadata.PublicationDate = null;
                    metadata.PublicationDatePrecision = null;
                }
            }

            // Remove empty strings from tags
            metadata.Tags = tempMetadata.Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList() ?? [];

            // Process entities
            metadata.Authors = await processAuthors(tempMetadata);
            metadata.Organisations = await processOrganisations(tempMetadata);
            metadata.RelatedPersons = await processRelatedPersons(tempMetadata);
            
            logger.Information("Metadata extraction completed successfully. Title: {Title}, Authors: {AuthorCount}, Organisations: {OrgCount}, RelatedPersons: {PersonCount}, Tags: {TagCount}",
                metadata.Title, metadata.Authors.Count, metadata.Organisations.Count, metadata.RelatedPersons.Count, metadata.Tags.Count);
            
            return metadata;
        }
        catch (Exception e) 
        {
            logger.Error(e, "Error extracting metadata");
            return null;
        }
    }
    
    private async Task<List<AuthorWithSimilars>> processAuthors(TempExtractedMetadata tempMetadata) 
    {
        TempAuthor[] tempAuthors = tempMetadata.Authors?.Where(a => !string.IsNullOrWhiteSpace(a.Name)).ToArray() ?? [];
        List<AuthorWithSimilars> authorsWithSimilars = [];
        
        foreach (TempAuthor author in tempAuthors) 
        {
            logger.Information("Finding similars for author '{Author}' (type: {Type})", author.Name, author.Type);

            // Search for similars in the database
            HybridSearchResult result = await searchService.SearchAsync(
                author.Name.Replace(".", ""),
                1,
                3,
                new Dictionary<string, object?> { { "type", new string[] { "person", "organisation" } } }
            );
            
            // Filter out low-confidence matches (below 50%) and map to SimilarEntity
            List<SimilarEntity> similars = [.. result.Items
                .Where(i => i.RelevanceScore >= 0.5f) // Only show matches with 50%+ confidence
                .Select(i => new SimilarEntity
                {
                    Id = i.Id,
                    Name = i.Name,
                    Score = i.RelevanceScore,
                    Type = i.Type
                })];
                
            foreach (SimilarEntity similar in similars) 
            {
                logger.Information("Found similar: {Name} ({Score})", similar.Name, similar.Score);
            }

            authorsWithSimilars.Add(new AuthorWithSimilars
            {
                Name = author.Name.Trim(),
                Type = author.Type.ToLowerInvariant().Trim(),
                Similars = similars
            });
        }

        return authorsWithSimilars;
    }
    
    private async Task<List<EntityWithSimilars>> processOrganisations(TempExtractedMetadata tempMetadata) 
    {
        string[] tempOrganisations = tempMetadata.Organisations?.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToArray() ?? [];
        List<EntityWithSimilars> organisationsWithSimilars = [];
        
        foreach (string organisation in tempOrganisations) 
        {
            logger.Information("Finding similars for organisation '{Organisation}'", organisation);

            // Search for similars in the database
            HybridSearchResult result = await searchService.SearchAsync(
                organisation.Replace(".", ""),
                1,
                3,
                new Dictionary<string, object?> { { "type", new string[] { "organisation" } } }
            );
            
            // Filter out low-confidence matches (below 50%) and map to SimilarEntity
            List<SimilarEntity> similars = [.. result.Items
                .Where(i => i.RelevanceScore >= 0.5f) // Only show matches with 50%+ confidence
                .Select(i => new SimilarEntity
                {
                    Id = i.Id,
                    Name = i.Name,
                    Score = i.RelevanceScore,
                    Type = i.Type
                })];
                
            foreach (SimilarEntity similar in similars) 
            {
                logger.Information("Found similar: {Name} ({Score})", similar.Name, similar.Score);
            }
            
            organisationsWithSimilars.Add(new EntityWithSimilars
            {
                Name = organisation,
                Type = "organisation",
                Similars = similars
            });
        }

        return organisationsWithSimilars;
    }
    
    private async Task<List<EntityWithSimilars>> processRelatedPersons(TempExtractedMetadata tempMetadata) 
    {
        string[] tempRelatedPersons = tempMetadata.RelatedPersons?.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToArray() ?? [];
        List<EntityWithSimilars> relatedPersonsWithSimilars = [];

        foreach (string person in tempRelatedPersons) 
        {
            logger.Information("Finding similars for related person '{Person}'", person);
            
            // Search for similars in the database
            HybridSearchResult result = await searchService.SearchAsync(
                person.Replace(".", ""),
                1,
                3,
                new Dictionary<string, object?> { { "type", new string[] { "person" } } }
            );
            
            // Filter out low-confidence matches (below 50%) and map to SimilarEntity
            List<SimilarEntity> similars = [.. result.Items
                .Where(i => i.RelevanceScore >= 0.5f) // Only show matches with 50%+ confidence
                .Select(i => new SimilarEntity
                {
                    Id = i.Id,
                    Name = i.Name,
                    Score = i.RelevanceScore,
                    Type = i.Type
                })];
                
            foreach (SimilarEntity similar in similars) 
            {
                logger.Information("Found similar: {Name} ({Score})", similar.Name, similar.Score);
            }
            
            relatedPersonsWithSimilars.Add(new EntityWithSimilars
            {
                Name = person,
                Type = "person",
                Similars = similars
            });
        }

        return relatedPersonsWithSimilars;
    }
    
    #endregion

}

/// <summary>
/// Temporary class for deserializing LLM JSON response (before enrichment with similars)
/// </summary>
internal class TempExtractedMetadata
{
    public string? Title { get; set; }
    public string? Abstract { get; set; }
    public string? Description { get; set; }
    public string? PublicationDate { get; set; }  // String to support partial dates (YYYY, YYYY-MM, YYYY-MM-DD)
    public string? LanguageCode { get; set; }
    public List<TempAuthor> Authors { get; set; } = [];
    public List<string> Organisations { get; set; } = [];
    public List<string> RelatedPersons { get; set; } = [];
    public string? PublicationCode { get; set; }
    public List<string> Tags { get; set; } = [];
}

/// <summary>
/// Temporary class for author with type information from LLM
/// </summary>
internal class TempAuthor
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "person" or "organisation"
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


