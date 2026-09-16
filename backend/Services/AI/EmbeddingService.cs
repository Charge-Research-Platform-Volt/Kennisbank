using System.ClientModel;
using OpenAI;
using OpenAI.Embeddings;
using KnowledgeBank.Utils;
using OpenAI.Chat;
using Serilog;
using System.Diagnostics;

namespace KnowledgeBank.Services.AI;

public enum ChunkType { ContentText, MetaData }

public class EmbeddingService
{
    private readonly Serilog.ILogger logger;
    private readonly Dictionary<string, (float[] Embedding, DateTime CachedAt)> embeddingCache;
    private readonly TimeSpan cacheExpiration = TimeSpan.FromHours(1);
    private readonly Lock cacheLock = new();
    private const int MaxCacheSize = 1000;

    private const double SafetyMargin = 0.9;
    private const int LimiterWaitLogThresholdMs = 50;
    private readonly RequestTokenLimiter rateLimiter;
    private readonly SemaphoreSlim concurrencyLimiter;

    private const int MaxBatchTokenBudget = 50_000;
    private const int MaxEmbeddingBatchSize = MaxBatchTokenBudget / IngestionService.MaxChunkTokens;

    public EmbeddingClient EmbeddingClient { get; private set; }

    public EmbeddingService(EnvironmentConfig environmentConfig)
    {
        logger = Log.ForContext<EmbeddingService>();
        embeddingCache = [];

        OpenAIClientOptions embeddingsOptions = new() { Endpoint = new Uri(environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_ENDPOINT)) };
        OpenAIClient embeddingsClient = new(new ApiKeyCredential(environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_API_KEY)), embeddingsOptions);
        EmbeddingClient = embeddingsClient.GetEmbeddingClient(environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_MODEL_NAME));

        int requestsPerMinute = int.Parse(environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_REQUESTS_PER_MINUTE));
        int tokensPerMinute = int.Parse(environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_TOKENS_PER_MINUTE));
        int maxConcurrentRequests = int.Parse(environmentConfig.GetVariableValue(EnvironmentVariable.EMBEDDINGS_MAX_CONCURRENT_REQUESTS));

        rateLimiter = new RequestTokenLimiter((long)(requestsPerMinute * SafetyMargin), (long)(tokensPerMinute * SafetyMargin), TimeSpan.FromSeconds(60));
        concurrencyLimiter = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);

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

        Stopwatch stopwatch = Stopwatch.StartNew();
        float[][] result = await GenerateEmbeddingsBatchAsync([query]);
        stopwatch.Stop();
        logger.Debug("Embedding generation took {ElapsedMs}ms for query: {Query}", stopwatch.ElapsedMilliseconds, query);
        
        return result[0];
    }

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
    /// Retries a call with exponential backoff on 429/502/503 specifically.
    /// Also waits for rate limiter slot and concurrency slot before every attempt.
    /// </summary>
    private async Task<ClientResult<OpenAIEmbeddingCollection>> WithRetryAsync(Func<Task<ClientResult<OpenAIEmbeddingCollection>>> action, int maxAttempts = 3)
    {
        System.Diagnostics.Stopwatch concurrencyWait = System.Diagnostics.Stopwatch.StartNew();
        await concurrencyLimiter.WaitAsync();
        if (concurrencyWait.ElapsedMilliseconds > LimiterWaitLogThresholdMs)
            logger.Warning("Embedding concurrency limiter delayed request by {DelayMs}ms", concurrencyWait.ElapsedMilliseconds);

        try
        {
            System.Diagnostics.Stopwatch rateWait = System.Diagnostics.Stopwatch.StartNew();
            await rateLimiter.WaitForSlotAsync();
            if (rateWait.ElapsedMilliseconds > LimiterWaitLogThresholdMs)
                logger.Warning("Embedding rate limiter delayed request by {DelayMs}ms", rateWait.ElapsedMilliseconds);

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    ClientResult<OpenAIEmbeddingCollection> result = await action();
                    rateLimiter.RecordTokens(result.Value.Usage.InputTokenCount);
                    return result;
                }
                catch (ClientResultException ex) when (attempt < maxAttempts && ex.Status is 429 or 502 or 503)
                {
                    TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    logger.Warning(ex, "Embedding request returned {Status}, retrying in {Delay}s (attempt {Attempt}/{Max})", ex.Status, delay.TotalSeconds, attempt, maxAttempts);
                    await Task.Delay(delay);
                }
            }
        }
        finally
        {
            concurrencyLimiter.Release();
        }
    }
}
