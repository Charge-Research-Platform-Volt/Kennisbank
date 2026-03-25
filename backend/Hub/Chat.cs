using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services;
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
public class Chat : Hub
{
    private readonly Serilog.ILogger _logger;
    private readonly RAGSystem _ragSystem;
    private readonly RAGManager _ragManager;
    private readonly ResourceManager _resourceManager;
    private readonly HybridSearchService _hybridSearchService;

    public Chat(RAGSystem ragSystem, RAGManager ragManager, ResourceManager resourceManager, HybridSearchService hybridSearchService)
    {
        _logger = Serilog.Log.ForContext<Chat>();
        _ragSystem = ragSystem;
        _ragManager = ragManager;
        _resourceManager = resourceManager;
        _hybridSearchService = hybridSearchService;
    }

    /// <summary>
    /// Creates a new chat session with an AI-generated title based on the initial message.
    /// </summary>
    /// <param name="message">The initial message used to generate the chat title.</param>
    /// <returns>The unique identifier of the newly created chat session.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user identifier is not available.</exception>
    /// <exception cref="ArgumentException">Thrown when the message is null or whitespace.</exception>
    public async Task<Guid> CreateChat(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.Warning("Attempt to create chat with empty message from {UserIdentifier}", Context.UserIdentifier);
            throw new ArgumentException("Message cannot be null or empty.", nameof(message));
        }

        if (string.IsNullOrEmpty(Context.UserIdentifier))
        {
            _logger.Error("User identifier is not available for chat creation");
            throw new InvalidOperationException("User identifier is required to create a chat.");
        }

        _logger.Information("Creating new chat for user {UserIdentifier}", Context.UserIdentifier);

        try
        {
            ChatsCreateDto chat = new ChatsCreateDto
            {
                UserId = Guid.Parse(Context.UserIdentifier),
                Title = "New Chat",
            };

            Guid chatSessionId = await _resourceManager.CreateChatAsync(chat);

            _logger.Information("Chat created with ID {ChatId}, generating title in background", chatSessionId);

            // Generate title in the background — capture caller proxy before returning
            var caller = Clients.Caller;
            _ = GenerateTitleAsync(chatSessionId, message, caller);

            return chatSessionId;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create chat for user {UserIdentifier}", Context.UserIdentifier);
            throw;
        }
    }

    private async Task GenerateTitleAsync(Guid chatId, string message, IClientProxy caller)
    {
        try
        {
            string title = await _ragManager.GenerateChatTitleAsync(message);
            await _resourceManager.UpdateChatAsync(chatId, c => c.Title, title);
            await caller.SendAsync("ChatTitleUpdated", chatId.ToString());
            _logger.Information("Title updated for chat {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate title for chat {ChatId}", chatId);
        }
    }

    /// <summary>
    /// Streams an AI response asynchronously for a given message within a chat session.
    /// </summary>
    /// <param name="message">The user message to process. Must not be null or whitespace.</param>
    /// <param name="contentBased">Indicates whether the response should be content-based.</param>
    /// <param name="chatId">The unique identifier of the chat session. Must be a valid ID format.</param>
    /// <param name="cancellationToken">Token to cancel the streaming operation.</param>
    /// <returns>An async enumerable of string chunks representing the streamed AI response.</returns>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Validates input parameters (message and chatId)
    /// 2. Loads existing chat history for the session
    /// 3. Saves the user message to the database
    /// 4. Streams the AI response in real-time chunks
    /// 5. Saves the complete AI response to the database
    /// 
    /// The method will terminate early (yield break) if:
    /// - The message is null or whitespace
    /// - The chatId is invalid
    /// - Chat history cannot be loaded
    /// - The user message cannot be saved
    /// </remarks>
    public async IAsyncEnumerable<string> StreamAiResponse(
        string message,
        bool contentBased,
        string chatId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.Information("Streaming AI response for {UserIdentifier}", Context.UserIdentifier);

        // Validate the inputs
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.Warning("Received empty message from {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        if (!ValidityUtil.IsValidId(chatId))
        {
            _logger.Error("Invalid chat session ID: {ChatId}", chatId);
            yield break;
        }

        // Load the chat history
        List<ChatMessage>? chatHistory = await LoadChatHistoryAsync(chatId);
        if (chatHistory == null) yield break;

        // Save the user message to the database
        bool savedMessage = await SaveUserMessageAsync(message, chatId);
        if (!savedMessage) yield break;

        var response = new StringBuilder();

        // Generate the AI response
        await foreach (var content in GetAiResponseStream(message, contentBased, chatHistory, cancellationToken))
        {
            response.Append(content);
            yield return content;
        }

        // Save the AI response to the database
        bool savedResponse = await SaveAiResponseAsync(response.ToString(), chatId);
        if (!savedResponse) yield break;

        _logger.Information("Finished streaming AI response to {UserIdentifier}", Context.UserIdentifier);
    }

    /// <summary>
    /// Asynchronously loads the chat history for a specific chat by its ID.
    /// </summary>
    /// <param name="chatId">The unique identifier of the chat to load history for.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains:
    /// - A list of <see cref="ChatMessage"/> objects representing the chat history if successful and authorized.
    /// - <c>null</c> if the chat is not found, the user is not authorized to access it, or an error occurs.
    /// </returns>
    /// <remarks>
    /// This method performs authorization checks to ensure the requesting user owns the chat.
    /// Any exceptions during the operation are logged and the method returns <c>null</c>.
    /// </remarks>
    private async Task<List<ChatMessage>?> LoadChatHistoryAsync(string chatId)
    {
        try
        {
            Chats? chatMessages = await _resourceManager.GetChatAsync(c => c.Id == Guid.Parse(chatId), includeProperties: "Messages");

            if (chatMessages == null || chatMessages.UserId != Guid.Parse(Context.UserIdentifier!))
            {
                _logger.Warning("Chat with ID {ChatId} not found or user not authorized", chatId);
                return null;
            }

            return BuildChatHistory(chatMessages.Messages, chatId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load messages for chat {ChatId}", chatId);
            return null;
        }
    }

    /// <summary>
    /// Builds a list of chat messages from a collection of dynamic message objects.
    /// </summary>
    /// <param name="messages">The collection of dynamic message objects to convert into ChatMessage instances.</param>
    /// <param name="chatId">The unique identifier of the chat, used for logging purposes.</param>
    /// <returns>A list of ChatMessage objects representing the chat history. Unknown message roles are filtered out.</returns>
    /// <remarks>
    /// This method converts dynamic message objects into strongly typed ChatMessage instances based on their MessageRole property.
    /// Supported roles are User and Assistant. Messages with unknown roles are logged as warnings and excluded from the result.
    /// </remarks>
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
            {
                chatHistory.Add(chatMessage);
            }
            else
            {
                _logger.Warning("Unknown message role {MessageRole} in chat {ChatId}", messageItem.MessageRole, chatId);
            }
        }

        return chatHistory;
    }

    /// <summary>
    /// Asynchronously saves a user message to the specified chat.
    /// </summary>
    /// <param name="message">The content of the message to be saved.</param>
    /// <param name="chatId">The unique identifier of the chat where the message will be saved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a boolean value:
    /// <c>true</c> if the message was saved successfully; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method creates a <see cref="MessagesCreateDto"/> object with the user's message details,
    /// including the sender ID from the current context, chat ID, message role as User, and message content.
    /// Any exceptions during the save operation are logged and the method returns <c>false</c>.
    /// </remarks>
    private async Task<bool> SaveUserMessageAsync(string message, string chatId)
    {
        try
        {
            MessagesCreateDto messageDto = new MessagesCreateDto
            {
                SenderId = Guid.Parse(Context.UserIdentifier!),
                ChatId = Guid.Parse(chatId),
                MessageRole = MessageRole.User.ToString(),
                Content = message
            };

            await _resourceManager.CreateMessageAsync(messageDto);
            _logger.Information("User message saved successfully for chat {ChatId}", chatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save user message for chat {ChatId}", chatId);
            return false;
        }
    }

    /// <summary>
    /// Asynchronously streams AI response content based on the specified mode.
    /// </summary>
    /// <param name="message">The user message to process.</param>
    /// <param name="contentBased">Indicates whether to use content-based AI response (true) or standard AI response (false).</param>
    /// <param name="chatHistory">The list of previous chat messages for context.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous enumerable of string content chunks from the AI response stream.</returns>
    private async IAsyncEnumerable<string> GetAiResponseStream(
        string message,
        bool contentBased,
        List<ChatMessage> chatHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (contentBased)
        {
            await foreach (var content in StreamContentBasedAiResponse(message, chatHistory, cancellationToken))
            {
                yield return content;
            }
        }
        else
        {
            await foreach (var content in StreamStandardAiResponse(message, chatHistory, cancellationToken))
            {
                yield return content;
            }
        }
    }

    /// <summary>
    /// Saves an AI assistant response message to the specified chat asynchronously.
    /// </summary>
    /// <param name="response">The AI response content to save. If null or empty, the operation will return false.</param>
    /// <param name="chatId">The unique identifier of the chat where the message will be saved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a boolean value:
    /// true if the AI response was saved successfully; otherwise, false.
    /// </returns>
    private async Task<bool> SaveAiResponseAsync(string response, string chatId)
    {
        if (string.IsNullOrEmpty(response)) return false;

        try
        {
            var aiMessage = new MessagesCreateDto
            {
                SenderId = Guid.Parse(Context.UserIdentifier!),
                ChatId = Guid.Parse(chatId),
                MessageRole = MessageRole.Assistant.ToString(),
                Content = response
            };

            await _resourceManager.CreateMessageAsync(aiMessage);
            _logger.Information("AI response saved successfully for chat {ChatId}", chatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save AI response for chat {ChatId}", chatId);
            return false;
        }
    }

    /// <summary>
    /// Streams a response based on hybrid search results for a given query.
    /// </summary>
    /// <param name="query">The search query to process.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the streaming operation.</param>
    /// <param name="chatHistory">The chat history to include in the AI response.</param>
    /// <returns>An asynchronous enumerable of string chunks representing the streaming response.</returns>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Uses HybridSearchService to get high-quality, ranked results from multiple search sources
    /// 2. Creates a relevance-aware prompt template that incorporates search results with scores
    /// 3. Invokes the RAG system with the enriched context
    /// 4. Yields each content update as it's generated
    ///
    /// The hybrid search combines:
    /// - Semantic (vector) search for meaning-based matching
    /// - Text-based search for exact keyword matches
    /// - Postgres full-text search for comprehensive coverage
    /// - RRF (Reciprocal Rank Fusion) for intelligent result ranking
    /// - Score thresholds to filter out low-relevance results
    /// </remarks>
    private async IAsyncEnumerable<string> StreamContentBasedAiResponse(
        string query,
        List<ChatMessage> chatHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.Information("Content-based AI initiated with query: {Query}", query);

        // Use HybridSearchService for better search quality
        // We request more results (50) but only use the top ones to ensure quality
        HybridSearchResult? searchResult = null;
        string? errorMessage = null;

        try
        {
            searchResult = await _hybridSearchService.SearchAsync(
                searchQuery: query,
                pageIndex: 1,
                pageSize: 50,
                filters: []
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in hybrid search for query: {Query}", query);
            errorMessage = "I encountered an error while searching for information. Please try again.";
        }

        if (errorMessage != null)
        {
            yield return errorMessage;
            yield break;
        }

        if (searchResult == null)
        {
            yield return "An unexpected error occurred. Please try again.";
            yield break;
        }

        if (searchResult.Items.Length == 0)
        {
            _logger.Warning("No search results found for query: {Query}", query);
            yield return "I couldn't find any relevant information in the knowledge base to answer your question. Please try rephrasing your question or ask about a different topic.";
            yield break;
        }

        _logger.Information("Hybrid search returned {Count} results with average score {AvgScore:F3}",
            searchResult.Items.Length, searchResult.Metadata?.AverageScore ?? 0);

        // Prepare content with relevance scores and chunks
        var contentWithScores = searchResult.Items
            .Where(item => item.MatchedChunks != null && item.MatchedChunks.Any(c => !string.IsNullOrWhiteSpace(c)))
            .Select((item, index) => new
            {
                SourceNumber = index + 1,
                Text = string.Join("\n", item.MatchedChunks ?? []),
                Link = $"/library?inspectorId={item.Id}&inspectorType={item.Type}",
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
            _logger.Warning("No content chunks found in search results for query: {Query}", query);
            yield return "I found some results but they don't contain detailed content to answer your question. Please try a more specific question.";
            yield break;
        }

        // Prepare data for the prompt template
        var data = new
        {
            query,
            content = contentWithScores
        };

        var result = Prompts.QuestionAnsweringWithScoresTemplate(data);

        List<ChatMessage> messages =
        [
            new SystemChatMessage(Prompts.SystemContentBasedAiWithScores),
            .. chatHistory,
            new UserChatMessage(result),
        ];

        // Stream the response
        AsyncCollectionResult<StreamingChatCompletionUpdate> responseStreaming;
        try
        {
            responseStreaming = _ragSystem.ChatClient.CompleteChatStreamingAsync(messages, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while getting streaming response for {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        await foreach (StreamingChatCompletionUpdate update in responseStreaming)
        {
            foreach (ChatMessageContentPart updatePart in update.ContentUpdate)
            {
                yield return updatePart.Text;
            }
        }
    }

    /// <summary>
    /// Streams a standard AI response for the provided message.
    /// </summary>
    /// <param name="message">The user's input message to process.</param>
    /// <param name="cancellationToken">A token to cancel the streaming operation.</param>
    /// <param name="chatHistory">The chat history to include in the AI response.</param>
    /// <returns>
    /// An asynchronous stream of string fragments representing the AI's response,
    /// with each fragment emitted as it becomes available.
    /// </returns>
    /// <remarks>
    /// The method performs the following steps:
    /// 1. Truncates chat history if it exceeds the threshold
    /// 2. Adds the user message to chat history
    /// 3. Attempts to get a streaming response from the RAG system
    /// 4. Yields each content update from the streaming response
    /// 
    /// If an exception occurs during the streaming operation, it will be logged
    /// and the method will stop yielding results.
    /// </remarks>
    private async IAsyncEnumerable<string> StreamStandardAiResponse(
        string message,
        List<ChatMessage> chatHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.Information("Standard Ai initiated with message: {Message}", message);

        List<ChatMessage> messages =
        [
            new SystemChatMessage(Prompts.SystemPromptStandardAi),
            .. chatHistory,
            new UserChatMessage(message)
        ];

        // Stream the response
        AsyncCollectionResult<StreamingChatCompletionUpdate> response;
        try
        {
            response = _ragSystem.ChatClient.CompleteChatStreamingAsync(messages, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while getting streaming response for {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        await foreach (StreamingChatCompletionUpdate update in response)
        {
            foreach (ChatMessageContentPart updatePart in update.ContentUpdate)
            {
                yield return updatePart.Text;
            }
        }
    }

    /// <summary>
    /// Handles client connection to the SignalR hub.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override async Task OnConnectedAsync()
    {
        _logger.Information("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Handles client disconnection from the SignalR hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.Information("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
