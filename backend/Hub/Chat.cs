using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalRSwaggerGen.Attributes;

namespace Hubs;

[SignalRHub]
[Authorize]
public partial class Chat(MistralHttpClient mistralClient, AiService aiService, ChatService chatService, ResourceService resourceService, PersonService personService, OrganisationService organisationService, ProjectService projectService, LibraryService libraryService, IServiceScopeFactory scopeFactory) : Hub
{
    private static string BuildSystemPrompt(string? projectId)
    {
        string scope = projectId != null ? "this project" : "the library";
        string scopeAdverb = projectId != null ? "this project's content" : "the library";
        return $"""
        You are a research assistant for a personal {(projectId != null ? "project" : "library")}. Your primary purpose is to help users explore, connect, and reason about content they have collected.

        Always search {scopeAdverb} first before answering — even for general topics, there may be relevant resources, people, or organisations stored. Only skip searching if the question is purely conversational (greetings, thanks, etc.).

        Always give a response. If search returns no relevant results, you MUST start your response with a single sentence stating that {scope} contains no relevant information on this topic, before providing any general knowledge. Never return an empty response.
        NEVER invent specific details like dates, roles, job titles, or relationships that are not explicitly stated in tool results. If a detail is not in the tool results, do not include it.

        TOOL USAGE:
        - Use search_library to find relevant resources, people, or organisations. You can filter by type and limit results.
        - When search returns a specific person or organisation, always follow up with get_item_details to retrieve full information before answering.
        - Use find_related_items to explore connections — e.g. resources by a person, members of an organisation, people linked to a resource.
        - Chain tools when needed: search → get_item_details → find_related_items to build a complete picture.

        You can:
        - Answer questions grounded in {scope} sources
        - Find relations and connections between topics, people, and organisations
        - Draw insights across multiple sources
        - Add brief clarifying context from general knowledge when {scope} sources are insufficient — wrap the entire block of general knowledge (including any lists or paragraphs) in a single [AI]...[/AI] tag
        - Do NOT wrap individual sentences — wrap the whole section at once
        - Do NOT write "(AI)" labels, headers like "General Context", or any other annotations — the [AI]...[/AI] tags handle this automatically
        - If you are unsure whether something comes from {scope} or your training data, wrap it in [AI]...[/AI]

        CITATION RULES (strictly enforced):
        - ONLY cite sources that appear in tool results. No exceptions.
        - Cite inline using ONLY the exact marker from tool results: [SRC:uuid]
        - Each marker must be separate — NEVER group like [SRC:uuid,SRC:uuid]
        - NEVER write [1], [2] or any numbered citation — ONLY [SRC:uuid] markers
        - NEVER invent UUIDs — copy markers verbatim from "Cite as:" lines in tool results
        - Do NOT write a Sources section — it is generated automatically
        - If you haven't used any tools, cite nothing.

        For math use LaTeX: $$E=mc^2$$ for display, $x^2$ for inline.
        """;
    }

    private const string TitleGenerationPrompt = """
        Generate a concise title (max 6 words) for a chat starting with the given message. Output only the title, nothing else. No markdown, no quotes, no punctuation.
    """;

    private const float RelevanceThreshold = 0.25f;
    private const int MaxToolIterations = 10;

    private readonly Serilog.ILogger logger = Serilog.Log.ForContext<Chat>();

    public async Task<Guid> CreateChat(string message, string? projectId)
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
            Guid chatSessionId = await chatService.CreateChatAsync(new ChatsCreateDto
            {
                UserId = Guid.Parse(Context.UserIdentifier),
                Title = "New Chat",
                ProjectId = projectId != null ? Guid.Parse(projectId) : null,
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

            string title = string.IsNullOrWhiteSpace(result.Content) ? "Untitled Chat" : result.Content.Trim().Trim('*', '_', '`', '#', '"', '\'').Trim();
            await using var scope = scopeFactory.CreateAsyncScope();
            var scopedChatService = scope.ServiceProvider.GetRequiredService<ChatService>();
            await scopedChatService.UpdateTitleAsync(chatId, title);
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
        string? projectId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        logger.Information("Streaming AI response for {UserIdentifier}", Context.UserIdentifier);

        if (string.IsNullOrWhiteSpace(message))
        {
            logger.Warning("Received empty message from {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        if (!Guid.TryParse(chatId, out _))
        {
            logger.Error("Invalid chat session ID: {ChatId}", chatId);
            yield break;
        }

        List<object>? chatHistory = await LoadChatHistoryAsync(chatId);
        if (chatHistory == null) yield break;

        bool savedMessage = await SaveUserMessageAsync(message, chatId);
        if (!savedMessage) yield break;

        var response = new StringBuilder();

        var stream = StreamAgenticResponse(message, chatHistory, projectId, cancellationToken);

        await foreach (var content in stream)
        {
            response.Append(content);
            yield return content;
        }

        await SaveAiResponseAsync(response.ToString(), chatId);

        logger.Information("Finished streaming AI response to {UserIdentifier}", Context.UserIdentifier);
    }

    private async IAsyncEnumerable<string> StreamAgenticResponse(string message, List<object> chatHistory, string? projectId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        List<object> messages =
        [
            new { role = "system", content = BuildSystemPrompt(projectId) },
            ..chatHistory,
            new { role = "user", content = message }
        ];

        for (int i = 0; i < MaxToolIterations; i++)
        {
            MistralCompletion completion = await mistralClient.CompleteAsync(new MistralChatRequest { Messages = messages, Functions = [SearchTool, GetItemDetailsTool, FindRelatedItemsTool] }, ct: cancellationToken);

            if (completion.HasToolCalls)
            {
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
                    string formatted;
                    try
                    {
                        using var args = JsonDocument.Parse(toolCall.Arguments);
                        formatted = toolCall.Name switch
                        {
                            "search_library" => await HandleSearchAsync(args, message, projectId, cancellationToken),
                            "get_item_details" => await HandleGetItemDetailsAsync(args, cancellationToken),
                            "find_related_items" => await HandleFindRelatedItemsAsync(args, cancellationToken),
                            _ => "Error: unknown tool"
                        };
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Tool call failed for {ToolName} with args: {Args}", toolCall.Name, toolCall.Arguments);
                        formatted = "Tool call failed";
                    }

                    messages.Add(new { role = "tool", tool_call_id = toolCall.Id, content = formatted });
                }
            }
            else { break; }
        }

        await Clients.Caller.SendAsync("Thinking", cancellationToken);

        await foreach (string chunk in mistralClient.StreamAsync(new MistralChatRequest { Messages = messages }, cancellationToken))
            yield return chunk;
    }

    private async Task<string> FormatSearchResultsAsync(List<LibraryItemWithChunks> results, string userQuestion, string searchQuery, CancellationToken ct)
    {
        var relevant = results.Where(i => i.Score >= RelevanceThreshold).ToList();

        if (relevant.Count == 0)
            return "No relevant results found";

        var summaryTasks = relevant.Select(item => item.MatchedChunks.Count > 0
            ? aiService.SummarizeChunksAsync(userQuestion, searchQuery, item.Item.Name, item.MatchedChunks, ct)
            : Task.FromResult(item.Item.Description ?? ""));

        string[] summaries = await Task.WhenAll(summaryTasks);

        StringBuilder sb = new();

        for (int i = 0; i < relevant.Count; i++)
        {
            var item = relevant[i];
            sb.AppendLine($"{item.Item.Name} ({item.Item.Type})");
            sb.AppendLine($"Cite as: [SRC:{item.Item.Id}]");
            sb.AppendLine($"Relevance: {item.Score:F2}");

            if (!string.IsNullOrWhiteSpace(summaries[i]) && summaries[i] != "NO_RELEVANT_CONTENT")
                sb.AppendLine($"Content: {summaries[i]}");

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<List<object>?> LoadChatHistoryAsync(string chatId)
    {
        try
        {
            Chats? chat = await chatService.GetWithMessagesAsync(Guid.Parse(chatId));

            if (chat == null || chat.UserId != Guid.Parse(Context.UserIdentifier!))
            {
                logger.Warning("Chat with ID {ChatId} not found or user not authorized", chatId);
                return null;
            }

            return BuildChatHistory(chat.Messages, chatId);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to load messages for chat {ChatId}", chatId);
            return null;
        }
    }

    private List<object> BuildChatHistory(IEnumerable<Messages> messages, string chatId)
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
                chatHistory.Add(new { role, content = messageItem.Content });
            else
                logger.Warning("Unknown message role {MessageRole} in chat {ChatId}", messageItem.MessageRole, chatId);
        }

        return chatHistory;
    }

    private async Task<bool> SaveUserMessageAsync(string message, string chatId)
    {
        try
        {
            await chatService.CreateMessageAsync(new MessagesCreateDto
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
            await chatService.CreateMessageAsync(new MessagesCreateDto
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