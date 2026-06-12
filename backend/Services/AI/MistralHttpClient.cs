using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using KnowledgeBank.Utils;

namespace KnowledgeBank.Services.AI;

public enum MistralReasoningEffort { Default, None, High }
public enum MistralCapability { CodeInterpreter, WebSearch, PremiumWebSearch, ImageGeneration }
public enum MistralResponseFormat { Text, Json, JsonSchema }
public record MistralToolCall(string Id, string Name, string Arguments);
public record MistralCompletion(string? Content, List<MistralToolCall>? ToolCalls)
{
    public bool HasToolCalls => ToolCalls?.Count > 0;
}
public record MistralFunction(string Name, string Description, object Parameters);

public class MistralChatRequest
{
    public required List<object> Messages { get; init; }
    public float Temperature { get; init; } = 0.2f;
    public MistralReasoningEffort ReasoningEffort { get; init; }
    public MistralFunction[]? Functions { get; init; }
    public MistralCapability[]? Capabilities { get; init; }
    public MistralResponseFormat ResponseFormat { get; init; } = MistralResponseFormat.Text;
    public object? JsonSchema { get; init; }
}

public class MistralHttpClient
{
    private const int MaxRetries = 3;

    private readonly HttpClient httpClient;
    private readonly string modelName;
    private readonly Serilog.ILogger logger = Serilog.Log.ForContext<MistralHttpClient>();

    public MistralHttpClient(EnvironmentConfig environmentConfig)
    {
        modelName = environmentConfig.GetVariableValue(EnvironmentVariable.MEDIUM_MODEL_NAME);
        httpClient = new HttpClient
        {
            BaseAddress = new Uri(environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_ENDPOINT).TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(120)
        };
        httpClient.DefaultRequestHeaders.Add("Authorization",
            $"Bearer {environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_API_KEY)}");
    }

    public async Task<MistralCompletion> CompleteAsync(MistralChatRequest request, string? modelOverride = null, CancellationToken ct = default)
    {
        logger.Debug("Mistral call [{Model}]", modelOverride ?? modelName);
        var body = JsonSerializer.Serialize(BuildBody(request, modelOverride: modelOverride));

        HttpResponseMessage response = null!;
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            response = await httpClient.PostAsync("chat/completions", content, ct);

            if (response.IsSuccessStatusCode) break;

            if ((int)response.StatusCode is 503 or 529 or 429 && attempt < MaxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                continue;
            }

            string err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Mistral {response.StatusCode} — URL: {response.RequestMessage?.RequestUri} — Body: {err}");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var choice = doc.RootElement.GetProperty("choices")[0];
        var message = choice.GetProperty("message");
        string finishReason = choice.GetProperty("finish_reason").GetString() ?? "stop";

        if (finishReason == "tool_calls")
        {
            var toolCalls = message.GetProperty("tool_calls").EnumerateArray()
                .Select(tc => new MistralToolCall(
                    tc.GetProperty("id").GetString() ?? "",
                    tc.GetProperty("function").GetProperty("name").GetString() ?? "",
                    tc.GetProperty("function").GetProperty("arguments").GetString() ?? "{}"
                )).ToList();

            return new MistralCompletion(null, toolCalls);
        }

        return new MistralCompletion(ExtractTextContent(message.GetProperty("content")), null);
    }

    public async IAsyncEnumerable<string> StreamAsync(MistralChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(BuildBody(request, stream: true));

        HttpResponseMessage response = null!;
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

            if (response.IsSuccessStatusCode) break;

            if ((int)response.StatusCode is 503 or 529 or 429 && attempt < MaxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                continue;
            }

            string err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Mistral {response.StatusCode} — URL: {response.RequestMessage?.RequestUri} — Body: {err}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            string data = line["data: ".Length..];
            if (data == "[DONE]") yield break;

            using var doc = JsonDocument.Parse(data);
            var delta = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("delta");

            if (delta.TryGetProperty("content", out var contentProp))
            {
                string chunk = ExtractTextContent(contentProp);
                if (!string.IsNullOrEmpty(chunk))
                    yield return chunk;
            }
        }
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
            model = modelOverride ?? modelName,
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
            }
        };
    }

    private static string ExtractTextContent(JsonElement content) => content.ValueKind switch
    {
        JsonValueKind.String => content.GetString() ?? "",
        JsonValueKind.Array => content.EnumerateArray()
            .FirstOrDefault(e => e.TryGetProperty("type", out var t) && t.GetString() == "text")
            .GetProperty("text").GetString() ?? "",
        _ => ""
    };
    
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
}