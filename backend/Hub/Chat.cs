using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using HandlebarsDotNet;
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
        You are an AI assistant that answers questions based on provided sources from a knowledge base.

        CITATION FORMAT:
        - Each source has a pre-assigned Source Number — use it exactly, do not renumber.
        - Cite inline using the number as a markdown link: [1](link), [2](link), etc.
        - Use the link exactly as provided. Never modify, expand, or add a domain to it. Never use bare [1] without (link).
        - The same source always gets the same number and link throughout the response.
        - Do not repeat the full title inline — just the number.
        - At the very end, add a Sources section as a numbered markdown list:
          1. [Exact Source Title](link)
          2. [Exact Source Title](link)

        CORRECT:
        ✓ Framing effects were observed [1](https://yourdomain.com/library?inspectorId=abc&inspectorType=resource).
        ✓ **Sources**
          1. [Food Recommender Systems](https://yourdomain.com/library?inspectorId=abc&inspectorType=resource)

        WRONG:
        ✗ [Food Recommender Systems](https://yourdomain.com/library?inspectorId=abc&inspectorType=resource) — do not use full titles inline
        ✗ [Source 1](https://yourdomain.com/library?inspectorId=abc&inspectorType=resource) — do not use "Source N" format

        Instructions:
        - Base your answer only on the provided sources.
        - Be concise, accurate, and directly address the question.
        - Consider chat history when formulating your answer.
        - If the sources do not answer the question, say so and do not include citations.
        - For math use LaTeX: $$E=mc^2$$ for display, $x^2$ for inline.
        """;

    private const string StandardSystemPrompt = """
        You are an AI assistant that helps people find information.
        For math use LaTeX: $$E=mc^2$$ for display, $x^2$ for inline.
        """;

    private static readonly HandlebarsTemplate<object, object> SourcesTemplate = Handlebars.Compile("""
        The question:
        {{query}}

        Relevant sources (ranked by relevance):
        {{#each content}}
        ---
        Source Number: {{SourceNumber}}
        Title: {{Title}}
        Relevance: {{RelevanceLevel}}
        Text: {{Text}}
        Link: {{Link}}
        {{/each}}
        ---
        """);

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
        bool contentBased,
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

        var stream = contentBased
            ? StreamContentBasedAiResponse(message, chatHistory, cancellationToken)
            : StreamStandardAiResponse(message, chatHistory, cancellationToken);

        await foreach (var content in stream)
        {
            response.Append(content);
            yield return content;
        }

        await SaveAiResponseAsync(response.ToString(), chatId);

        logger.Information("Finished streaming AI response to {UserIdentifier}", Context.UserIdentifier);
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

    private async IAsyncEnumerable<string> StreamContentBasedAiResponse(
        string query,
        List<ChatMessage> chatHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        logger.Information("Content-based AI initiated with query: {Query}", query);

        HybridSearchResult? searchResult = null;
        try
        {
            searchResult = await hybridSearchService.SearchAsync(
                searchQuery: query,
                pageIndex: 1,
                pageSize: 50,
                filters: []
            );
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error in hybrid search for query: {Query}", query);
        }

        if (searchResult == null)
        {
            yield return "I encountered an error while searching for information. Please try again.";
            yield break;
        }

        if (searchResult.Items.Length == 0)
        {
            logger.Warning("No search results found for query: {Query}", query);
            yield return "I couldn't find any relevant information in the knowledge base to answer your question. Please try rephrasing your question or ask about a different topic.";
            yield break;
        }

        logger.Information("Hybrid search returned {Count} results with average score {AvgScore:F3}",
            searchResult.Items.Length, searchResult.Metadata?.AverageScore ?? 0);

        var contentWithScores = searchResult.Items
            .Where(item => item.MatchedChunks != null && item.MatchedChunks.Any(c => !string.IsNullOrWhiteSpace(c)))
            .Select((item, index) => new
            {
                SourceNumber = index + 1,
                Text = string.Join("\n", item.MatchedChunks ?? []),
                Link = $"{aiClientProvider.HostUrl}/library?inspectorId={item.Id}&inspectorType={item.Type}",
                item.RelevanceScore,
                Title = item.Name,
                RelevanceLevel = item.RelevanceScore switch
                {
                    >= 0.7f => "High",
                    >= 0.4f => "Medium",
                    _ => "Low"
                }
            })
            .ToList();

        if (contentWithScores.Count == 0)
        {
            logger.Warning("No content chunks found in search results for query: {Query}", query);
            yield return "I found some results but they don't contain detailed content to answer your question. Please try a more specific question.";
            yield break;
        }

        string prompt = SourcesTemplate(new { query, content = contentWithScores });

        List<ChatMessage> messages =
        [
            new SystemChatMessage(SystemPrompt),
            .. chatHistory,
            new UserChatMessage(prompt),
        ];

        AsyncCollectionResult<StreamingChatCompletionUpdate> responseStreaming;
        try
        {
            responseStreaming = aiClientProvider.ChatClient.CompleteChatStreamingAsync(messages, new ChatCompletionOptions { Temperature = 0.3f }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error while getting streaming response for {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        await foreach (StreamingChatCompletionUpdate update in responseStreaming)
        {
            foreach (ChatMessageContentPart updatePart in update.ContentUpdate)
                yield return updatePart.Text;
        }
    }

    private async IAsyncEnumerable<string> StreamStandardAiResponse(
        string message,
        List<ChatMessage> chatHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        logger.Information("Standard AI initiated with message: {Message}", message);

        List<ChatMessage> messages =
        [
            new SystemChatMessage(StandardSystemPrompt),
            .. chatHistory,
            new UserChatMessage(message)
        ];

        AsyncCollectionResult<StreamingChatCompletionUpdate> response;
        try
        {
            response = aiClientProvider.ChatClient.CompleteChatStreamingAsync(messages, new ChatCompletionOptions { Temperature = 0.7f }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error while getting streaming response for {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        await foreach (StreamingChatCompletionUpdate update in response)
        {
            foreach (ChatMessageContentPart updatePart in update.ContentUpdate)
                yield return updatePart.Text;
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
