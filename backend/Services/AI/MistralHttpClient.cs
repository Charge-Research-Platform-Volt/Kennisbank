using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using KnowledgeBank.Utils;

namespace KnowledgeBank.Services.AI;

public enum MistralReasoningEffort { Default, None, High }
public enum MistralCapability { CodeInterpreter, WebSearch, PremiumWebSearch, ImageGeneration }
public enum MistralResponseFormat { Text, Json, JsonSchema }
public record MistralToolCall(string Id, string Name, string Arguments);
public record MistralCompletion(string? Content, List<MistralToolCall>? ToolCalls, int? PromptTokens = null)
{
    public bool HasToolCalls => ToolCalls?.Count > 0;
}
public record MistralFunction(string Name, string Description, object Parameters);
public record MistralStreamChunk(
    string? Content = null,
    string? Reasoning = null,
    List<MistralToolCall>? ToolCalls = null, 
    int? PromptTokens = null,
    int? CompletionTokens = null,
    int? TotalTokens = null,
    int? CachedTokens = null,
    string? Model = null,
    string? Id = null
);

public class MistralContextLengthExceededException(string message) : Exception(message);

public class MistralChatRequest
{
    public required List<object> Messages { get; init; }
    public float Temperature { get; init; } = 0.2f;
    public MistralReasoningEffort ReasoningEffort { get; init; }
    public MistralFunction[]? Functions { get; init; }
    public MistralCapability[]? Capabilities { get; init; }
    public MistralResponseFormat ResponseFormat { get; init; } = MistralResponseFormat.Text;
    public int? MaxTokens {get; init; }
    public object? JsonSchema { get; init; }
}

public record MistralOcrPage(string? Markdown, string? Header, string? Footer);
public record MistralOcrResult(List<MistralOcrPage> Pages, string Model);

public class MistralHttpClient
{
    private const int MaxRetries = 3;
    private const int MaxRetryAfterSeconds = 30;

    private const double SafetyMargin = 0.9;
    private const int LimiterWaitLogThresholdMs = 50;
    private static readonly TimeSpan LimiterWindow = TimeSpan.FromSeconds(60);

    private readonly HttpClient textHttpClient;
    private readonly HttpClient filesHttpClient;
    private readonly string smallModelName;
    private readonly string mediumModelName;
    private readonly MistralStatusService mistralStatusService;
    private readonly Serilog.ILogger logger = Serilog.Log.ForContext<MistralHttpClient>();

    private readonly RequestTokenLimiter smallRateLimiter;
    private readonly RequestTokenLimiter mediumRateLimiter;
    private readonly RollingWindowLimiter ocrPageLimiter;

    public MistralHttpClient(EnvironmentConfig environmentConfig, MistralStatusService mistralStatusService)
    {
        this.mistralStatusService = mistralStatusService;

        // Load values
        smallModelName = environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_SMALL_MODEL_NAME);
        mediumModelName = environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_MEDIUM_MODEL_NAME);
        Uri endpoint = new Uri(environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_ENDPOINT).TrimEnd('/') + "/");
        string apiKey = environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_API_KEY);

        int smallRpm = int.Parse(environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_SMALL_REQUESTS_PER_MINUTE));
        int smallTpm = int.Parse(environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_SMALL_TOKENS_PER_MINUTE));
        int mediumRpm = int.Parse(environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_MEDIUM_REQUESTS_PER_MINUTE));
        int mediumTpm = int.Parse(environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_MEDIUM_TOKENS_PER_MINUTE));
        int ocrPpm = int.Parse(environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_OCR_PAGES_PER_MINUTE));

        // Initialize request limiters
        smallRateLimiter = new RequestTokenLimiter((long)(smallRpm * SafetyMargin), (long)(smallTpm * SafetyMargin), LimiterWindow);
        mediumRateLimiter = new RequestTokenLimiter((long)(mediumRpm * SafetyMargin), (long)(mediumTpm * SafetyMargin), LimiterWindow);
        ocrPageLimiter = new RollingWindowLimiter((long)(ocrPpm * SafetyMargin), LimiterWindow);

        // Initialize HTTP Client for text
        textHttpClient = new HttpClient { BaseAddress = endpoint, Timeout = TimeSpan.FromSeconds(120) };
        textHttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        // Initialize HTTP Client for files (OCR)
        filesHttpClient = new HttpClient { BaseAddress = endpoint, Timeout = TimeSpan.FromMinutes(5) };
        filesHttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    private RequestTokenLimiter GetLimiter(string model)
        => model == smallModelName ? smallRateLimiter : mediumRateLimiter;

    public async Task<MistralCompletion> CompleteAsync(MistralChatRequest request, string? modelOverride = null, CancellationToken ct = default)
    {
        string effectiveModel = modelOverride ?? mediumModelName;
        logger.Debug("Mistral call [{Model}]", effectiveModel);
        Stopwatch stopwatch = Stopwatch.StartNew();
        string body = JsonSerializer.Serialize(BuildBody(request, modelOverride: modelOverride));

        RequestTokenLimiter limiter = GetLimiter(effectiveModel);
        Stopwatch limiterWait = Stopwatch.StartNew();
        await limiter.WaitForSlotAsync(ct);
        if (limiterWait.ElapsedMilliseconds > LimiterWaitLogThresholdMs)
            logger.Warning("Mistral rate limiter delayed [{Model}] completion by {DelayMs}ms", effectiveModel, limiterWait.ElapsedMilliseconds);

        HttpResponseMessage response = await SendWithRetryAsync(
            textHttpClient,
            () => new HttpRequestMessage(HttpMethod.Post, "chat/completions") { Content = new StringContent(body, Encoding.UTF8, "application/json") },
            HttpCompletionOption.ResponseContentRead,
            ct
        );

        if (!response.IsSuccessStatusCode)
        {
            string err = await response.Content.ReadAsStringAsync(ct);

            if ((int)response.StatusCode == 400)
                if (IsContextLengthError(err))
                    throw new MistralContextLengthExceededException(err);
                else
                    logger.Warning("Mistral returned 400 but not a context length error: {Error}", err);

            throw new HttpRequestException($"Mistral {response.StatusCode} - URL: {response.RequestMessage?.RequestUri} - Body: {err}");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var choice = doc.RootElement.GetProperty("choices")[0];

        int? promptTokens = null;
        int? completionTokens = null;
        if (doc.RootElement.TryGetProperty("usage", out var usage))
        {
            if (usage.TryGetProperty("prompt_tokens", out var pt)) promptTokens = pt.GetInt32();
            if (usage.TryGetProperty("completion_tokens", out var ctok)) completionTokens = ctok.GetInt32();
        }

        limiter.RecordTokens((promptTokens ?? 0) + (completionTokens ?? 0));

        var message = choice.GetProperty("message");
        string finishReason = choice.GetProperty("finish_reason").GetString() ?? "stop";

        stopwatch.Stop();
        logger.Debug("Mistral call [{Model}] took {ElapsedMs}ms, finish_reason={FinishReason}, prompt_tokens={PromptTokens}", effectiveModel, stopwatch.ElapsedMilliseconds, finishReason, promptTokens);

        if (finishReason == "tool_calls")
        {
            var toolCalls = message.GetProperty("tool_calls").EnumerateArray()
                .Select(tc => new MistralToolCall(
                    tc.GetProperty("id").GetString() ?? "",
                    tc.GetProperty("function").GetProperty("name").GetString() ?? "",
                    tc.GetProperty("function").GetProperty("arguments").GetString() ?? "{}"
                )).ToList();

            return new MistralCompletion(null, toolCalls, promptTokens);
        }

        return new MistralCompletion(ExtractTextContent(message.GetProperty("content")), null, promptTokens);
    }

    public async IAsyncEnumerable<MistralStreamChunk> StreamAsync(MistralChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string body = JsonSerializer.Serialize(BuildBody(request, stream: true));

        RequestTokenLimiter limiter = GetLimiter(mediumModelName);
        Stopwatch limiterWait = Stopwatch.StartNew();
        await limiter.WaitForSlotAsync(ct);
        if (limiterWait.ElapsedMilliseconds > LimiterWaitLogThresholdMs)
            logger.Warning("Mistral rate limiter delayed [{Model}] stream by {DelayMs}ms", mediumModelName, limiterWait.ElapsedMilliseconds);

        HttpResponseMessage response = await SendWithRetryAsync(
            textHttpClient,
            () => new HttpRequestMessage(HttpMethod.Post, "chat/completions") { Content = new StringContent(body, Encoding.UTF8, "application/json") },
            HttpCompletionOption.ResponseHeadersRead,
            ct
        );

        if (!response.IsSuccessStatusCode)
        {
            string err = await response.Content.ReadAsStringAsync(ct);

            if ((int)response.StatusCode == 400)
                if (IsContextLengthError(err))
                    throw new MistralContextLengthExceededException(err);
                else
                    logger.Warning("Mistral returned 400 but not a context length error: {Error}", err);

            throw new HttpRequestException($"Mistral {response.StatusCode} - URL: {response.RequestMessage?.RequestUri} - Body: {err}");
        }

        logger.Debug("Mistral stream headers received after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        bool anyContent = false;
        string? finishReason = null;
        string? lastData = null;
        string? model = null;
        string? id = null;
        int? promptTokens = null;
        int? completionTokens = null;
        int? totalTokens = null;
        int? cachedTokens = null;
        var toolCallBuilders = new Dictionary<int, ToolCallBuilder>();

        while (!ct.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(ct);
            if (line == null) break;

            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            string data = line["data: ".Length..];
            if (data == "[DONE]")
            {
                logger.Debug("Mistral stream completed after {ElapsedMs}ms, finish_reason={FinishReason}", stopwatch.ElapsedMilliseconds, finishReason);

                List<MistralToolCall>? toolCalls = toolCallBuilders.Count > 0
                    ? toolCallBuilders.OrderBy(kv => kv.Key).Select(kv => new MistralToolCall(kv.Value.Id ?? "", kv.Value.Name ?? "", kv.Value.Arguments.ToString())).ToList()
                    : null;

                if (!anyContent && toolCalls == null)
                    logger.Warning("Mistral stream produced no content, finish_reason={FinishReason}, last_chunk={LastChunk}", finishReason, lastData);

                limiter.RecordTokens(totalTokens ?? (promptTokens ?? 0) + (completionTokens ?? 0));

                yield return new MistralStreamChunk(
                    ToolCalls: toolCalls,
                    PromptTokens: promptTokens,
                    CompletionTokens: completionTokens,
                    TotalTokens: totalTokens,
                    CachedTokens: cachedTokens,
                    Model: model,
                    Id: id
                );

                yield break;
            }

            lastData = data;

            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            if (root.TryGetProperty("model", out var modelProp))
                model = modelProp.GetString();
            if (root.TryGetProperty("id", out var idProp))
                id = idProp.GetString();

            if (root.TryGetProperty("usage", out var usageProp) && usageProp.ValueKind == JsonValueKind.Object)
            {
                if (usageProp.TryGetProperty("prompt_tokens", out var pt)) promptTokens = pt.GetInt32();
                if (usageProp.TryGetProperty("completion_tokens", out var compTokensProp)) completionTokens = compTokensProp.GetInt32();
                if (usageProp.TryGetProperty("total_tokens", out var tt)) totalTokens = tt.GetInt32();
                if (usageProp.TryGetProperty("prompt_tokens_details", out var ptd) && ptd.TryGetProperty("cached_tokens", out var cached)) cachedTokens = cached.GetInt32();
            }

            var choice = root.GetProperty("choices")[0];

            if (choice.TryGetProperty("finish_reason", out var frProp) && frProp.ValueKind != JsonValueKind.Null)
                finishReason = frProp.GetString();

            var delta = choice.GetProperty("delta");

            if (delta.TryGetProperty("content", out var contentProp))
            {
                if (contentProp.ValueKind == JsonValueKind.String)
                {
                    string chunk = contentProp.GetString() ?? "";
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        anyContent = true;
                        yield return new MistralStreamChunk(Content: chunk);
                    }
                }
                else if (contentProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in contentProp.EnumerateArray())
                    {
                        string type = element.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                        
                        if (type == "thinking")
                        {
                            if (element.TryGetProperty("thinking", out var thinkingArr) && thinkingArr.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var thinkPart in thinkingArr.EnumerateArray())
                                {
                                    string thinkText = thinkPart.TryGetProperty("text", out var tt) && tt.ValueKind == JsonValueKind.String
                                        ? tt.GetString() ?? ""
                                        : "";

                                    if (!string.IsNullOrEmpty(thinkText))
                                        yield return new MistralStreamChunk(Reasoning: thinkText);
                                }
                            }
                        }
                        else
                        {
                            string text = element.TryGetProperty("text", out var txt) && txt.ValueKind == JsonValueKind.String
                                ? txt.GetString() ?? ""
                                : "";

                            if (string.IsNullOrEmpty(text))
                            {
                                logger.Debug("Unrecognized content chunk shape: {Chunk}", element.GetRawText());
                                continue;
                            }

                            anyContent = true;
                            yield return new MistralStreamChunk(Content: text);
                        }
                    }
                }
            }

            if (delta.TryGetProperty("tool_calls", out var toolCallsProp) && toolCallsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var tc in toolCallsProp.EnumerateArray())
                {
                    int index = tc.TryGetProperty("index", out var idxProp) ? idxProp.GetInt32() : 0;
                    if (!toolCallBuilders.TryGetValue(index, out var builder))
                        toolCallBuilders[index] = builder = new ToolCallBuilder();

                    if (tc.TryGetProperty("id", out var tcId) && tcId.ValueKind == JsonValueKind.String)
                        builder.Id = tcId.GetString();

                    if (tc.TryGetProperty("function", out var fn))
                    {
                        if (fn.TryGetProperty("name", out var fnName) && fnName.ValueKind == JsonValueKind.String)
                            builder.Name = fnName.GetString();
                        if (fn.TryGetProperty("arguments", out var fnArgs) && fnArgs.ValueKind == JsonValueKind.String)
                            builder.Arguments.Append(fnArgs.GetString());
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resolves a model alias (e.g. "mistral-ocr-latest") to the concrete dated model id it currently points to.
    /// Falls back to returning the alias unchanged if resolution fails for any reason.
    /// </summary>
    public async Task<string> ResolveModelAliasAsync(string alias, CancellationToken ct = default)
    {
        try
        {
            using HttpResponseMessage response = await SendWithRetryAsync(
                filesHttpClient,
                () => new HttpRequestMessage(HttpMethod.Get, "models"),
                HttpCompletionOption.ResponseContentRead, 
                ct
            );
            response.EnsureSuccessStatusCode();

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

            foreach (JsonElement model in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                if (model.TryGetProperty("aliases", out JsonElement aliases) && aliases.EnumerateArray().Any(a => a.GetString() == alias))
                {
                    return model.GetProperty("id").GetString() ?? alias;
                }
            }

            logger.Warning("Could not find a model with alias {Alias} in Mistral's model list, falling back to alias", alias);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to resolve {Alias} to a pinned model id, falling back to alias", alias);
        }

        return alias;
    }

    /// <summary>
    /// Uploads a file to Mistral's Files API, runs OCR on it via a signed URL, and deletes the uploaded
    /// file afterwards (best-effort, to avoid storage charges). Returns the raw per-page OCR result;
    /// callers handle any app-specific post-processing (markdown cleanup, caching) themselves.
    /// </summary>
    public async Task<MistralOcrResult> RunOcrAsync(byte[] fileBytes, string fileExtension, string model, bool isPdf, CancellationToken ct = default)
    {
        using HttpResponseMessage uploadResponse = await SendWithRetryAsync(
            filesHttpClient,
            () =>
            {
                MultipartFormDataContent uploadForm = new()
                {
                    { new StreamContent(new MemoryStream(fileBytes)), "file", $"document{fileExtension}" },
                    { new StringContent("ocr"), "purpose" }
                };
                return new HttpRequestMessage(HttpMethod.Post, "files") { Content = uploadForm };
            },
            HttpCompletionOption.ResponseContentRead,
            ct
        );

        string uploadResponseBody = await uploadResponse.Content.ReadAsStringAsync(ct);
        logger.Information("Mistral file upload response ({Status}): {Body}", (int)uploadResponse.StatusCode, uploadResponseBody);
        uploadResponse.EnsureSuccessStatusCode();

        string mistralFileId = JsonDocument.Parse(uploadResponseBody).RootElement.GetProperty("id").GetString()!;
        logger.Information("Uploaded file to Mistral Files API: {FileId}", mistralFileId);

        using HttpResponseMessage signedUrlResponse = await SendWithRetryAsync(
            filesHttpClient,
            () => new HttpRequestMessage(HttpMethod.Get, $"files/{mistralFileId}/url?expiry=1"),
            HttpCompletionOption.ResponseContentRead,
            ct
        );

        string signedUrlResponseBody = await signedUrlResponse.Content.ReadAsStringAsync(ct);
        logger.Information("Mistral signed URL response ({Status}): {Body}", (int)signedUrlResponse.StatusCode, signedUrlResponseBody);
        signedUrlResponse.EnsureSuccessStatusCode();

        string fileUrl = JsonDocument.Parse(signedUrlResponseBody).RootElement.GetProperty("url").GetString()!;

        object requestBody = isPdf
            ? new { model, document = new { type = "document_url", document_url = fileUrl }, extract_header = true, extract_footer = true }
            : new { model, document = new { type = "image_url", image_url = fileUrl }, extract_header = true, extract_footer = true };

        Stopwatch pageLimiterWait = Stopwatch.StartNew();
        await ocrPageLimiter.WaitForCapacityAsync(ct);
        if (pageLimiterWait.ElapsedMilliseconds > LimiterWaitLogThresholdMs)
            logger.Warning("Mistral OCR page rate limiter delayed request by {DelayMs}ms", pageLimiterWait.ElapsedMilliseconds);

        using HttpResponseMessage ocrResponse = await SendWithRetryAsync(
            filesHttpClient,
            () => new HttpRequestMessage(HttpMethod.Post, "ocr")
                { Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json") },
            HttpCompletionOption.ResponseContentRead,
            ct
        );

        string ocrResponseBody = await ocrResponse.Content.ReadAsStringAsync(ct);
        if (!ocrResponse.IsSuccessStatusCode)
            logger.Error("Mistral OCR failed ({Status}): {Body}", (int)ocrResponse.StatusCode, ocrResponseBody);
        ocrResponse.EnsureSuccessStatusCode();

        using JsonDocument doc = JsonDocument.Parse(ocrResponseBody);
        List<MistralOcrPage> pages = doc.RootElement.GetProperty("pages").EnumerateArray()
            .Select(page => new MistralOcrPage(
                page.GetProperty("markdown").GetString(),
                page.TryGetProperty("header", out JsonElement h) && h.ValueKind == JsonValueKind.String ? h.GetString() : null,
                page.TryGetProperty("footer", out JsonElement f) && f.ValueKind == JsonValueKind.String ? f.GetString() : null
            ))
            .ToList();

        ocrPageLimiter.RecordUsage(pages.Count);

        string responseModel = doc.RootElement.GetProperty("model").GetString() ?? model;

        try
        {
            using HttpRequestMessage deleteRequest = new(HttpMethod.Delete, $"files/{mistralFileId}");
            await filesHttpClient.SendAsync(deleteRequest, ct);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to delete Mistral file {FileId} after OCR", mistralFileId);
        }

        return new MistralOcrResult(pages, responseModel);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpClient client, Func<HttpRequestMessage> requestFactory, HttpCompletionOption completionOption, CancellationToken ct)
    {
        HttpResponseMessage? response = null;
        Exception? transportException = null;

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            transportException = null;
            using HttpRequestMessage request = requestFactory();

            try
            {
                response = await client.SendAsync(request, completionOption, ct);
            }
            catch (HttpRequestException ex) { transportException = ex; }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested) { transportException = ex; }

            if (transportException == null && response!.IsSuccessStatusCode)
            {
                await mistralStatusService.ReportSuccess();
                return response;
            }

            bool retryable = transportException != null || (int)response!.StatusCode is 503 or 529 or 429;
            if (retryable && attempt < MaxRetries - 1)
            {
                TimeSpan delay = GetRetryDelay(transportException == null ? response : null, attempt);
                await Task.Delay(delay, ct);
                continue;
            }

            break;
        }

        if (transportException != null)
        {
            await mistralStatusService.ReportFailure($"Mistral is unreachable: {transportException.Message}");
            throw new HttpRequestException($"Mistral request failed: {transportException.Message}", transportException);
        }

        if (MistralStatusService.IsOutageStatusCode((int)response!.StatusCode))
            await mistralStatusService.ReportFailure($"Mistral returned HTTP {(int)response.StatusCode}");

        return response;
    }

    /// <summary>
    /// Uses the server's own Retry-After header when present instead of guessing.
    /// Falls back to exponential backoff when header is absent.
    /// </summary>
    private TimeSpan GetRetryDelay(HttpResponseMessage? response, int attempt)
    {
        TimeSpan fallback = TimeSpan.FromSeconds(Math.Pow(2, attempt));
        RetryConditionHeaderValue? retryAfter = response?.Headers.RetryAfter;

        TimeSpan? serverDelay = retryAfter?.Delta
            ?? (retryAfter?.Date is DateTimeOffset date ? date - DateTimeOffset.UtcNow : null);

        if (serverDelay is not TimeSpan delay || delay <= TimeSpan.Zero)
            return fallback;

        TimeSpan capped = delay > TimeSpan.FromSeconds(MaxRetryAfterSeconds) ? TimeSpan.FromSeconds(MaxRetryAfterSeconds) : delay;
        logger.Debug("Using server-provided Retry-After of {DelaySeconds}s (capped at {MaxSeconds}s)", delay.TotalSeconds, MaxRetryAfterSeconds);
        return capped;
    }

    private object BuildBody(MistralChatRequest request, bool stream = false, string? modelOverride = null)
    {
        List<object> tools = [];

        if (request.Functions != null)
            tools.AddRange(request.Functions.Select(f => (object)new
            {
                type = "function",
                function = new { name = f.Name, description = f.Description, parameters = f.Parameters }
            }));

        if (request.Capabilities != null)
            tools.AddRange(request.Capabilities.Select(c => (object)new
            {
                type = CapabilityToString(c)
            }));

        return new
        {
            model = modelOverride ?? mediumModelName,
            messages = request.Messages,
            temperature = request.Temperature,
            reasoning_effort = request.ReasoningEffort == MistralReasoningEffort.Default ? null : EffortToString(request.ReasoningEffort),
            stream,
            tools = tools.Count > 0 ? tools.ToArray() : null,
            response_format = request.ResponseFormat switch
            {
                MistralResponseFormat.Json => (object)new { type = "json_object" },
                MistralResponseFormat.JsonSchema => new { type = "json_schema", json_schema = new { name = "response", schema = request.JsonSchema } },
                _ => new { type = "text" }
            },
            max_tokens = request.MaxTokens
        };
    }

    private static string ExtractTextContent(JsonElement content) => content.ValueKind switch
    {
        JsonValueKind.String => content.GetString() ?? "",
        // With reasoning enabled, a content array may hold only a ThinkChunk (type: "thinking")
        // during the thinking phase, with no "text"-typed element at all — fall back to "" rather
        // than calling GetProperty on the default/undefined JsonElement FirstOrDefault returns.
        JsonValueKind.Array => content.EnumerateArray()
            .FirstOrDefault(e => e.TryGetProperty("type", out var t) && t.GetString() == "text") is { ValueKind: JsonValueKind.Object } textChunk
                ? textChunk.GetProperty("text").GetString() ?? ""
                : "",
        _ => ""
    };

    private static bool IsContextLengthError(string body) =>
        body.Contains("context length", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("maximum context", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("too many tokens", StringComparison.OrdinalIgnoreCase);
    
    private static string CapabilityToString(MistralCapability c) => c switch
    {
        MistralCapability.CodeInterpreter => "code_interpreter",
        MistralCapability.WebSearch => "web_search",
        MistralCapability.PremiumWebSearch => "premium_web_search",
        MistralCapability.ImageGeneration => "image_generation",
        _ => throw new ArgumentOutOfRangeException()
    };

    private static string EffortToString(MistralReasoningEffort e) => e switch
    {
        MistralReasoningEffort.None => "none",
        MistralReasoningEffort.High => "high",
        _ => "none"
    };

    private class ToolCallBuilder
    {
        public string? Id;
        public string? Name;
        public StringBuilder Arguments = new();
    }
}