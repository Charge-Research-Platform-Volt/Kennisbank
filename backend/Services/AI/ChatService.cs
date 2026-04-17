using System.ClientModel;
using System.Text.Json;
using Serilog;

namespace KnowledgeBank.Services.AI;

public enum ReasoningEffort { None, High }
public record MistralToolCall(string Id, string Name, string Arguments);
public record MistralCompletion(string? Content, List<MistralToolCall>? ToolCalls)
{
    public bool HasToolCalls => ToolCalls?.Count > 0;
}
public record ToolDefinition(string Name, string Description, object Parameters);

public class ChatService(AiClientProvider aiClientProvider)
{
    private readonly Serilog.ILogger logger = Log.ForContext<ChatService>();

    private const string TitleGenerationPrompt = """
        Generate a concise title (max 6 words) for a chat starting with the given message. Output only the title, nothing else.
    """;

    private static string ExtractTextContent(JsonElement content) => content.ValueKind switch
    {
        JsonValueKind.String => content.GetString() ?? "",
        JsonValueKind.Array => content.EnumerateArray().FirstOrDefault(e => e.TryGetProperty("type", out var t) && t.GetString() == "text").GetProperty("text").GetString() ?? "",
        _ => ""
    };

    private async Task<string> CompleteChatRawAsync(object[] messages, ReasoningEffort reasoningEffort = ReasoningEffort.None)
    {
        logger.Debug("Starting title generation.");

        try
        {
            BinaryContent body = BinaryContent.Create(BinaryData.FromObjectAsJson(new
            {
                model = aiClientProvider.ChatModelName,
                messages,
                temperature = 0f,
                reasoning_effort = GetEffortString(reasoningEffort)
            }));

            ClientResult result = await aiClientProvider.ChatClient.CompleteChatAsync(body);

            logger.Debug("Raw response: {Response}", result.GetRawResponse().Content.ToString());

            using var doc = JsonDocument.Parse(result.GetRawResponse().Content);
            return ExtractTextContent(doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content"));
        }
        catch (Exception e)
        {
            logger.Error(e, "Raw chat completion failed");
            return "";
        }
    }

    public async Task<string> GenerateChatTitleAsync(string message)
    {
        object[] messages = [
            new { role = "system", content = TitleGenerationPrompt },
            new { role = "user", content = message },
        ];

        string result = await CompleteChatRawAsync(messages);
        return string.IsNullOrWhiteSpace(result) ? "Untitled Chat" : result.Trim();
    }

    public async Task<MistralCompletion> CompleteChatWithToolsAsync(object[] messages, ToolDefinition[] tools, CancellationToken cancellationToken = default, ReasoningEffort reasoningEffort = ReasoningEffort.None)
    {
        try
        {
            BinaryContent body = BinaryContent.Create(BinaryData.FromObjectAsJson(new
            {
                model = aiClientProvider.ChatModelName,
                messages,
                temperature = 0.7f,
                reasoning_effort = GetEffortString(reasoningEffort),
                tools = tools.Select(t => new { type = "function", function = new { name = t.Name, description = t.Description, parameters = t.Parameters } }).ToArray()
            }));

            ClientResult result = await aiClientProvider.ChatClient.CompleteChatAsync(body, new System.ClientModel.Primitives.RequestOptions { CancellationToken = cancellationToken });

            using var doc = JsonDocument.Parse(result.GetRawResponse().Content);
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
        catch (Exception ex)
        {
            logger.Error(ex, "CompleteChatWithToolsAsync failed");
            return new MistralCompletion("", null);
        }
    }
    
    private static string GetEffortString(ReasoningEffort reasoningEffort)
    {
        string effort = reasoningEffort switch
        {
            ReasoningEffort.None => "none",
            ReasoningEffort.High => "high",
            _ => "none"
        };

        return effort;
    }
}