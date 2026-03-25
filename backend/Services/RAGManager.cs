using System.Text;
using System.Text.Json;
using HandlebarsDotNet;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using OpenAI.Chat;
using Serilog;
using System.ClientModel;
using KnowledgeBank.Services.Search.Models;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Services.Vector;

namespace KnowledgeBank.Services;

public class RAGManager(ResourceManager resourceManager, RAGSystem ragSystem, HybridSearchService searchService, IVectorStore vectorStore, TextExtractionService textExtractionService)
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
    /// 4. Vector Storage: Store the chunks and their corresponding embeddings in the PostgreSQL vector database
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
    /// <returns>A tuple of the enhanced metadata string and the resource object</returns>
    private async Task<(string Metadata, Resource? Resource)> BuildRichMetadataChunk(Guid id)
    {
        try
        {
            // Fetch additional metadata from database with nested entities
            var resource = await resourceManager.GetResourceAsync(r => r.Id == id, includeProperties: new[]
            {
                "ResourceAuthorRelations.Author",
                "ResourceTagRelations.Tag",
                "ResourceOrganisationRelations.Organisation",
                "ResourceRegionRelations.Region",
                "ResourceType"
            });

            if (resource == null)
            {
                logger.Warning("Resource {Id} not found for metadata enrichment, returning empty metadata.", id);
                return (string.Empty, null);
            }

            var metadataBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(resource.Title))
                metadataBuilder.AppendLine($"Title: {resource.Title}");

            if (!string.IsNullOrEmpty(resource.Description))
                metadataBuilder.AppendLine($"Description: {resource.Description}");

            if (resource.ResourceType != null)
                metadataBuilder.AppendLine($"Type: {resource.ResourceType.Name}");

            if (resource.PublicationDate.HasValue)
                metadataBuilder.AppendLine($"Publication Date: {resource.PublicationDate.Value:yyyy-MM-dd}");

            if (!string.IsNullOrEmpty(resource.LanguageCode))
                metadataBuilder.AppendLine($"Language: {resource.LanguageCode}");

            if (!string.IsNullOrEmpty(resource.License))
                metadataBuilder.AppendLine($"License: {resource.License}");

            if (!string.IsNullOrEmpty(resource.Note))
                metadataBuilder.AppendLine($"Note: {resource.Note}");

            if (resource.ResourceAuthorRelations?.Any() == true)
            {
                var authorNames = resource.ResourceAuthorRelations
                    .Where(r => r.Author != null)
                    .Select(r => r.Author!.Name)
                    .ToList();
                if (authorNames.Any())
                    metadataBuilder.AppendLine($"Authors: {string.Join(", ", authorNames)}");
            }

            if (resource.ResourceTagRelations?.Any() == true)
            {
                var tagNames = resource.ResourceTagRelations
                    .Where(r => r.Tag != null)
                    .Select(r => r.Tag!.Name)
                    .ToList();
                if (tagNames.Any())
                    metadataBuilder.AppendLine($"Tags: {string.Join(", ", tagNames)}");
            }

            if (resource.ResourceOrganisationRelations?.Any() == true)
            {
                var orgNames = resource.ResourceOrganisationRelations
                    .Where(r => r.Organisation != null)
                    .Select(r => r.Organisation!.Name)
                    .ToList();
                if (orgNames.Any())
                    metadataBuilder.AppendLine($"Organizations: {string.Join(", ", orgNames)}");
            }

            if (resource.ResourceRegionRelations?.Any() == true)
            {
                var regionNames = resource.ResourceRegionRelations
                    .Where(r => r.Region != null)
                    .Select(r => r.Region!.Name)
                    .ToList();
                if (regionNames.Any())
                    metadataBuilder.AppendLine($"Regions: {string.Join(", ", regionNames)}");
            }

            logger.Information("Built rich metadata chunk for resource {Id}", id);
            return (metadataBuilder.ToString(), resource);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to build rich metadata for resource {Id}, returning empty metadata.", id);
            return (string.Empty, null);
        }
    }

    public async Task ResourcePipeline(Guid id, string? fileType = null, Stream? fileStream = null)
    {
        logger.Information("Resource RAG pipeline started for resource ID: {Id}", id);

        try
        {
            // Build a rich metadata chunk for better retrieval
            var (richMetadata, resource) = await BuildRichMetadataChunk(id);

            // Initialize chunks collection with the enhanced metadata chunk
            List<string> chunks = [richMetadata];

            // * STEP 1: Content Extraction
            logger.Information("Extracting content for resource ID: {Id}", id);

            if (fileStream != null)
            {
                // Normalize extension to include leading dot
                string ext = fileType != null
                    ? (fileType.StartsWith('.') ? fileType : $".{fileType}")
                    : ".pdf";

                // Extract text from file
                string extractedText = await textExtractionService.ExtractTextFromFileAsync(fileStream, ext);

                if (string.IsNullOrEmpty(extractedText))
                {
                    logger.Warning("No text extracted from the document");
                    return;
                }

                chunks.AddRange(ragSystem.Toolbox.SplitTextIntoChunks(extractedText, logChunks: false, markdownSplit: true));
                logger.Information("Successfully extracted and chunked text into {ChunkCount} segments for resource ID: {Id}", chunks.Count, id);
            }
            else if (resource?.FileType == "website" && !string.IsNullOrEmpty(resource.SourceUrl))
            {
                // Scrape website content
                logger.Information("Scraping website content for resource ID: {Id}", id);
                ReadabilityResult readabilityResult = await textExtractionService.ExtractTextFromWebAsync(resource.SourceUrl);

                if (!string.IsNullOrWhiteSpace(readabilityResult.TextContent))
                {
                    chunks.AddRange(ragSystem.Toolbox.SplitTextIntoChunks(readabilityResult.TextContent, logChunks: false, markdownSplit: false));
                    logger.Information("Successfully scraped and chunked website into {ChunkCount} segments for resource ID: {Id}", chunks.Count, id);
                }
                else
                {
                    logger.Warning("No text extracted from website for resource ID: {Id}", id);
                }
            }

            // * STEP 3 & 4: Vector Embedding Generation and Storage
            // Generate embeddings for each chunk and store them in the vector database
            logger.Information("Generating vector embeddings and storing {ChunkCount} chunks for resource ID: {Id}", chunks.Count, id);
            await vectorStore.DeletePointsByResourceIdAsync(id);
            var chunkData = chunks.Select((text, index) => (
                Text: text,
                Type: index == 0 ? ChunkType.MetaData : ChunkType.ContentText,
                Part: index
            )).ToList();
            await vectorStore.CreateResourcePointsAsync(id, chunkData);

            logger.Information("RAG pipeline completed successfully for resource ID: {Id}", id);
        }
        catch (Exception ex)
        {
            // Todo: Add a way to notify the user that the pipeline failed, with some options to retry.

            logger.Error(ex, "An error occurred while processing the document for resource ID: {Id}. Pipeline execution failed.", id);
            throw;
        }
    }

    public async Task EntityPipeline(Guid id, string chunk)
    {
        logger.Information("Entity RAG pipeline started for entity ID: {Id}", id);

        try
        {
            await vectorStore.DeletePointsByEntityIdAsync(id);
            var chunkData = new List<(string Text, ChunkType Type, int Part)>
            {
                (chunk, ChunkType.MetaData, 0)
            };

            await vectorStore.CreateEntityPointsAsync(id, chunkData);

            logger.Information("Entity RAG pipeline completed for entity ID: {Id}", id);
        }
        catch (Exception ex)
        {
            // TODO: Add a way to notify the user that the pipeline failed, with some options to retry

            logger.Error(ex, "An error occured while processing entity ID: {Id}", id);
            throw;
        }
    }

    public Task EntityPipeline(Person person)
    {
        StringBuilder sb = new();

        sb.AppendLine($"Person: {person.Name}");
        if (!string.IsNullOrWhiteSpace(person.Occupation)) sb.AppendLine($"Occupation: {person.Occupation}");
        if (!string.IsNullOrWhiteSpace(person.EmailAddress)) sb.AppendLine($"Email: {person.EmailAddress}");
        if (!string.IsNullOrWhiteSpace(person.Description)) sb.AppendLine($"Description: {person.Description}");

        return EntityPipeline(person.Id, sb.ToString());
    }

    public async Task PersonEntityPipeline(Guid id)
    {
        Person? person = await resourceManager.GetPersonAsync(id);
        await EntityPipeline(person!);
    }

    public Task EntityPipeline(Organisation organisation)
    {
        StringBuilder sb = new();

        sb.AppendLine($"Organisation: {organisation.Name}");
        if (!string.IsNullOrWhiteSpace(organisation.Website)) sb.AppendLine($"Website: {organisation.Website}");
        if (!string.IsNullOrWhiteSpace(organisation.EmailAddress)) sb.AppendLine($"Email: {organisation.EmailAddress}");
        if (!string.IsNullOrWhiteSpace(organisation.Description)) sb.AppendLine($"Description: {organisation.Description}");

        return EntityPipeline(organisation.Id, sb.ToString());
    }

    public async Task OrganisationEntityPipeline(Guid id)
    {
        Organisation? organisation = await resourceManager.GetOrganisationAsync(id);
        await EntityPipeline(organisation!);
    }

    #region Metadata Updates

    /// <summary>
    /// Updates the metadata point in the vector database when resource metadata changes.
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
            var (richMetadata, _) = await BuildRichMetadataChunk(id);

            // Update the vector database
            bool success = await vectorStore.UpdateMetadataPointAsync(id, richMetadata);

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

    private const string metadataExtractionRules = $@"
    
        Extract the following information in JSON format:
        {{
            ""title"": ""The document title"",
            ""abstract"": ""The abstract of the paper when it is a scientific paper, else leave empty"",
            ""description"": ""A complete and concise description of the document (50-300 words)"",
            ""publicationDate"": ""YYYY, YYYY-MM, or YYYY-MM-DD format (use most specific format available, or null)"",
            ""languageCode"": ""ISO 639-1 two-letter code (e.g., 'en', 'nl', 'fr', etc.)"",
            ""authors"": [{{""name"": ""Author name"", ""type"": ""person or organisation""}}],
            ""organisations"": [""String: organization name 1"", ""String: organization name 2""],
            ""relatedPersons"": [""String: person name 1"", ""String: person name 2""],
            ""publicationCode"": ""DOI, ISBN, arXiv ID, etc. or null"",
            ""tags"": [""String: tag 1"", ""String: tag 2""]
        }}

        IMPORTANT: organisations, relatedPersons, and tags are arrays of STRING values only, NOT objects.
        Only authors uses the object format with name and type fields.

        Rules:
        - IMPORTANT: Only extract information that is explicitly present in the document text. Do not infer, guess, or assume values. If a field cannot be clearly found in the document, use null or an empty array.
        - If a field cannot be determined, use null or empty array
        - Language code must be 2 letters lowercase (From the ISO 639-1 list)
        - Publication date can be partial: YYYY (year only), YYYY-MM (year and month), or YYYY-MM-DD (full date). Use the most specific format you can determine from the document.
        - If the document uses relative dates (""today"", ""yesterday"", ""vandaag"", ""gisteren"", etc.), calculate the actual date using the Current date provided above
        - Note which type of publication code it is before the actual publication code
        - Make sure tags are capitalized, so they look good
        - **DEDUPLICATION RULE**: Each entity (person/organization) must appear ONLY ONCE per list
            * If a shortened and a full version of the same person's name appear, include only the full version
            * If the same name appears multiple times in the document, include it ONLY ONCE in output
            * If an abbreviated and full organization name refer to the same entity, include only the more complete version
            * Always prefer the most complete version when you encounter variations of the same entity
            * Check each name before adding - if it's already in the list (even with slight variation), don't add it again
        - **CRITICAL NAME FORMATTING RULE**: All person names MUST follow the format: Given name(s) FIRST, Family name LAST
            * If the document shows a name in ""Last, First"" format (common in citations or references), reorder it to ""First Last""
            * NEVER preserve comma-separated ""Last, First"" format from citations or references
            * Always reorder names from the actual document to: [Given name] [Family name]
        - When the full name is available, prefer it over initials or shortened forms

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
    
    /// <summary>
    /// Extracts metadata from document text using LLM analysis
    /// </summary>
    /// <param name="text">The full text extracted from the document</param>
    /// <param name="fileName">The original filename for context</param>
    /// <param name="progressCallback">Optional callback invoked during processing to report progress</param>
    /// <returns>Extracted metadata or null if extraction fails</returns>
    public async Task<ExtractedMetadata?> ExtractMetadataFromFileAsync(string text, string fileName, Action? progressCallback = null)
    {
        logger.Information("Starting document metadata extraction for: {file}", fileName);

        // Make text fit in context window of LLM
        string textToAnalyze = trimTextForFile(text);

        logger.Information("Analyzing {Length} characters of text", textToAnalyze.Length);

        // Creating the prompt for documents
        string prompt = $@"
            You are a metadata extraction assistant. Analyze the following document text and extract structured metadata.

            Current date: {DateTime.UtcNow:yyyy-MM-dd}

            Filename: {fileName}

            Document text:
            {textToAnalyze}
        " + metadataExtractionRules;

        // Extract metadata using LLM (this is raw metadata that needs to be processed)
        TempExtractedMetadata? tempMetadata = await extractMetadataWithLLM(prompt);

        if (tempMetadata == null)
        {
            logger.Warning("Failed to deserialize metadata from LLM response");
            return null;
        }

        // Notify that we're now finding similar entities
        progressCallback?.Invoke();

        // Validate and process the metadata
        return await validateAndProcessMetadata(tempMetadata);
    }
    
    public async Task<ExtractedMetadata?> ExtractMetadataFromWebAsync(ReadabilityResult readabilityResult, string url, Action? progressCallback = null) 
    {
        if (string.IsNullOrWhiteSpace(readabilityResult.TextContent)) 
        {
            logger.Warning("No text content to be analyzed!");
            return null;
        }
    
        logger.Information("Starting web metadata extraction for: " + readabilityResult.SiteName);

        // Use more text if Readability failed to extract the key structured fields,
        // since the LLM will have to find title, author, and date from the body text alone.
        bool readabilitySucceeded = !string.IsNullOrWhiteSpace(readabilityResult.Title)
            || !string.IsNullOrWhiteSpace(readabilityResult.Byline)
            || !string.IsNullOrWhiteSpace(readabilityResult.SiteName);
        string textToAnalyze = readabilitySucceeded
            ? trimTextForWeb(readabilityResult.TextContent)
            : trimTextForFile(readabilityResult.TextContent);
        
        logger.Information("Analyzing {Length} characters of text", textToAnalyze.Length);

        // Creating the prompt for web
        string prompt = $@"
            You are a metadata extraction assistant. Analyze the following web page and extract structured metadata.

            Current date: {DateTime.UtcNow:yyyy-MM-dd}

            URL:
            {url}

            The text was extracted using Readability or reading the inner text. The following information was already collected and should be preferred over anything else you can find:
             - Title: {readabilityResult.Title ?? "Title was not extracted - Please identify from text"}
             - Byline: {readabilityResult.Byline ?? "Byline was not extracted - Please identify from text"}
             - Excerpt: {readabilityResult.Excerpt ?? "Excerpt was not extracted - Please identify from text"}
             - SiteName: {readabilityResult.SiteName ?? "SiteName was not extracted - Please identify from text"}
             
            **CRITICAL BYLINE RULE FOR ORGANIZATIONAL AUTHORSHIP**:

            If the Byline is EMPTY or was not extracted:
             → This indicates organizational authorship (no individual journalist credited)
             → The AUTHOR is the publishing organization itself
             → Find the publisher name by checking: (1) the SiteName provided above, (2) branding at the START of the document
             → The organization name might be the full SiteName (with domain), or just the base name (without domain extension)
             → Author type = ""organisation""
             → CRITICAL: Organizations mentioned in the article content (quoted sources, subjects) are NOT authors - add them to organisations or relatedPersons instead
             → When in doubt with empty byline, default to deriving the author from SiteName

            If the Byline is NOT EMPTY and indicates the article was written by an internal team, staff, or department of the publishing organization (in any language):
             → The author is the publishing organization itself
             → Author type = ""organisation""
             → Look for the actual organization name in the document (headers, prominent mentions, branding)
             → Use the SiteName provided above as a strong hint about which organization to identify
             → The organization name might be the full SiteName (with domain), or just the base name (without domain extension)
             → DO NOT use the byline text literally as the author name (it's a department, not the organization)
             → DO NOT invent organization names not found in the document or SiteName

            Common indicators (not exhaustive): possessive pronouns + department/team/editorial references.

            Extracted text:
            {textToAnalyze}

            ===== IMPORTANT CONTEXT FOR THIS SPECIFIC ARTICLE =====
            Byline status: {(string.IsNullOrWhiteSpace(readabilityResult.Byline) ? "EMPTY - Use publisher as author" : $"Present: {readabilityResult.Byline}")}
            Publisher (SiteName): {readabilityResult.SiteName ?? "Unknown"}

            {(string.IsNullOrWhiteSpace(readabilityResult.Byline) ?
                "⚠️ CRITICAL: Since Byline is EMPTY, the author MUST be the publisher organization. Do NOT use organizations mentioned in the article text (like quoted sources) as authors. The author is the publishing organization only." :
                "")}
            =======================================================
        " + metadataExtractionRules;

        // Extract metadat using LLM (this is raw metadata that needs to be processed)
        TempExtractedMetadata? tempMetadata = await extractMetadataWithLLM(prompt);

        if (tempMetadata == null)
        {
            logger.Warning("Failed to deserialize metadata from LLM response");
            return null;
        }

        // Notify that we're now finding similar entities
        progressCallback?.Invoke();

        // Validate and process the metadata
        return await validateAndProcessMetadata(tempMetadata);
    }

    /// <summary>
    /// Omits the middle of very long documents to make it fit in the context of the LLM.
    /// We keep the start and end, because these often contain abstracts, authors and other metadata.
    /// </summary>
    private string trimTextForFile(string text)
    {
        const int firstChars = 16000;  // ~4K tokens - captures intro, abstract, authors, publication info
        const int lastChars = 4000;    // ~1K tokens - captures references, acknowledgments
        return trimText(text, firstChars, lastChars);
    }

    /// <summary>
    /// Trims web page text for metadata extraction. Uses smaller limits since Readability
    /// already extracts title, byline, excerpt, and sitename separately.
    /// </summary>
    private string trimTextForWeb(string text)
    {
        const int firstChars = 8000;  // ~2K tokens - captures opening paragraphs and key metadata
        const int lastChars = 0;      // Not needed - web article metadata is always near the top
        return trimText(text, firstChars, lastChars);
    }

    private static string trimText(string text, int firstChars, int lastChars)
    {
        int maxTotal = firstChars + lastChars;

        if (text.Length <= maxTotal)
            return text;

        string beginning = text.Substring(0, firstChars);

        if (lastChars == 0)
            return beginning + "\n\n[...rest of document omitted...]";

        string ending = text.Substring(text.Length - lastChars);
        return beginning + "\n\n[...middle section omitted...]\n\n" + ending;
    }

    private readonly static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true};
    private async Task<TempExtractedMetadata?> extractMetadataWithLLM(string prompt)
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
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("MetadataExtraction", BinaryData.FromString(Prompts.MetadataExtractionOutputJsonSchema))
        };

        // Run chat with retry logic for rate limits
        logger.Information("Calling LLM for metadata extraction");

        const int maxRetries = 3;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                ClientResult<ChatCompletion> response = await ragSystem.ChatClient.CompleteChatAsync(messages, chatOptions);

                // Extract and parse JSON
                string jsonContent = response.Value.Content[0].Text;
                logger.Information("Received LLM response: {Length} characters", jsonContent.Length);
                TempExtractedMetadata? tempMetadata = JsonSerializer.Deserialize<TempExtractedMetadata>(jsonContent, jsonSerializerOptions);

                return tempMetadata;
            }
            catch (System.ClientModel.ClientResultException ex) when (ex.Message.Contains("429") || ex.Message.Contains("RateLimitReached"))
            {
                retryCount++;
                int waitSeconds = 60 * retryCount; // 60s, 120s, 180s

                if (retryCount >= maxRetries)
                {
                    logger.Error(ex, "Rate limit exceeded and max retries reached for metadata extraction");
                    throw;
                }

                logger.Warning("Rate limit hit (429). Waiting {WaitSeconds} seconds before retry {RetryCount}/{MaxRetries}", waitSeconds, retryCount, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
            }
        }

        return null;
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
