using System.ClientModel;
using System.Text.Json;
using OpenAI.Chat;
using Serilog;

namespace KnowledgeBank.Services.AI;

public class ChatService(AiClientProvider aiClientProvider)
{
    private readonly Serilog.ILogger logger = Log.ForContext<ChatService>();

    private const string TitleGenerationPrompt = """
        You are an assistant that generates concise chat titles.
    """;

    private const string ChatTitleOutputJsonSchema = """
        {
            "type": "object",
            "properties": {
                "Title": { "type": "string" }
            },
            "required": ["Title"]
        }
    """;


    public async Task<string> GenerateChatTitleAsync(string query)
    {
        logger.Information("Generation chat title for query");

        try
        {
            string prompt = $"Generate a title for a chat that starts with this message: {query}";

            ChatCompletionOptions options = new()
            {
                Temperature = 0.5f,
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    "TitleExtraction",
                    BinaryData.FromString(ChatTitleOutputJsonSchema)
                )
            };

            List<ChatMessage> messages = [
                new SystemChatMessage(TitleGenerationPrompt),
                new UserChatMessage(prompt)
            ];

            ClientResult<ChatCompletion> response = await aiClientProvider.ChatClient.CompleteChatAsync(messages, options);
            TitleGeneration? result = JsonSerializer.Deserialize<TitleGeneration>(response.Value.Content[0].Text);

            return result?.Title ?? "Untitled Chat";
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to generate chat title");
            return "Untitled Chat";
        }
    }

    private class TitleGeneration
    {
        public required string Title { get; set; }
    }
}