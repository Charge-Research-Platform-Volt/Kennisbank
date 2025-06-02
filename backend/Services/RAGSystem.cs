using System.ClientModel;
using Azure;
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


public class RAGSystem
{
    // Constants:
    public const string COLLECTION_NAME = "Knowledgebank";
    public const int EMBEDDING_DIMENSIONS = 3072;


    // Dependencies:
    private readonly Serilog.ILogger _logger;
    private readonly IAzureBlobService _blobService;
    private readonly ITools _toolbox;



    // RAG System components:
    public QdrantClient QdrantClient { get; private set; }
    public EmbeddingsClient EmbeddingsClient { get; private set; }

    // chat
    public ChatCompletionsClient ChatCompletionsClient { get; private set; }
    public AzureOpenAIClient AzureOpenAIClient { get; private set; }
    public ChatClient ChatClient { get; private set; }


    public RAGSystem(IAzureBlobService blobService, EnvironmentConfig environmentConfig)
    {
        _logger = Log.ForContext<RAGSystem>();
        _blobService = blobService;

        _toolbox = new Tools();


        // *************** RAF System Initialization ***************
        _logger.Information("Initializing RAG system");

        QdrantClient = new QdrantClient(
            host: "qdrant",
            port: 6334,
            https: false,
            apiKey: null // No API key needed for local Qdrant      
        );
        _logger.Information("Qdrant client successfully initialized with host: {Host}, port: {Port}", "qdrant", 6334);

        // Ensure the Qdrant collection exists
        InitializeCollectionAsync().GetAwaiter().GetResult();
        _logger.Information("Qdrant collection {CollectionName} checked and initialized if necessary", COLLECTION_NAME);

        // Ensure the payload fields are indexed
        IndexPayloadFieldsAsync().GetAwaiter().GetResult();


        // Embeddings
        Uri embeddingsEndpoint = new Uri(environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_CLIENT_ENDPOINT));
        AzureKeyCredential embeddingsKeyCredential = new AzureKeyCredential(environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_CLIENT_API_KEY));
        EmbeddingsClient = new EmbeddingsClient(embeddingsEndpoint, embeddingsKeyCredential);
        _logger.Information("Embeddings client successfully initialized");


        Uri azureOpenAiEndpoint = new Uri(environmentConfig.GetVariableValue(EnvironmentVariable.AZURE_OPENAI_CLIENT_ENDPOINT));
        ApiKeyCredential azureOpenAiApiKeyCredential = new ApiKeyCredential(environmentConfig.GetVariableValue(EnvironmentVariable.AZURE_OPENAI_CLIENT_API_KEY));
        AzureOpenAIClient = new AzureOpenAIClient(azureOpenAiEndpoint, azureOpenAiApiKeyCredential);
        ChatClient = AzureOpenAIClient.GetChatClient("gpt-4.1");

        _logger.Information("Azure OpenAI client successfully initialized");


        Uri chatCompletionsEndpoint = new Uri(environmentConfig.GetVariableValue(EnvironmentVariable.CHAT_COMPLETIONS_CLIENT_ENDPOINT));
        AzureKeyCredential chatCompletionsApiKeyCredential = new AzureKeyCredential(environmentConfig.GetVariableValue(EnvironmentVariable.CHAT_COMPLETIONS_CLIENT_API_KEY));
        ChatCompletionsClient = new ChatCompletionsClient(chatCompletionsEndpoint, chatCompletionsApiKeyCredential);
        _logger.Information("Chat completions client successfully initialized");

        _logger.Information("RAG system successfully initialized");
    }



    /// <summary>
    /// Initializes the Qdrant (vector database) collection if it does not exist.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InitializeCollectionAsync()
    {
        bool exists = await QdrantClient.CollectionExistsAsync(COLLECTION_NAME);

        if (!exists)
        {
            _logger.Information("Collection {CollectionName} does not exist. Creating it now.", COLLECTION_NAME);

            // Configration:
            StrictModeConfig strictModeConfig = new StrictModeConfig
            {
                Enabled = true,
                UnindexedFilteringRetrieve = false // Ensure strict mode is enabled
            };

            VectorParams vectorParams = new VectorParams
            {
                Size = EMBEDDING_DIMENSIONS,
                Distance = Distance.Cosine,
                HnswConfig = new HnswConfigDiff
                {
                    M = 16,
                    EfConstruct = 100,
                    FullScanThreshold = 1000,
                }
            };

            await QdrantClient.CreateCollectionAsync(
                collectionName: COLLECTION_NAME,
                strictModeConfig: strictModeConfig,
                vectorsConfig: vectorParams
            );
        }
        else
        {
            _logger.Information("Collection {CollectionName} already exists. No action taken.", COLLECTION_NAME);
        }
    }


    public async Task IndexPayloadFieldsAsync()
    {
        await QdrantClient.CreatePayloadIndexAsync(
            collectionName: COLLECTION_NAME,
            fieldName: "resourceId"
        );

        // Full text index for chunkText
        await QdrantClient.CreatePayloadIndexAsync(
            collectionName: COLLECTION_NAME,
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
    /// Processes a document through the RAG (Retrieval-Augmented Generation) pipeline.
    /// </summary>
    /// <param name="id">The unique identifier for the resource being processed.</param>
    /// <param name="fileType">The type of file being processed (e.g., PDF, DOCX).</param>
    /// <param name="chunk">An initial chunk of text to include in the processing.</param>
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
    public async Task CreatePoints(Guid id, string chunk, string? fileType)
    {
        // Include metadata in the first chunk
        List<string> chunks = [$"{chunk}",];

        // -- Extract text from the document
        if (!string.IsNullOrWhiteSpace(fileType))
        {
            string extractedText = await _toolbox.ExtractTextAsync(fileType, id, _blobService);

            if (string.IsNullOrEmpty(extractedText))
            {
                _logger.Warning("No text extracted from the document");
                return;
            }

            // -- Chunk the extracted text
            chunks.AddRange(_toolbox.SplitTextIntoChunks(extractedText, true));
        }


        // -- Generate embeddings for the chunks
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
                point.Vectors = new float[EMBEDDING_DIMENSIONS]; // Placeholder for empty vector
                point.Payload.Add(customPayload.ToPayload());

                pointsList.Add(point);
            }
        }

        await QdrantClient.UpsertAsync(COLLECTION_NAME, pointsList);
    }

    public async Task<float[]> GenerateEmbedding(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.Warning("Query is null or empty. Cannot generate embedding.");
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        EmbeddingsOptions requestOptions = new EmbeddingsOptions(new List<string> { query });
        Response<EmbeddingsResult> response = await EmbeddingsClient.EmbedAsync(requestOptions);

        float[]? embeddingData = response.Value.Data[0].Embedding.ToObjectFromJson<float[]>();
        if (embeddingData == null || embeddingData.Length == 0)
        {
            _logger.Warning("Generated embedding is null or empty.");
            throw new InvalidOperationException("Generated embedding is null or empty.");
        }

        return embeddingData;
    }

    public async Task<Response<EmbeddingsResult>> GenerateEmbeddings(List<string> chunks)
    {
        if (chunks == null || chunks.Count == 0)
        {
            _logger.Warning("Chunks list is null or empty. Cannot generate embeddings.");
            throw new ArgumentException("Chunks list cannot be null or empty.", nameof(chunks));
        }

        EmbeddingsOptions requestOptions = new EmbeddingsOptions(chunks);
        Response<EmbeddingsResult> responses = await EmbeddingsClient.EmbedAsync(requestOptions);

        return responses;
    }

    public async Task UpdateMetadataPointAsync(string id, string newChankText)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.Warning("Resource ID is null or empty. Cannot update metadata.");
            throw new ArgumentException("Resource ID cannot be null or empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(newChankText))
        {
            _logger.Warning("New chunk text is null or empty. Cannot update metadata.");
            throw new ArgumentException("New chunk text cannot be null or empty.", nameof(newChankText));
        }

        // Get the existing point ID
        IReadOnlyList<ScoredPoint> restult = await QdrantClient.QueryAsync(
             collectionName: COLLECTION_NAME,
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
            collectionName: COLLECTION_NAME,
            points: new List<PointVectors> { pointVectors }
        );

        await QdrantClient.OverwritePayloadAsync(
            collectionName: COLLECTION_NAME,
            payload: new Dictionary<string, Value> { { "chunkText", newChankText } },
            filter: MatchKeyword("resourceId", id) & MatchKeyword("chunkType", ChunkType.MetaData.ToString())
        );

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
                collectionName: COLLECTION_NAME,
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




// ---notes


//  Groups 
// await _ragSystem.QdrantClient.CreatePayloadIndexAsync(
//     collectionName: RAGSystem.COLLECTION_NAME,
//     fieldName: "chunkType"
// );

// await _ragSystem.QdrantClient.CreatePayloadIndexAsync(
//      collectionName: RAGSystem.COLLECTION_NAME,
//      fieldName: "resourceId"
//  );


// // print the search results
// Console.WriteLine($"Found {search.Count} results:");
// foreach (var result2 in search)
// {
//     // var resourceId = result2.Payload.TryGetValue("resourceId", out var ridValue) ? ridValue.StringValue : "N/A";
//     // var chunkText = result2.Payload.TryGetValue("chunkText", out var ctValue) ? ctValue.StringValue : "N/A";
//     Console.WriteLine(result2);
// }


// --------------