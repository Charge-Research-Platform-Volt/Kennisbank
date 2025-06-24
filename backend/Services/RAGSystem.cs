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
    public RAGSystem(IAzureBlobService blobService, EnvironmentConfig environmentConfig)
    {
        _logger = Log.ForContext<RAGSystem>();
        Toolbox = new Tools();
        _environmentConfig = environmentConfig;

        // * RAF System Initialization
        _logger.Information("Initializing RAG system");


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
        AzureKeyCredential credential = new AzureKeyCredential(_environmentConfig.GetVariableValue(EnvironmentVariable.DOCUMENT_INTELLIGENCE_CLIENT_API_KEY));
        DocumentIntelligenceClient = new DocumentIntelligenceClient(new Uri(_environmentConfig.GetVariableValue(EnvironmentVariable.DOCUMENT_INTELLIGENCE_CLIENT_ENDPOINT)), credential);


        // * Chat Completions - Azure OpenAI
        Uri azureOpenAiEndpoint = new Uri(_environmentConfig.GetVariableValue(EnvironmentVariable.AZURE_OPENAI_CLIENT_ENDPOINT));
        ApiKeyCredential azureOpenAiApiKeyCredential = new ApiKeyCredential(_environmentConfig.GetVariableValue(EnvironmentVariable.AZURE_OPENAI_CLIENT_API_KEY));
        AzureOpenAIClient AzureOpenAIClient = new AzureOpenAIClient(azureOpenAiEndpoint, azureOpenAiApiKeyCredential);
        ChatClient = AzureOpenAIClient.GetChatClient(_environmentConfig.GetVariableValue(EnvironmentVariable.CHAT_DEPLOYMENT_NAME));
        _logger.Information("Azure OpenAI client successfully initialized");


        // * Embeddings
        Uri embeddingsEndpoint = new Uri(_environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_CLIENT_ENDPOINT));
        AzureKeyCredential embeddingsKeyCredential = new AzureKeyCredential(_environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_CLIENT_API_KEY));
        EmbeddingsClient = new EmbeddingsClient(embeddingsEndpoint, embeddingsKeyCredential);
        _logger.Information("Embeddings client successfully initialized");

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

        try
        {
            const int batchSize = 96; // Maximum allowed by the API
            int totalProcessed = 0;

            for (int i = 0; i < chunks.Count; i += batchSize)
            {
                var batch = chunks.Skip(i).Take(batchSize).ToList();
                Response<EmbeddingsResult> response = await GenerateEmbeddings(batch);

                foreach (EmbeddingItem item in response.Value.Data)
                {
                    float[]? embeddingData = item.Embedding.ToObjectFromJson<float[]>();
                    if (embeddingData == null || embeddingData.Length == 0) continue;

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

                totalProcessed += batch.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate embeddings for the chunks.");
            _logger.Information("Indexing only the text without embeddings");

            int index = 0;
            foreach (string item in chunks)
            {
                string ChunkTypeString = index == 0 ? ChunkType.MetaData.ToString() : ChunkType.ContentText.ToString();

                CustomPayload customPayload = new CustomPayload
                {
                    ResourceId = id.ToString(),
                    ChunkType = ChunkTypeString,
                    ChunkText = item,
                    ChunkPart = index++
                };

                PointStruct point = new PointStruct();
                point.Id = Guid.NewGuid();
                point.Vectors = new float[EmbeddingsDimensions]; // Placeholder for empty vector
                point.Payload.Add(customPayload.ToPayload());

                pointsList.Add(point);
            }
        }

        await QdrantClient.UpsertAsync(CollectionName, pointsList);
    }



    /// <summary>
    /// Updates the metadata point in the vector database with new chunk text and corresponding embedding.
    /// </summary>
    /// <param name="id">The resource ID used to identify the metadata point to update.</param>
    /// <param name="newChankText">The new chunk text to replace the existing metadata content.</param>
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
    public async Task<bool> UpdateMetadataPointAsync(string id, string newChankText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.Warning("Resource ID is null or empty. Cannot update metadata.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(newChankText))
            {
                _logger.Warning("New chunk text is null or empty. Cannot update metadata.");
                return false;
            }

            // Get the existing point ID
            IReadOnlyList<ScoredPoint> restult = await QdrantClient.QueryAsync(
                 collectionName: CollectionName,
                 filter: MatchKeyword("resourceId", id) & MatchKeyword("chunkType", ChunkType.MetaData.ToString()),
                 limit: 1
            );

            float[] newEmbeding = await GenerateEmbedding(newChankText);

            PointVectors pointVectors = new PointVectors
            {
                Id = restult[0].Id,
                Vectors = newEmbeding,
            };

            await QdrantClient.UpdateVectorsAsync(
                collectionName: CollectionName,
                points: new List<PointVectors> { pointVectors }
            );

            await QdrantClient.OverwritePayloadAsync(
                collectionName: CollectionName,
                payload: new Dictionary<string, Value> { { "chunkText", newChankText } },
                filter: MatchKeyword("resourceId", id) & MatchKeyword("chunkType", ChunkType.MetaData.ToString())
            );

            return true;
        }
        catch { return false; }
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
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


