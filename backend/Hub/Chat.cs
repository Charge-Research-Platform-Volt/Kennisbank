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
using SignalRSwaggerGen.Attributes;

namespace Hubs;

[SignalRHub]
[Authorize]
public partial class Chat(EnvironmentConfig environmentConfig, MistralHttpClient mistralClient, AiService aiService, ResourceManager resourceManager, HybridSearchService hybridSearchService) : Hub
{
    private const string SystemPrompt = """
        You are a research assistant for a personal library. Your primary purpose is to help users explore, connect, and reason about content they have collected.

        Always search the library first before answering — even for general topics, there may be relevant resources, people, or organisations stored. Only skip searching if the question is purely conversational (greetings, thanks, etc.).

        TOOL USAGE:
        - Use search_library to find relevant resources, people, or organisations. You can filter by type and limit results.
        - When search returns a specific person or organisation, always follow up with get_item_details to retrieve full information before answering.
        - Use find_related_items to explore connections — e.g. resources by a person, members of an organisation, people linked to a resource.
        - Chain tools when needed: search → get_item_details → find_related_items to build a complete picture.

        You can:
        - Answer questions grounded in library sources
        - Find relations and connections between topics, people, and organisations
        - Synthesize insights across multiple sources
        - Enrich answers with your own reasoning on top of what you find

        CITATION RULES (strictly enforced):
        - ONLY cite sources that appear in tool results. No exceptions.
        - NEVER invent, guess, or recall source links from memory — every link must come verbatim from tool results.
        - NEVER cite a source inline unless it also appears in the Sources section at the bottom.
        - If you haven't used any tools, cite nothing.

        CITATION FORMAT (strictly enforced):
        - Inline: each citation is a separate markdown link — [1](link) [2](link) — NEVER grouped as [1, 2] or [1,2]
        - Every inline citation MUST include the link: [N](link) — bare [N] without a link is forbidden
        - End response with a "Sources" section: numbered markdown list of [Title](link)
        - Only list sources you actually cited inline. Renumber sequentially from 1.
        - Use the same number for the same source throughout.

        For math use LaTeX: $$E=mc^2$$ for display, $x^2$ for inline.
    """;

    private const string TitleGenerationPrompt = """
        Generate a concise title (max 6 words) for a chat starting with the given message. Output only the title, nothing else.
    """;

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
            MistralCompletion result = await mistralClient.CompleteAsync(new MistralChatRequest
            {
                Messages = [
                    new { role = "system", content = TitleGenerationPrompt },
                    new { role = "user", content = message }
                ],
                Temperature = 0f
            });

            string title = string.IsNullOrWhiteSpace(result.Content) ? "Untitled Chat" : result.Content.Trim();
            await resourceManager.UpdateChatAsync(chatId, c => c.Title, title);
            await caller.SendAsync("ChatTitleUpdated", chatId.ToString());
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

        List<object>? chatHistory = await LoadChatHistoryAsync(chatId);
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

    private async IAsyncEnumerable<string> StreamAgenticResponse(string message, List<object> chatHistory, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Plain objects for tool loop (ChatService)
        List<object> messages =
        [
            new { role = "system", content = SystemPrompt },
            ..chatHistory,
            new { role = "user", content = message }
        ];

        for (int i = 0; i < 10; i++)
        {
            MistralCompletion completion = await mistralClient.CompleteAsync(new MistralChatRequest { Messages = messages, Functions = [SearchTool, GetItemDetailsTool, FindRelatedItemsTool] }, cancellationToken);

            if (completion.HasToolCalls)
            {
                // Add assistant tool_call message to both lists
                messages.Add(new
                {
                    role = "assistant",
                    tool_calls = completion.ToolCalls!.Select(tc => new
                    {
                        id = tc.Id,
                        type = "function",
                        function = new { name = tc.Name, arguments = tc.Arguments }
                    }).ToArray()
                });

                foreach (MistralToolCall toolCall in completion.ToolCalls!)
                {
                    using var args = JsonDocument.Parse(toolCall.Arguments);

                    string formatted = toolCall.Name switch
                    {
                        "search_library" => await HandleSearchAsync(args, message, cancellationToken),
                        "get_item_details" => await HandleGetItemDetailsAsync(args, cancellationToken),
                        "find_related_items" => await HandleFindRelatedItemsAsync(args, cancellationToken),
                        _ => "Unknown tool"
                    };

                    messages.Add(new { role = "tool", tool_call_id = toolCall.Id, content = formatted });
                }
            }
            else { break; }
        }

        // Stream final answer
        await Clients.Caller.SendAsync("Thinking", cancellationToken);

        await foreach (string chunk in mistralClient.StreamAsync(new MistralChatRequest { Messages = messages }, cancellationToken))
            yield return chunk;
    }
    
    private async Task<string> FormatSearchResultsAsync(HybridSearchResult result, string queryContext, CancellationToken ct)
    {
        var relevant = result.Items.Where(i => i.RelevanceScore >= 0.25f).ToList();

        if (relevant.Count == 0)
            return "No relevant results found";

        var summaryTasks = relevant.Select(item => item.MatchedChunks.Count > 0
            ? aiService.SummarizeChunksAsync(queryContext, item.Name, item.MatchedChunks, ct)
            : Task.FromResult(item.Description ?? ""));

        string[] summaries = await Task.WhenAll(summaryTasks);

        StringBuilder sb = new();
        string hostUrl = environmentConfig.GetVariableValue(EnvironmentVariable.HOST_URL).TrimEnd('/');

        for (int i = 0; i < relevant.Count; i++)
        {
            var item = relevant[i];
            sb.AppendLine($"[{i + 1}] {item.Name} ({item.Type})");
            sb.AppendLine($"Link: {hostUrl}/library?inspectorId={item.Id}&inspectorType={item.Type}");
            sb.AppendLine($"Relevance: {item.RelevanceScore:F2}");

            if (!string.IsNullOrWhiteSpace(summaries[i]))
                sb.AppendLine($"Content: {summaries[i]}");

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<List<object>?> LoadChatHistoryAsync(string chatId)
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

    private List<object> BuildChatHistory(IEnumerable<dynamic> messages, string chatId)
    {
        var chatHistory = new List<object>();

        foreach (var messageItem in messages)
        {
            string? role = messageItem.MessageRole switch
            {
                var r when r == MessageRole.User.ToString() => "user",
                var r when r == MessageRole.Assistant.ToString() => "assistant",
                _ => null
            };

            if (role != null)
                chatHistory.Add(new { role, content = (string)messageItem.Content });
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
