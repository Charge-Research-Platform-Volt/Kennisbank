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

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0070

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
    }



    /// <summary>
    /// Processes a document through the RAG (Retrieval-Augmented Generation) pipeline.
    /// </summary>
    /// <param name="resourceId">The unique identifier for the resource being processed.</param>
    /// <param name="fileType">The type of file being processed (e.g., PDF, DOCX).</param>
    /// <param name="resourcMetaData">Metadata associated with the file resource.</param>
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
    public async Task MainPipeline(Guid resourceId, string fileType, FileResourceCreateDto resourcMetaData)
    {
        // -- Extract text from the document
        string extractedText = await _toolbox.ExtractTextAsync(fileType, resourceId, _blobService);

        if (string.IsNullOrEmpty(extractedText))
        {
            _logger.Warning("No text extracted from the document");
            return;
        }

        // -- Chunk the extracted text
        List<string> chunks =
        [
            $"{resourcMetaData.Title}\n{resourcMetaData.Description}", // Include metadata in the first chunk
            .. _toolbox.SplitTextIntoChunks(extractedText, true),
        ];


        // -- Generate embeddings for the chunks
        List<PointStruct> pointsList = [];

        try
        {
            EmbeddingsOptions requestOptions = new EmbeddingsOptions(chunks);
            Response<EmbeddingsResult> response = await EmbeddingsClient.EmbedAsync(requestOptions);
            _logger.Information("Embeddings generated for {Count} chunks", response.Value.Data.Count);

            foreach (EmbeddingItem item in response.Value.Data)
            {
                float[]? embeddingData = item.Embedding.ToObjectFromJson<float[]>();
                if (embeddingData == null || embeddingData.Length == 0) continue;

                pointsList.Add(new PointStruct
                {
                    Id = Guid.NewGuid(),
                    Vectors = embeddingData,
                    Payload = { ["resourceId"] = resourceId.ToString(), ["chunkType"] = ChunkType.ContentText.ToString(), ["chunkText"] = chunks[item.Index], ["chunkPart"] = item.Index }
                });
            }
        }
        catch (Exception)
        {
            _logger.Error("Failed to generate embeddings for the chunks. Ensure the Azure OpenAI service is configured correctly.");
            _logger.Information("Indexing only the text without embeddings");

            int index = 0;
            foreach (string item in chunks)
            {
                pointsList.Add(new PointStruct
                {
                    Id = Guid.NewGuid(),
                    Vectors = new float[EMBEDDING_DIMENSIONS], // Placeholder for empty vector
                    Payload = { ["resourceId"] = resourceId.ToString(), ["chunkType"] = ChunkType.ContentText.ToString(), ["chunkText"] = item, ["chunkPart"] = index++ }
                });
            }
        }

        await QdrantClient.UpsertAsync(COLLECTION_NAME, pointsList);
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