using System.ClientModel;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using KnowledgeBank.Data;
using KnowledgeBank.Utils;
using OpenAI.Chat;
using Serilog;


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
    /// <item><description>Sets up vector database client via IVectorStore abstraction</description></item>
    /// <item><description>Configures Azure Document Intelligence client for document processing</description></item>
    /// <item><description>Establishes Azure OpenAI chat client for conversational AI</description></item>
    /// <item><description>Initializes embeddings client for vector generation</description></item>
    /// </list>
    /// The constructor initializes embedding and chat clients for the RAG system.
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

        _logger.Information("RAG system successfully initialized");
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
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


