using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalRSwaggerGen.Attributes;
using KnowledgeBank.Services.Search;

namespace Hubs;

[SignalRHub]
[Authorize]
public partial class Chat(MistralHttpClient mistralClient, AiService aiService, ChatService chatService, AttachmentChunkSearchIndexService attachmentChunkSearchIndexService, EmbeddingService embeddingService, IServiceScopeFactory scopeFactory) : Hub
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
        - If a search returns no relevant results, try again with a broader or differently-worded query before giving up — but if two or three attempts still find nothing useful, stop searching rather than repeating similar queries.
        - search_library results are summarized for brevity. If a result looks relevant but you need more depth or detail than the summary gives, use search_item_content on that item's id to pull more from its full content — most useful for resources.
        - You can call multiple tools in a single turn when you have several distinct angles to cover — e.g. multiple search_library calls with different queries, or search_item_content on several items at once — instead of spreading them one at a time across turns.
        - The list of files attached to this chat (if any) is always provided as a separate system message — use search_attachment_content with the attachment's id to look up relevant sections.
        - Attachments and {scope} search are complementary, not alternatives — even when an attached file directly and thoroughly answers the question, still call search_library for the same topic, since {scope} may hold separate relevant resources, people, or organisations the attachment doesn't cover.

        You can:
        - Answer questions grounded in {scope} sources
        - Find relations and connections between topics, people, and organisations
        - Draw insights across multiple sources
        - Add brief clarifying context from general knowledge when {scope} sources are insufficient — wrap the entire block of general knowledge (including any lists or paragraphs) in a single [AI]...[/AI] tag
        - Do NOT wrap individual sentences — wrap the whole section at once
        - Do NOT write "(AI)" labels, headers like "General Context", or any other annotations — the [AI]...[/AI] tags handle this automatically
        - If you are unsure whether something comes from {scope} or your training data, wrap it in [AI]...[/AI]
        - Content from attached files is grounded, user-provided information, NOT general training data — treat it the same as {scope} sources and do NOT wrap it in [AI]...[/AI], even though it has no [SRC:...] marker.

        CITATION RULES (strictly enforced):
        - ONLY cite sources that appear in tool results. No exceptions.
        - Cite inline using ONLY the exact marker from tool results: [SRC:uuid]
        - Each marker must be separate — NEVER group like [SRC:uuid,SRC:uuid]
        - NEVER write [1], [2] or any numbered citation — ONLY [SRC:uuid] markers
        - NEVER invent UUIDs — copy markers verbatim from "Cite as:" lines in tool results
        - Do NOT write a Sources section — it is generated automatically
        - If you haven't used any tools, cite nothing.
        - Attached files are NOT library sources — never use [SRC:...] markers for them, since no link can be generated for an attachment. Refer to them by name in plain text instead (e.g. "the attached file report.pdf states that...").

        For math use LaTeX: $$E=mc^2$$ for display, $x^2$ for inline.
        """;
    }

    private const string TitleGenerationPrompt = """
        Generate a concise title (max 6 words) for a chat starting with the given message. Output only the title, nothing else. No markdown, no quotes, no punctuation.
    """;

    private const float RelevanceThreshold = 0.25f;
    private const int MaxToolIterations = 14;
    private const int MaxHistoryTokens = 80_000;
    private const int MaxSummaryTokens = 2000;
    private const int MessagesKeptUncompacted = 10;

    private static readonly Regex SourceMarkerPattern = new(@"\[SRC:[0-9a-fA-F]+\]", RegexOptions.Compiled);
    private static readonly Regex AiTagPattern = new(@"\[/?AI\]", RegexOptions.Compiled);
    private static string StripCitationMarkers(string text) => SourceMarkerPattern.Replace(text, "").Trim();
    private static string StripAiTags(string text) => AiTagPattern.Replace(text, "").Trim();

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

    private async Task CompactChatAsync(Chats chat) {
        Messages[] uncompacted = await chatService.GetMessagesForContextAsync(chat.Id, chat.SummarizedThroughCreatedOn);
        if (uncompacted.Length <= MessagesKeptUncompacted) return;

        var toFold = uncompacted.Take(uncompacted.Length - MessagesKeptUncompacted).ToList();
        DateTime cutoff = toFold[^1].CreatedOn;
        string transcript = string.Join("\n\n", toFold.Select(m => $"{m.MessageRole}: {m.Content}"));

        string prompt = $"""
            Summarize this excerpt of a conversation between a user and a research-assistant chatbot, in a concise paragraph (aim for around 600 words).
            Focus on: what the user is trying to accomplish or find out, key facts or conclusions established, and any preferences or constraints the user stated.
            Discard any information that is not relevant to the current conversation.
            Do not include any [SRC:...] citation markers or [AI][/AI] tags — this summary is background context only, never a source to cite from.
            {(string.IsNullOrEmpty(chat.ContextSummary) ? "" : $"Existing summary of earlier parts of the conversation — preserve its important details, don't drop them:\n{chat.ContextSummary}\n\nIncorporate this with the new content below into one updated summary.")}

            Conversation excerpt:
            {transcript}
        """;

        MistralCompletion result = await mistralClient.CompleteAsync(new MistralChatRequest
        {
            Messages = [new { role = "user", content = prompt }],
            Temperature = 0f,
            ReasoningEffort = MistralReasoningEffort.None,
            MaxTokens = MaxSummaryTokens
        });

        string summary = StripAiTags(StripCitationMarkers(result.Content ?? ""));

        await chatService.UpdateCompactionAsync(chat.Id, summary, cutoff);
        chat.ContextSummary = summary;
        chat.SummarizedThroughCreatedOn = cutoff;

        logger.Information("Compacted chat {ChatId} through {Cutoff}, folded {Count} messages", chat.Id, cutoff, toFold.Count);
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
        List<string>? attachmentIds,
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

        Guid? savedMessageId = await SaveUserMessageAsync(message, chatId);
        if (savedMessageId == null) yield break;

        List<MessageAttachments> attachments = [];
        if (attachmentIds is { Count: > 0 })
        {
            await chatService.LinkAttachmentsToMessageAsync(Guid.Parse(chatId), savedMessageId.Value, attachmentIds.Select(Guid.Parse).ToList());
            attachments = await chatService.GetAttachmentsForMessageAsync(savedMessageId.Value);
        }

        var (messageContent, attachmentNote) = BuildMessageWithAttachments(message, attachments);

        var response = new StringBuilder();
        var stream = StreamAgenticResponse(chatId, message, messageContent, attachmentNote, chatHistory, projectId, cancellationToken);
        await using var enumerator = stream.GetAsyncEnumerator(cancellationToken);
        bool hasNext = true;

        while (hasNext) {
            string? errorNotice = null;

            try {
                hasNext = await enumerator.MoveNextAsync();
            }
            catch (MistralContextLengthExceededException) {
                logger.Warning("Chat {ChatId} exceeded the model's context length", chatId);
                errorNotice = "This conversation has become too long for me to process. Please use **Compact** to summarize older messages, or start a new chat.";
            }
            catch (Exception ex) {
                logger.Error(ex, "Streaming failed for chat {ChatId}", chatId);
                errorNotice = "Something went wrong while generating a response. Please try again.";
            }

            if (errorNotice != null) {
                response.Append(errorNotice);
                yield return errorNotice;
                break;
            }

            if (hasNext) {
                response.Append(enumerator.Current);
                yield return enumerator.Current;
            }
        }

        if (response.Length == 0)
        {
            logger.Warning("Chat {ChatId} received an empty response from the model", chatId);
            string fallback = "I wasn't able to generate a response. Please try again.";
            response.Append(fallback);
            yield return fallback;
        }

        await SaveAiResponseAsync(response.ToString(), chatId);

        logger.Information("Finished streaming AI response to {UserIdentifier}", Context.UserIdentifier);
    }

    private async IAsyncEnumerable<string> StreamAgenticResponse(string chatId, string message, string messageContent, string? attachmentNote, List<object> chatHistory, string? projectId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        List<object> messages = [new { role = "system", content = BuildSystemPrompt(projectId) }];

        List<MessageAttachments> allAttachments = await chatService.GetAttachmentsForChatAsync(Guid.Parse(chatId));
        if (allAttachments.Count > 0)
            messages.Add(new { role = "system", content = BuildAttachmentsListText(allAttachments) });

        messages.AddRange(chatHistory);
        messages.Add(new { role = "user", content = messageContent });

        if (attachmentNote != null)
            messages.Add(new { role = "system", content = attachmentNote });

        for (int i = 0; i < MaxToolIterations; i++)
        {
            MistralCompletion completion = await mistralClient.CompleteAsync(new MistralChatRequest { Messages = messages, Functions = [SearchTool, GetItemDetailsTool, FindRelatedItemsTool, SearchItemContentTool, SearchAttachmentContentTool] }, ct: cancellationToken);

            if (i == 0 && completion.PromptTokens is int promptTokens)
                await chatService.UpdateLastContextTokensAsync(Guid.Parse(chatId), promptTokens);

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

                string[] results = await Task.WhenAll(completion.ToolCalls!.Select(tc => HandleToolCallAsync(tc, message, projectId, Guid.Parse(chatId), cancellationToken)));

                for (int j = 0; j < completion.ToolCalls!.Count; j++)
                    messages.Add(new { role = "tool", tool_call_id = completion.ToolCalls[j].Id, content = results[j] });

                if (i == MaxToolIterations - 1)
                    messages.Add(new { role = "system", content = "You've reached the maximum number of tool calls for this turn. Provide your best answer now based on the information gathered so far." });
            }
            else { break; }
        }

        await Clients.Caller.SendAsync("Thinking", cancellationToken);

        bool anyContent = false;
        await foreach (string chunk in mistralClient.StreamAsync(new MistralChatRequest { Messages = messages }, cancellationToken))
        {
            anyContent = true;
            yield return chunk;
        }

        if (!anyContent)
        {
            logger.Warning("Chat {ChatId} got an empty stream on the first attempt, retrying once", chatId);
            await foreach (string chunk in mistralClient.StreamAsync(new MistralChatRequest { Messages = messages }, cancellationToken))
                yield return chunk;
        }
    }

    private async Task<string> HandleToolCallAsync(MistralToolCall toolCall, string message, string? projectId, Guid chatId, CancellationToken cancellationToken)
    {
        try
        {
            using var args = JsonDocument.Parse(toolCall.Arguments);
            await using var scope = scopeFactory.CreateAsyncScope();

            return toolCall.Name switch
            {
                "search_library" => await HandleSearchAsync(
                    scope.ServiceProvider.GetRequiredService<LibraryService>(),
                    scope.ServiceProvider.GetRequiredService<ProjectService>(),
                    args, message, projectId, cancellationToken),
                "get_item_details" => await HandleGetItemDetailsAsync(
                    scope.ServiceProvider.GetRequiredService<ResourceService>(),
                    scope.ServiceProvider.GetRequiredService<PersonService>(),
                    scope.ServiceProvider.GetRequiredService<OrganisationService>(),
                    args, cancellationToken),
                "find_related_items" => await HandleFindRelatedItemsAsync(
                    scope.ServiceProvider.GetRequiredService<ResourceService>(),
                    scope.ServiceProvider.GetRequiredService<PersonService>(),
                    scope.ServiceProvider.GetRequiredService<OrganisationService>(),
                    args, cancellationToken),
                "search_item_content" => await HandleSearchItemContentAsync(
                    scope.ServiceProvider.GetRequiredService<LibraryService>(),
                    args, cancellationToken),
                "search_attachment_content" => await HandleSearchAttachmentContentAsync(
                    scope.ServiceProvider.GetRequiredService<ChatService>(),
                    args, chatId, cancellationToken),
                _ => "Error: unknown tool"
            };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Tool call failed for {ToolName} with args: {Args}", toolCall.Name, toolCall.Arguments);
            return $"Tool call failed: {ex.Message}";
        }
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
            Chats? chat = await chatService.GetByIdAsync(Guid.Parse(chatId));

            if (chat == null || chat.UserId != Guid.Parse(Context.UserIdentifier!))
            {
                logger.Warning("Chat with ID {ChatId} not found or user not authorized", chatId);
                return null;
            }

            if (chat.LastContextTokens is int t && t >= MaxHistoryTokens)
            {
                try { await CompactChatAsync(chat); }
                catch (Exception ex) { logger.Error(ex, "Compaction failed for chat {ChatId}", chatId); }
            }

            Messages[] recentMessages = await chatService.GetMessagesForContextAsync(chat.Id, chat.SummarizedThroughCreatedOn);
            return BuildChatHistory(chat, recentMessages, chatId);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to load messages for chat {ChatId}", chatId);
            return null;
        }
    }

    private List<object> BuildChatHistory(Chats chat, IEnumerable<Messages> recentMessages, string chatId)
    {
        List<object> chatHistory = [];

        if (!string.IsNullOrEmpty(chat.ContextSummary))
            chatHistory.Add(new { role = "assistant", content = $"[Summary of earlier conversation]\n{chat.ContextSummary}" });

        foreach (var messageItem in recentMessages)
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

    private async Task<Guid?> SaveUserMessageAsync(string message, string chatId)
    {
        try
        {
            Guid messageId = await chatService.CreateMessageAsync(new MessagesCreateDto
            {
                SenderId = Guid.Parse(Context.UserIdentifier!),
                ChatId = Guid.Parse(chatId),
                MessageRole = MessageRole.User.ToString(),
                Content = message
            });
            logger.Information("User message saved for chat {ChatId}", chatId);
            return messageId;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save user message for chat {ChatId}", chatId);
            return null;
        }
    }

    private static (string Content, string? AttachmentNote) BuildMessageWithAttachments(string message, List<MessageAttachments> attachments)
    {
        if (attachments.Count == 0) return (message, null);

        StringBuilder content = new(message);
        StringBuilder note = new();

        foreach (var attachment in attachments) 
        {
            if (!attachment.IsChunked)
            {
                content.AppendLine();
                content.AppendLine();
                content.AppendLine($"[Attached file: {attachment.FileName}]");
                content.AppendLine(attachment.ExtractedText);
            }
            else
            {
                note.AppendLine($"The user just attached a large file, \"{attachment.FileName}\" (id: {attachment.Id}). Use search_attachment_content with a relevant query to look up sections before answering.");
            }
        }

        return (content.ToString(), note.Length > 0 ? note.ToString() : null);
    }

    private static string BuildAttachmentsListText(List<MessageAttachments> attachments)
    {
        StringBuilder sb = new();
        sb.AppendLine("Files attached to this chat:");

        foreach (var attachment in attachments)
        {
            string sizeLabel = attachment.IsChunked ? " [large document]" : " [small document]";
            string descriptionLabel = string.IsNullOrEmpty(attachment.Description) ? "" : $" — {attachment.Description}";
            sb.AppendLine($"- {attachment.FileName} (id: {attachment.Id}){sizeLabel}{descriptionLabel}");
        }

        return sb.ToString();
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