using System.ClientModel;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Utils;
using OpenAI.Chat;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Serilog;
using static Qdrant.Client.Grpc.Conditions;


namespace KnowledgeBank.Services;

// enum for resource types
public enum ChunkType { ContentText, MetaData }


public class RAGSystem
{
    private readonly Serilog.ILogger _logger;
    private readonly EnvironmentConfig _environmentConfig;

    // Query embedding cache for improved performance
    private readonly Dictionary<string, (float[] Embedding, DateTime CachedAt)> _embeddingCache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);
    private readonly object _cacheLock = new object();
    private const int MaxCacheSize = 1000;

    // RAG System components:
    public Tools Toolbox { get; private set; }
    public QdrantClient QdrantClient { get; private set; }
    public ulong EmbeddingsDimensions { get; private set; }
    public string CollectionName { get; private set; }
    public DocumentIntelligenceClient DocumentIntelligenceClient { get; private set; }
    public EmbeddingsClient EmbeddingsClient { get; private set; }
    public ChatClient ChatClient { get; private set; }



    /// <summary>
    /// Initializes a new instance of the RAGSystem class with the specified blob service and environment configuration.
    /// Sets up all required components for the Retrieval-Augmented Generation system including vector database,
    /// document intelligence, chat completions, and embeddings services.
    /// </summary>
    /// <param name="blobService">The Azure Blob service instance for file storage operations</param>
    /// <param name="environmentConfig">The environment configuration containing all necessary API keys, endpoints, and settings</param>
    /// <param name="documentIntelligenceClient">The Azure Document Intelligence client for document processing</param>
    /// <remarks>
    /// This constructor performs the following initialization steps:
    /// <list type="bullet">
    /// <item><description>Configures logging with Serilog context</description></item>
    /// <item><description>Initializes the Tools toolbox</description></item>
    /// <item><description>Sets up Qdrant vector database client with collection management</description></item>
    /// <item><description>Configures Azure Document Intelligence client for document processing</description></item>
    /// <item><description>Establishes Azure OpenAI chat client for conversational AI</description></item>
    /// <item><description>Initializes embeddings client for vector generation</description></item>
    /// </list>
    /// The constructor ensures that the Qdrant collection exists and payload fields are properly indexed.
    /// All async operations are executed synchronously during initialization.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when environment configuration values are invalid or missing</exception>
    /// <exception cref="HttpRequestException">Thrown when API endpoints are unreachable during initialization</exception>
    public RAGSystem(IAzureBlobService blobService, EnvironmentConfig environmentConfig, DocumentIntelligenceClient documentIntelligenceClient)
    {
        _logger = Log.ForContext<RAGSystem>();
        Toolbox = new Tools();
        _environmentConfig = environmentConfig;
        _embeddingCache = new Dictionary<string, (float[], DateTime)>();

        // * RAG System Initialization
        _logger.Information("Initializing RAG system with embedding cache enabled");


        // * Qdrant (Vector Database) Initialization
        EmbeddingsDimensions = ulong.Parse(_environmentConfig.GetVariableValue(EnvironmentVariable.QDRANT_EMBEDDINGS_DIMENSIONS));
        CollectionName = _environmentConfig.GetVariableValue(EnvironmentVariable.QDRANT_COLLECTION_NAME);

        string qdrantApiKey = _environmentConfig.GetVariableValue(EnvironmentVariable.QDRANT_API_KEY);

        QdrantClient = new QdrantClient(
            host: _environmentConfig.GetVariableValue(EnvironmentVariable.QDRANT_HOST),
            https: bool.Parse(_environmentConfig.GetVariableValue(EnvironmentVariable.QDRANT_HTTPS)),
            apiKey: qdrantApiKey.Equals("null", StringComparison.OrdinalIgnoreCase) ? null : qdrantApiKey
        );
        _logger.Information("Qdrant client successfully initialized with host: {Host}, port: {Port}", "qdrant", 6334);

        // Ensure the Qdrant collection exists
        InitializeCollectionAsync().GetAwaiter().GetResult();
        _logger.Information("Qdrant collection {CollectionName} checked and initialized if necessary", CollectionName);

        // Ensure the payload fields are indexed
        IndexPayloadFieldsAsync().GetAwaiter().GetResult();


        // * Document Intelligence
        DocumentIntelligenceClient = documentIntelligenceClient;
        _logger.Information("Azure Document Intelligence client initialized");


        // * Chat Completions - Azure OpenAI
        Uri azureOpenAiEndpoint = new Uri(_environmentConfig.GetVariableValue(EnvironmentVariable.AZURE_OPENAI_CLIENT_ENDPOINT));
        ApiKeyCredential azureOpenAiApiKeyCredential = new ApiKeyCredential(_environmentConfig.GetVariableValue(EnvironmentVariable.AZURE_OPENAI_CLIENT_API_KEY));
        AzureOpenAIClient AzureOpenAIClient = new AzureOpenAIClient(azureOpenAiEndpoint, azureOpenAiApiKeyCredential);
        string chatDeploymentName = _environmentConfig.GetVariableValue(EnvironmentVariable.CHAT_DEPLOYMENT_NAME);
        ChatClient = AzureOpenAIClient.GetChatClient(chatDeploymentName);
        _logger.Information("Azure OpenAI client successfully initialized with deployment: {DeploymentName} at endpoint: {Endpoint}", chatDeploymentName, azureOpenAiEndpoint);


        // * Embeddings
        Uri embeddingsEndpoint = new Uri(_environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_CLIENT_ENDPOINT));
        AzureKeyCredential embeddingsKeyCredential = new AzureKeyCredential(_environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_CLIENT_API_KEY));
        string embeddingsModelName = _environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_MODEL_NAME);
        EmbeddingsClient = new EmbeddingsClient(embeddingsEndpoint, embeddingsKeyCredential);
        _logger.Information("Embeddings client successfully initialized with model: {ModelName} at endpoint: {Endpoint}, configured dimensions: {Dimensions}",
            embeddingsModelName, embeddingsEndpoint, EmbeddingsDimensions);

        _logger.Information("RAG system successfully initialized");
    }



    /// <summary>
    /// Initializes the Qdrant collection for the RAG system if it doesn't already exist.
    /// Creates a new collection with cosine distance similarity and HNSW indexing configuration,
    /// or logs that the collection already exists if it's already present.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of initializing the collection.</returns>
    /// <remarks>
    /// The collection is created with the following configuration:
    /// - Strict mode enabled with unindexed filtering disabled
    /// - Vector parameters using cosine distance similarity
    /// - HNSW configuration with M=16, EfConstruct=100, and FullScanThreshold=1000
    /// If the collection already exists, no action is taken and an informational log is recorded.
    /// </remarks>
    public async Task InitializeCollectionAsync()
    {
        bool exists = await QdrantClient.CollectionExistsAsync(CollectionName);

        if (!exists)
        {
            _logger.Information("Collection {CollectionName} does not exist. Creating it now.", CollectionName);

            // Configration:
            StrictModeConfig strictModeConfig = new StrictModeConfig
            {
                Enabled = true,
                UnindexedFilteringRetrieve = false // Ensure strict mode is enabled
            };

            VectorParams vectorParams = new VectorParams
            {
                Size = EmbeddingsDimensions,
                Distance = Distance.Cosine,
                HnswConfig = new HnswConfigDiff
                {
                    M = 16,
                    EfConstruct = 100,
                    FullScanThreshold = 1000,
                }
            };

            await QdrantClient.CreateCollectionAsync(
                collectionName: CollectionName,
                strictModeConfig: strictModeConfig,
                vectorsConfig: vectorParams
            );
        }
        else
        {
            _logger.Information("Collection {CollectionName} already exists. No action taken.", CollectionName);
        }
    }



    /// <summary>
    /// Creates payload indexes for the Qdrant collection to optimize search performance.
    /// This method sets up two indexes: one for resourceId field and another for full-text search on chunkText field.
    /// </summary>
    /// <returns>A task that represents the asynchronous indexing operation.</returns>
    /// <remarks>
    /// The method creates:
    /// - A standard index on the "resourceId" field for efficient filtering by resource identifier
    /// - A full-text search index on the "chunkText" field with multilingual tokenization support,
    ///   configured with token length constraints (2-10 characters) and lowercase normalization
    /// </remarks>
    public async Task IndexPayloadFieldsAsync()
    {
        // Ensure the collection exists before creating indexes

        // Standard index for resourceId
        await QdrantClient.CreatePayloadIndexAsync(
            collectionName: CollectionName,
            fieldName: "resourceId"
        );

        // Full text index for chunkText
        await QdrantClient.CreatePayloadIndexAsync(
            collectionName: CollectionName,
            fieldName: "chunkText",
            schemaType: PayloadSchemaType.Text,
            indexParams: new PayloadIndexParams
            {
                TextIndexParams = new TextIndexParams
                {
                    Tokenizer = TokenizerType.Multilingual,
                    MinTokenLen = 2,
                    MaxTokenLen = 10,
                    Lowercase = true
                }
            }
        );
    }



    /// <summary>
    /// Generates a vector embedding for the specified query string using the configured embeddings model.
    /// </summary>
    /// <param name="query">The input text query to generate an embedding for. Cannot be null or whitespace.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a float array representing the embedding vector.</returns>
    /// <exception cref="ArgumentException">Thrown when the query parameter is null, empty, or contains only whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the embedding generation fails or returns null/empty data.</exception>
    /// <remarks>
    /// This method uses the embeddings model specified in the environment configuration to generate
    /// a numerical vector representation of the input query. The embedding can be used for semantic
    /// search, similarity comparison, and other natural language processing tasks.
    /// </remarks>
    public async Task<float[]> GenerateEmbedding(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.Warning("Query is null or empty. Cannot generate embedding.");
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        // Normalize query for cache key (lowercase, trim)
        string cacheKey = query.Trim().ToLowerInvariant();

        // Check cache first
        lock (_cacheLock)
        {
            if (_embeddingCache.TryGetValue(cacheKey, out var cached))
            {
                // Check if cache entry is still valid
                if (DateTime.UtcNow - cached.CachedAt < _cacheExpiration)
                {
                    _logger.Debug("Cache hit for query embedding");
                    return cached.Embedding;
                }
                else
                {
                    // Remove expired entry
                    _embeddingCache.Remove(cacheKey);
                    _logger.Debug("Cache entry expired, removing");
                }
            }
        }

        // Generate new embedding
        EmbeddingsOptions requestOptions = new EmbeddingsOptions([query])
        {
            Model = _environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_MODEL_NAME),
        };

        Response<EmbeddingsResult> response = await EmbeddingsClient.EmbedAsync(requestOptions);

        float[]? embeddingData = response.Value.Data[0].Embedding.ToObjectFromJson<float[]>();
        if (embeddingData == null || embeddingData.Length == 0)
        {
            _logger.Warning("Generated embedding is null or empty.");
            throw new InvalidOperationException("Generated embedding is null or empty.");
        }

        // Log actual dimensions returned by the embeddings API
        if (embeddingData.Length != (int)EmbeddingsDimensions)
        {
            _logger.Warning("Embedding dimension mismatch! Expected: {Expected}, Actual: {Actual}, Model: {Model}",
                EmbeddingsDimensions, embeddingData.Length, _environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_MODEL_NAME));
        }

        // Store in cache
        lock (_cacheLock)
        {
            // Implement simple LRU: remove oldest entries if cache is full
            if (_embeddingCache.Count >= MaxCacheSize)
            {
                var oldestKey = _embeddingCache
                    .OrderBy(kvp => kvp.Value.CachedAt)
                    .First()
                    .Key;
                _embeddingCache.Remove(oldestKey);
                _logger.Debug("Cache full, removed oldest entry");
            }

            _embeddingCache[cacheKey] = (embeddingData, DateTime.UtcNow);
            _logger.Debug("Cached embedding for query. Cache size: {CacheSize}", _embeddingCache.Count);
        }

        _logger.Information("Successfully generated embedding");
        return embeddingData;
    }



    /// <summary>
    /// Generates embeddings for a collection of text chunks using the configured embeddings model.
    /// </summary>
    /// <param name="chunks">A list of text strings to generate embeddings for. Cannot be null or empty.</param>
    /// <returns>A <see cref="Response{EmbeddingsResult}"/> containing the generated embeddings for all input chunks.</returns>
    /// <exception cref="ArgumentException">Thrown when the chunks parameter is null or contains no elements.</exception>
    /// <remarks>
    /// This method uses the embeddings model specified in the environment configuration.
    /// The embedding generation is performed asynchronously and includes logging for both
    /// error conditions and successful operations.
    /// </remarks>
    public async Task<Response<EmbeddingsResult>> GenerateEmbeddings(List<string> chunks)
    {
        if (chunks == null || chunks.Count == 0)
        {
            _logger.Warning("Chunks list is null or empty. Cannot generate embeddings.");
            throw new ArgumentException("Chunks list cannot be null or empty.", nameof(chunks));
        }

        EmbeddingsOptions requestOptions = new EmbeddingsOptions(chunks)
        {
            Model = _environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_MODEL_NAME),
        };

        Response<EmbeddingsResult> responses = await EmbeddingsClient.EmbedAsync(requestOptions);
        _logger.Information("Successfully generated embeddings");

        return responses;
    }




    /// <summary>
    /// Creates vector points from text chunks and stores them in the Qdrant vector database.
    /// </summary>
    /// <param name="id">The unique identifier for the resource that the chunks belong to.</param>
    /// <param name="chunks">A list of text chunks to be converted into vector points. The first chunk is treated as metadata, subsequent chunks as content text.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method attempts to generate embeddings for the provided chunks using Azure OpenAI service.
    /// If embedding generation fails, it falls back to creating points with placeholder vectors.
    /// Each point includes a custom payload containing resource ID, chunk type, chunk text, and chunk part index.
    /// The first chunk (index 0) is classified as MetaData, while subsequent chunks are classified as ContentText.
    /// All created points are upserted to the configured Qdrant collection.
    /// </remarks>
    /// <exception cref="Exception">Logs an error if embedding generation fails and continues with placeholder vectors.</exception>
    public async Task CreatePoints(Guid id, List<string> chunks)
    {
        _logger.Information("Creating points for resource ID: {ResourceId}", id);
        _logger.Debug("Chunks count: {ChunksCount}", chunks.Count);

        List<PointStruct> pointsList = [];
        const int batchSize = 96; // Maximum allowed by the API
        const int maxRetries = 3;
        int totalProcessed = 0;
        var failedChunks = new List<(int Index, string Text)>();

        for (int i = 0; i < chunks.Count; i += batchSize)
        {
            var batch = chunks.Skip(i).Take(batchSize).ToList();
            int retryCount = 0;
            bool success = false;

            // Retry logic with exponential backoff
            while (retryCount < maxRetries && !success)
            {
                try
                {
                    _logger.Debug("Processing batch starting at chunk {StartIndex}, retry {RetryCount}",
                        i, retryCount);

                    Response<EmbeddingsResult> response = await GenerateEmbeddings(batch);

                    foreach (EmbeddingItem item in response.Value.Data)
                    {
                        float[]? embeddingData = item.Embedding.ToObjectFromJson<float[]>();

                        if (embeddingData == null || embeddingData.Length == 0)
                        {
                            _logger.Warning("Empty embedding received for chunk at index {Index}",
                                totalProcessed + item.Index);
                            failedChunks.Add((totalProcessed + item.Index, batch[item.Index]));
                            continue;
                        }

                        int actualIndex = totalProcessed + item.Index;
                        string ChunkTypeString = actualIndex == 0 ? ChunkType.MetaData.ToString() : ChunkType.ContentText.ToString();

                        CustomPayload customPayload = new()
                        {
                            ResourceId = id.ToString(),
                            ChunkType = ChunkTypeString,
                            ChunkText = chunks[actualIndex],
                            ChunkPart = actualIndex
                        };

                        PointStruct point = new()
                        {
                            Id = Guid.NewGuid(),
                            Vectors = embeddingData
                        };
                        point.Payload.Add(customPayload.ToPayload());

                        pointsList.Add(point);
                    }

                    success = true;
                    _logger.Debug("Successfully processed batch at index {StartIndex}", i);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.Warning(ex, "Failed to generate embeddings for batch at index {StartIndex}, attempt {Attempt}/{MaxAttempts}",
                        i, retryCount, maxRetries);

                    if (retryCount < maxRetries)
                    {
                        // Exponential backoff: wait 2^retryCount seconds
                        int delaySeconds = (int)Math.Pow(2, retryCount);
                        _logger.Information("Waiting {Delay} seconds before retry...", delaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    }
                    else
                    {
                        _logger.Error("Max retries exceeded for batch at index {StartIndex}. Recording failed chunks.", i);

                        // Record all chunks in this failed batch
                        for (int j = 0; j < batch.Count; j++)
                        {
                            failedChunks.Add((totalProcessed + j, batch[j]));
                        }
                    }
                }
            }

            totalProcessed += batch.Count;
        }

        // Check if we have enough successful points
        if (pointsList.Count == 0)
        {
            _logger.Error("No embeddings were successfully generated for resource {ResourceId}. Aborting.", id);
            throw new InvalidOperationException(
                $"Failed to generate any embeddings for resource {id}. " +
                "Please check API connectivity and try again.");
        }

        if (failedChunks.Count > 0)
        {
            double failureRate = (double)failedChunks.Count / chunks.Count;
            _logger.Warning("Failed to generate embeddings for {FailedCount}/{TotalCount} chunks ({FailureRate:P1}) for resource {ResourceId}",
                failedChunks.Count, chunks.Count, failureRate, id);

            // If more than 50% failed, this is a serious problem
            if (failureRate > 0.5)
            {
                _logger.Error("More than 50% of chunks failed embedding generation. This may indicate a serious issue.");
            }
        }

        // Upsert the successfully embedded points
        _logger.Information("Upserting {PointCount} points to Qdrant for resource {ResourceId}",
            pointsList.Count, id);
        await QdrantClient.UpsertAsync(CollectionName, pointsList);

        _logger.Information("Successfully created {SuccessCount}/{TotalCount} points for resource {ResourceId}",
            pointsList.Count, chunks.Count, id);
    }



    /// <summary>
    /// Updates the metadata point in the vector database with new chunk text and corresponding embedding.
    /// </summary>
    /// <param name="id">The resource ID used to identify the metadata point to update.</param>
    /// <param name="newChunkText">The new chunk text to replace the existing metadata content.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a boolean value:
    /// true if the metadata point was successfully updated; otherwise, false.
    /// </returns>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Validates that both the resource ID and new chunk text are not null or empty
    /// 2. Queries the vector database to find the existing metadata point by resource ID and chunk type
    /// 3. Generates a new embedding vector for the provided chunk text
    /// 4. Updates the vector data for the found point
    /// 5. Overwrites the payload with the new chunk text
    /// The method returns false if any validation fails or if an exception occurs during the update process.
    /// </remarks>
    public async Task<bool> UpdateMetadataPointAsync(string id, string newChunkText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.Warning("Resource ID is null or empty. Cannot update metadata.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(newChunkText))
            {
                _logger.Warning("New chunk text is null or empty. Cannot update metadata.");
                return false;
            }

            _logger.Information("Updating metadata point for resource {ResourceId}", id);

            // Get the existing point ID
            IReadOnlyList<ScoredPoint> result = await QdrantClient.QueryAsync(
                 collectionName: CollectionName,
                 filter: MatchKeyword("resourceId", id) & MatchKeyword("chunkType", ChunkType.MetaData.ToString()),
                 limit: 1
            );

            if (result.Count == 0)
            {
                _logger.Warning("No metadata point found for resource {ResourceId}. Cannot update.", id);
                return false;
            }

            // Generate new embedding for the updated metadata
            float[] newEmbedding = await GenerateEmbedding(newChunkText);

            // Update the vector
            PointVectors pointVectors = new PointVectors
            {
                Id = result[0].Id,
                Vectors = newEmbedding,
            };

            await QdrantClient.UpdateVectorsAsync(
                collectionName: CollectionName,
                points: new List<PointVectors> { pointVectors }
            );

            // Update the payload with new chunk text
            await QdrantClient.OverwritePayloadAsync(
                collectionName: CollectionName,
                payload: new Dictionary<string, Value> { { "chunkText", newChunkText } },
                filter: MatchKeyword("resourceId", id) & MatchKeyword("chunkType", ChunkType.MetaData.ToString())
            );

            _logger.Information("Successfully updated metadata point for resource {ResourceId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update metadata point for resource {ResourceId}", id);
            return false;
        }
    }



    /// <summary>
    /// Deletes all vector points associated with a specific resource ID from the Qdrant collection.
    /// </summary>
    /// <param name="id">The resource ID used to identify and delete associated vector points. Cannot be null or whitespace.</param>
    /// <returns>
    /// A task that represents the asynchronous delete operation. The task result contains:
    /// <c>true</c> if the deletion was successful; otherwise, <c>false</c> if the ID is invalid or an error occurred.
    /// </returns>
    /// <remarks>
    /// This method filters points by the "resourceId" field and removes all matching entries from the collection.
    /// All operations are logged for monitoring and debugging purposes.
    /// </remarks>
    public async Task<bool> DeleteAllPointsWithIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.Warning("Resource ID is null or empty. Cannot delete chunks for ResourceId: {ResourceId}", id);
            return false;
        }

        try
        {
            var deleteResult = await QdrantClient.DeleteAsync(
                collectionName: CollectionName,
                filter: MatchKeyword("resourceId", id)
            );

            _logger.Information("Successfully deleted points for ResourceId: {ResourceId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete points for ResourceId: {ResourceId}", id);
            return false;
        }
    }



    /// <summary>
    /// Retrieves a chunk with its surrounding context (previous and next chunks) for better understanding.
    /// </summary>
    /// <param name="resourceId">The resource ID containing the chunk</param>
    /// <param name="chunkPart">The chunk part/index to retrieve with context</param>
    /// <param name="contextWindow">Number of chunks before and after to include (default: 1)</param>
    /// <returns>A tuple containing (previousChunks, mainChunk, nextChunks)</returns>
    /// <remarks>
    /// This method helps preserve context by retrieving neighboring chunks around the target chunk.
    /// This is especially useful when the main chunk alone doesn't provide enough context for understanding.
    /// </remarks>
    public async Task<(List<string> PreviousChunks, string MainChunk, List<string> NextChunks)> GetChunkWithContextAsync(
        string resourceId,
        int chunkPart,
        int contextWindow = 1)
    {
        try
        {
            var previousChunks = new List<string>();
            var nextChunks = new List<string>();
            string mainChunk = string.Empty;

            // Get all chunks for this resource and filter by chunk part in memory
            // This is more efficient than multiple individual queries
            var allChunks = await QdrantClient.QueryAsync(
                CollectionName,
                filter: MatchKeyword("resourceId", resourceId),
                limit: (uint)(chunkPart + contextWindow + 10) // Get enough chunks
            );

            if (allChunks.Count == 0)
            {
                _logger.Warning("No chunks found for resource {ResourceId}", resourceId);
                return (new List<string>(), string.Empty, new List<string>());
            }

            // Parse all chunks and organize by chunk part
            var chunkMap = new Dictionary<int, string>();
            foreach (var point in allChunks)
            {
                var payload = CustomPayload.FromPayload(point.Payload);
                chunkMap[payload.ChunkPart] = payload.ChunkText;
            }

            // Get main chunk
            if (chunkMap.ContainsKey(chunkPart))
            {
                mainChunk = chunkMap[chunkPart];
            }

            // Get previous chunks
            for (int i = chunkPart - contextWindow; i < chunkPart; i++)
            {
                if (i >= 0 && chunkMap.ContainsKey(i))
                {
                    previousChunks.Add(chunkMap[i]);
                }
            }

            // Get next chunks
            for (int i = chunkPart + 1; i <= chunkPart + contextWindow; i++)
            {
                if (chunkMap.ContainsKey(i))
                {
                    nextChunks.Add(chunkMap[i]);
                }
            }

            _logger.Debug("Retrieved chunk {ChunkPart} with context: {PrevCount} previous, {NextCount} next",
                chunkPart, previousChunks.Count, nextChunks.Count);

            return (previousChunks, mainChunk, nextChunks);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to retrieve chunk with context for ResourceId: {ResourceId}, ChunkPart: {ChunkPart}",
                resourceId, chunkPart);
            return (new List<string>(), string.Empty, new List<string>());
        }
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


