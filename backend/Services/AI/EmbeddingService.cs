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

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await WithRetryAsync(() => EmbeddingClient.GenerateEmbeddingAsync(query, new EmbeddingGenerationOptions { Dimensions = 1024 }));
        stopwatch.Stop();
        logger.Debug("Embedding generation took {ElapsedMs}ms for query: {Query}", stopwatch.ElapsedMilliseconds, query);
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

    // A resource with many chunks (a long document) would otherwise batch all of them into a single
    // embeddings request — a much bigger/heavier request than a typical one-chunk entity, and more
    // likely to trip a rate limit on its own regardless of how many resources are processed concurrently.
    // Capping the batch size keeps every individual request roughly the same size no matter how large
    // the source document is.
    private const int MaxEmbeddingBatchSize = 20;

    public async Task<float[][]> GenerateEmbeddings(IReadOnlyList<string> texts)
    {
        if (texts.Count == 0) return [];

        if (texts.Count <= MaxEmbeddingBatchSize)
            return await GenerateEmbeddingsBatchAsync(texts);

        float[][] embeddings = new float[texts.Count][];
        for (int offset = 0; offset < texts.Count; offset += MaxEmbeddingBatchSize)
        {
            List<string> batch = [.. texts.Skip(offset).Take(MaxEmbeddingBatchSize)];
            float[][] batchEmbeddings = await GenerateEmbeddingsBatchAsync(batch);
            Array.Copy(batchEmbeddings, 0, embeddings, offset, batchEmbeddings.Length);
        }

        return embeddings;
    }

    private async Task<float[][]> GenerateEmbeddingsBatchAsync(IReadOnlyList<string> texts)
    {
        var result = await WithRetryAsync(() => EmbeddingClient.GenerateEmbeddingsAsync(texts, new EmbeddingGenerationOptions { Dimensions = 1024 }));

        float[][] embeddings = new float[texts.Count][];

        foreach (OpenAIEmbedding embedding in result.Value)
        {
            float[] vector = embedding.ToFloats().ToArray();
            embeddings[embedding.Index] = vector;

            lock (cacheLock)
            {
                if (embeddingCache.Count >= MaxCacheSize)
                {
                    var oldestKey = embeddingCache.OrderBy(kvp => kvp.Value.CachedAt).First().Key;
                    embeddingCache.Remove(oldestKey);
                }

                embeddingCache[texts[embedding.Index].Trim().ToLowerInvariant()] = (vector, DateTime.UtcNow);
            }
        }

        return embeddings;
    }

    /// <summary>
    /// Retries a call with exponential backoff on 429/502/503 specifically — same treatment as the
    /// Mistral OCR calls in TextExtractionService, since this SDK call had no retry logic at all.
    /// </summary>
    private async Task<T> WithRetryAsync<T>(Func<Task<T>> action, int maxAttempts = 3)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (ClientResultException ex) when (attempt < maxAttempts && ex.Status is 429 or 502 or 503)
            {
                TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                logger.Warning(ex, "Embedding request returned {Status}, retrying in {Delay}s (attempt {Attempt}/{Max})", ex.Status, delay.TotalSeconds, attempt, maxAttempts);
                await Task.Delay(delay);
            }
        }
    }
}
