using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Services.Search.Models;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenAI.Chat;
using SignalRSwaggerGen.Attributes;

namespace Hubs;

[SignalRHub]
[Authorize]
public class Chat(AiClientProvider aiClientProvider, ChatService chatService, ResourceManager resourceManager, HybridSearchService hybridSearchService) : Hub
{
    private const string SystemPrompt = """
        You are a research assistant for a personal library. Your primary purpose is to help users explore, connect, and reason about content they have collected.

        Always search the library first before answering — even for general topics, there may be relevant resources, people, or organisations stored. Only skip searching if the question is purely conversational (greetings, thanks, etc.).

        You can:
        - Answer questions grounded in library sources
        - Find relations and connections between topics, people, and organisations
        - Synthesize insights across multiple sources
        - Enrich answers with your own reasoning on top of what you find

        CITATION RULES:
        - Only cite sources returned by the search_library tool.
        - Never fabricate, invent, or recall links from memory.
        - If you haven't searched, don't cite anything.
        - External links you know from training are not valid sources here.

        CITATION FORMAT:
        - Cite inline as separate markdown links: [1](link) [2](link) — never grouped like [1, 2]
        - Each citation must include the link: [N](link) — never bare [N] without a link
        - End with a Sources section: numbered markdown list of [Title](link)
        - Renumber sources sequentially starting from 1 using only the sources you cite.
        - Use the same number consistently for the same source throughout the response.

        For math use LaTeX: $$E=mc^2$$ for display, $x^2$ for inline.
    """;

    private static readonly ToolDefinition SearchTool = new(
        "search_library",
        "Search the personal library for resources, people, or organisations",
        new {
            type = "object",
            properties = new { query = new { type = "string", description = "Search query" } },
            required = new[] { "query" },
            additionalProperties = false
        }
    );

    private readonly Serilog.ILogger logger = Serilog.Log.ForContext<Chat>();

    public async Task<Guid> CreateChat(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            logger.Warning("Attempt to create chat with empty message from {UserIdentifier}", Context.UserIdentifier);
            throw new ArgumentException("Message cannot be null or empty.", nameof(message));
        }

        if (string.IsNullOrEmpty(Context.UserIdentifier))
        {
            logger.Error("User identifier is not available for chat creation");
            throw new InvalidOperationException("User identifier is required to create a chat.");
        }

        logger.Information("Creating new chat for user {UserIdentifier}", Context.UserIdentifier);

        try
        {
            Guid chatSessionId = await resourceManager.CreateChatAsync(new()
            {
                UserId = Guid.Parse(Context.UserIdentifier),
                Title = "New Chat",
            });

            logger.Information("Chat created with ID {ChatId}, generating title in background", chatSessionId);

            var caller = Clients.Caller;
            _ = GenerateTitleAsync(chatSessionId, message, caller);

            return chatSessionId;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to create chat for user {UserIdentifier}", Context.UserIdentifier);
            throw;
        }
    }

    private async Task GenerateTitleAsync(Guid chatId, string message, IClientProxy caller)
    {
        try
        {
            string title = await chatService.GenerateChatTitleAsync(message);
            await resourceManager.UpdateChatAsync(chatId, c => c.Title, title);
            await caller.SendAsync("ChatTitleUpdated", chatId.ToString());
            logger.Information("Title updated for chat {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to generate title for chat {ChatId}", chatId);
        }
    }

    public async IAsyncEnumerable<string> StreamAiResponse(
        string message,
        string chatId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        logger.Information("Streaming AI response for {UserIdentifier}", Context.UserIdentifier);

        if (string.IsNullOrWhiteSpace(message))
        {
            logger.Warning("Received empty message from {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        if (!ValidityUtil.IsValidId(chatId))
        {
            logger.Error("Invalid chat session ID: {ChatId}", chatId);
            yield break;
        }

        List<ChatMessage>? chatHistory = await LoadChatHistoryAsync(chatId);
        if (chatHistory == null) yield break;

        bool savedMessage = await SaveUserMessageAsync(message, chatId);
        if (!savedMessage) yield break;

        var response = new StringBuilder();

        var stream = StreamAgenticResponse(message, chatHistory, cancellationToken);

        await foreach (var content in stream)
        {
            response.Append(content);
            yield return content;
        }

        await SaveAiResponseAsync(response.ToString(), chatId);

        logger.Information("Finished streaming AI response to {UserIdentifier}", Context.UserIdentifier);
    }

    private async IAsyncEnumerable<string> StreamAgenticResponse(string message, List<ChatMessage> chatHistory, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Plain objects for tool loop (ChatService)
        List<object> rawMessages =
        [
            new { role = "system", content = SystemPrompt },
            ..chatHistory.Select(m => m switch
            {
                UserChatMessage u => (object)new { role = "user", content = u.Content[0].Text },
                AssistantChatMessage a => new { role = "assistant", content = a.Content[0].Text },
                _ => new { role = "user", content = "" }
            }),
            new { role = "user", content = message }
        ];

        // Typed messages for streaming final answer
        List<ChatMessage> typedMessages = [new SystemChatMessage(SystemPrompt), .. chatHistory, new UserChatMessage(message)];

        bool toolLoopError = false;
        for (int i = 0; i < 3; i++)
        {
            MistralCompletion completion = await chatService.CompleteChatWithToolsAsync(rawMessages.ToArray(), [SearchTool], cancellationToken: cancellationToken);

            if (completion.HasToolCalls)
            {
                // Add assistant tool_call message to both lists
                rawMessages.Add(new
                {
                    role = "assistant",
                    tool_calls = completion.ToolCalls!.Select(tc => new
                    {
                        id = tc.Id,
                        type = "function",
                        function = new { name = tc.Name, arguments = tc.Arguments }
                    }).ToArray()
                });

                typedMessages.Add(new AssistantChatMessage(completion.ToolCalls!
                    .Select(tc => ChatToolCall.CreateFunctionToolCall(tc.Id, tc.Name, BinaryData.FromString(tc.Arguments))).ToList()));

                foreach (MistralToolCall toolCall in completion.ToolCalls!)
                {
                    using var args = JsonDocument.Parse(toolCall.Arguments);
                    string query = args.RootElement.GetProperty("query").GetString() ?? message;

                    logger.Information("LLM searching for: {Query}", query);
                    await Clients.Caller.SendAsync("SearchStatus", query, cancellationToken);

                    HybridSearchResult searchResult = await hybridSearchService.SearchAsync(query, 1, 15, [], includeMetadataChunks: true);
                    string formatted = FormatSearchResults(searchResult);

                    rawMessages.Add(new { role = "tool", tool_call_id = toolCall.Id, content = formatted });
                    typedMessages.Add(new ToolChatMessage(toolCall.Id, formatted));
                }
            }
            else { break; }
        }

        if (toolLoopError)
        {
            yield return "An error occurred. Please try again.";
            yield break;
        }

        // Stream final answer
        ChatCompletionOptions streamOptions = new() { Temperature = 0.7f };
        AsyncCollectionResult<StreamingChatCompletionUpdate> stream;

        try
        {
            stream = aiClientProvider.ChatClient.CompleteChatStreamingAsync(typedMessages, streamOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Streaming error for {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        await foreach (StreamingChatCompletionUpdate update in stream)
            foreach (ChatMessageContentPart part in update.ContentUpdate)
                yield return part.Text;
    }
    
    private string FormatSearchResults(HybridSearchResult result)
    {
        var relevant = result.Items.Where(i => i.RelevanceScore >= 0.25f).ToList();

        if (relevant.Count == 0)
            return "No relevant results found";

        StringBuilder sb = new();

        for (int i = 0; i < relevant.Count; i++)
        {
            var item = relevant[i];
            sb.AppendLine($"[{i + 1}] {item.Name} ({item.Type})");
            sb.AppendLine($"Link: {aiClientProvider.HostUrl}/library?inspectorId={item.Id}&inspectorType={item.Type}");
            sb.AppendLine($"Relevance: {item.RelevanceScore:F2}");

            if (!string.IsNullOrWhiteSpace(item.Description))
                sb.AppendLine($"Description: {item.Description}");

            if (item.MatchedChunks.Count > 0)
                sb.AppendLine($"Content: {string.Join("\n", item.MatchedChunks)}");

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<List<ChatMessage>?> LoadChatHistoryAsync(string chatId)
    {
        try
        {
            Chats? chatMessages = await resourceManager.GetChatAsync(c => c.Id == Guid.Parse(chatId), includeProperties: "Messages");

            if (chatMessages == null || chatMessages.UserId != Guid.Parse(Context.UserIdentifier!))
            {
                logger.Warning("Chat with ID {ChatId} not found or user not authorized", chatId);
                return null;
            }

            return BuildChatHistory(chatMessages.Messages, chatId);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to load messages for chat {ChatId}", chatId);
            return null;
        }
    }

    private List<ChatMessage> BuildChatHistory(IEnumerable<dynamic> messages, string chatId)
    {
        var chatHistory = new List<ChatMessage>();

        foreach (var messageItem in messages)
        {
            ChatMessage? chatMessage = messageItem.MessageRole switch
            {
                var role when role == MessageRole.User.ToString() => new UserChatMessage(messageItem.Content),
                var role when role == MessageRole.Assistant.ToString() => new AssistantChatMessage(messageItem.Content),
                _ => null
            };

            if (chatMessage != null)
                chatHistory.Add(chatMessage);
            else
                logger.Warning("Unknown message role {MessageRole} in chat {ChatId}", messageItem.MessageRole, chatId);
        }

        return chatHistory;
    }

    private async Task<bool> SaveUserMessageAsync(string message, string chatId)
    {
        try
        {
            await resourceManager.CreateMessageAsync(new()
            {
                SenderId = Guid.Parse(Context.UserIdentifier!),
                ChatId = Guid.Parse(chatId),
                MessageRole = MessageRole.User.ToString(),
                Content = message
            });
            logger.Information("User message saved for chat {ChatId}", chatId);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save user message for chat {ChatId}", chatId);
            return false;
        }
    }

    private async Task<bool> SaveAiResponseAsync(string response, string chatId)
    {
        if (string.IsNullOrEmpty(response)) return false;

        try
        {
            await resourceManager.CreateMessageAsync(new()
            {
                SenderId = Guid.Parse(Context.UserIdentifier!),
                ChatId = Guid.Parse(chatId),
                MessageRole = MessageRole.Assistant.ToString(),
                Content = response
            });
            logger.Information("AI response saved for chat {ChatId}", chatId);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save AI response for chat {ChatId}", chatId);
            return false;
        }
    }

    public override async Task OnConnectedAsync()
    {
        logger.Information("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.Information("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
