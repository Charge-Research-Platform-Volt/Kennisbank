using System.ClientModel;
using OpenAI;
using OpenAI.Embeddings;
using KnowledgeBank.Utils;
using OpenAI.Chat;
using Serilog;

namespace KnowledgeBank.Services.AI;

public enum ChunkType { ContentText, MetaData }

public class EmbeddingService
{
    private readonly Serilog.ILogger logger;
    private readonly Dictionary<string, (float[] Embedding, DateTime CachedAt)> embeddingCache;
    private readonly TimeSpan cacheExpiration = TimeSpan.FromHours(1);
    private readonly Lock cacheLock = new();
    private const int MaxCacheSize = 1000;

    public EmbeddingClient EmbeddingClient { get; private set; }

    public EmbeddingService(EnvironmentConfig environmentConfig)
    {
        logger = Log.ForContext<EmbeddingService>();
        embeddingCache = [];

        OpenAIClientOptions embeddingsOptions = new() { Endpoint = new Uri(environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_ENDPOINT)) };
        OpenAIClient embeddingsClient = new(new ApiKeyCredential(environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_API_KEY)), embeddingsOptions);
        EmbeddingClient = embeddingsClient.GetEmbeddingClient(environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_MODEL_NAME));

        logger.Information("AI client provider initialized");
    }

    public async Task<float[]> GenerateEmbedding(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));

        string cacheKey = query.Trim().ToLowerInvariant();

        lock (cacheLock)
        {
            if (embeddingCache.TryGetValue(cacheKey, out var cached))
            {
                if (DateTime.UtcNow - cached.CachedAt < cacheExpiration)
                    return cached.Embedding;

                embeddingCache.Remove(cacheKey);
            }
        }

        var result = await EmbeddingClient.GenerateEmbeddingAsync(query, new EmbeddingGenerationOptions { Dimensions = 1024 });
        float[] embedding = result.Value.ToFloats().ToArray();

        lock (cacheLock)
        {
            if (embeddingCache.Count >= MaxCacheSize)
            {
                var oldestKey = embeddingCache.OrderBy(kvp => kvp.Value.CachedAt).First().Key;
                embeddingCache.Remove(oldestKey);
            }

            embeddingCache[cacheKey] = (embedding, DateTime.UtcNow);
        }

        return embedding;
    }
}
