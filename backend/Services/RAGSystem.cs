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


    // Dependencies:
    private readonly Serilog.ILogger _logger;
    private readonly EnvironmentConfig _environmentConfig;

    public Tools Toolbox { get; private set; }



    // RAG System components:


    public QdrantClient QdrantClient { get; private set; }
    public ulong EmbeddingsDimensions { get; private set; }
    public string CollectionName { get; private set; }



    public DocumentIntelligenceClient DocumentIntelligenceClient { get; private set; }
    public EmbeddingsClient EmbeddingsClient { get; private set; }

    // chat
    public AzureOpenAIClient AzureOpenAIClient { get; private set; }
    public ChatClient ChatClient { get; private set; }


    public RAGSystem(IAzureBlobService blobService, EnvironmentConfig environmentConfig)
    {
        _logger = Log.ForContext<RAGSystem>();
        Toolbox = new Tools();
        _environmentConfig = environmentConfig;

        // *************** RAF System Initialization ***************
        _logger.Information("Initializing RAG system");


        // * Qdrant (Vector Database) Initialization
        EmbeddingsDimensions = ulong.Parse(_environmentConfig.GetVariableValue(EnvironmentVariable.QDRANT_EMBEDDINGS_DIMENSIONS));
        CollectionName = _environmentConfig.GetVariableValue(EnvironmentVariable.QDRANT_COLLECTION_NAME);

        QdrantClient = new QdrantClient(
            host: _environmentConfig.GetVariableValue(EnvironmentVariable.QDRANT_HOST),
            https: bool.Parse(_environmentConfig.GetVariableValue(EnvironmentVariable.QDRANT_HTTPS)),
            apiKey: string.IsNullOrEmpty(_environmentConfig.GetVariableValue(EnvironmentVariable.QDRANT_API_KEY))
            ? null
            : _environmentConfig.GetVariableValue(EnvironmentVariable.QDRANT_API_KEY)
        );
        _logger.Information("Qdrant client successfully initialized with host: {Host}, port: {Port}", "qdrant", 6334);

        // Ensure the Qdrant collection exists
        InitializeCollectionAsync().GetAwaiter().GetResult();
        _logger.Information("Qdrant collection {CollectionName} checked and initialized if necessary", CollectionName);

        // Ensure the payload fields are indexed
        IndexPayloadFieldsAsync().GetAwaiter().GetResult();


        // Document Intelligence 
        AzureKeyCredential credential = new AzureKeyCredential(_environmentConfig.GetVariableValue(EnvironmentVariable.DOCUMENT_INTELLIGENCE_CLIENT_API_KEY));
        DocumentIntelligenceClient = new DocumentIntelligenceClient(new Uri(_environmentConfig.GetVariableValue(EnvironmentVariable.DOCUMENT_INTELLIGENCE_CLIENT_ENDPOINT)), credential);


        // Chat Completions - Azure OpenAI
        Uri azureOpenAiEndpoint = new Uri(_environmentConfig.GetVariableValue(EnvironmentVariable.AZURE_OPENAI_CLIENT_ENDPOINT));
        ApiKeyCredential azureOpenAiApiKeyCredential = new ApiKeyCredential(_environmentConfig.GetVariableValue(EnvironmentVariable.AZURE_OPENAI_CLIENT_API_KEY));
        AzureOpenAIClient = new AzureOpenAIClient(azureOpenAiEndpoint, azureOpenAiApiKeyCredential);
        ChatClient = AzureOpenAIClient.GetChatClient(_environmentConfig.GetVariableValue(EnvironmentVariable.CHAT_DEPLOYMENT_NAME));
        _logger.Information("Azure OpenAI client successfully initialized");


        // Embeddings
        Uri embeddingsEndpoint = new Uri(_environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_CLIENT_ENDPOINT));
        AzureKeyCredential embeddingsKeyCredential = new AzureKeyCredential(_environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_CLIENT_API_KEY));
        EmbeddingsClient = new EmbeddingsClient(embeddingsEndpoint, embeddingsKeyCredential);
        _logger.Information("Embeddings client successfully initialized");

        _logger.Information("RAG system successfully initialized");
    }



    /// <summary>
    /// Initializes the Qdrant (vector database) collection if it does not exist.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    public async Task IndexPayloadFieldsAsync()
    {
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


    // ---

    public async Task CreatePoints(Guid id, List<string> chunks)
    {
        List<PointStruct> pointsList = [];

        try
        {
            Response<EmbeddingsResult> response = await GenerateEmbeddings(chunks);

            foreach (EmbeddingItem item in response.Value.Data)
            {
                float[]? embeddingData = item.Embedding.ToObjectFromJson<float[]>();
                if (embeddingData == null || embeddingData.Length == 0) continue;

                string ChunkTypeString = item.Index == 0 ? ChunkType.MetaData.ToString() : ChunkType.ContentText.ToString();

                CustomPayload customPayload = new CustomPayload
                {
                    ResourceId = id.ToString(),
                    ChunkType = ChunkTypeString,
                    ChunkText = chunks[item.Index],
                    ChunkPart = item.Index
                };

                PointStruct point = new PointStruct();
                point.Id = Guid.NewGuid();
                point.Vectors = embeddingData;
                point.Payload.Add(customPayload.ToPayload());

                pointsList.Add(point);
            }
        }
        catch (Exception)
        {
            _logger.Error("Failed to generate embeddings for the chunks. Ensure the Azure OpenAI service is configured correctly.");
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