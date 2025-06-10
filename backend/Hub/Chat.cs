using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using HandlebarsDotNet;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenAI.Chat;
using SignalRSwaggerGen.Attributes;

namespace Hubs;


// Todo: implement the SignalRHub and see how it results in the documentation website

[SignalRHub]
[Authorize]
public class Chat : Hub
{
    private readonly Serilog.ILogger _logger;
    private readonly RAGSystem _ragSystem;
    private readonly RAGManger _ragManager;
    private readonly ResourceManager _resourceManager;

    public Chat(RAGSystem ragSystem, RAGManger ragManager, ResourceManager resourceManager)
    {
        _logger = Serilog.Log.ForContext<Chat>();
        _ragSystem = ragSystem;
        _ragManager = ragManager;
        _resourceManager = resourceManager;
    }


    public async Task<Guid> CreateChat(string message)
    {
        _logger.Information("Creating new chat");
        string title = await _ragManager.GenerateChatTitleAsync(message);
        ChatsCreateDto chat = new ChatsCreateDto
        {
            UserId = Guid.Parse(Context.UserIdentifier!),
            Title = title,
        };

        Guid chatSessionId = await _resourceManager.CreateChatAsync(chat);
        _logger.Information("New chat created successfully");

        return chatSessionId;
    }

    /// <summary>
    /// Streams an AI-generated response to the client based on the given message.
    /// </summary>
    /// <param name="message">The user message to which the AI will respond.</param>
    /// <param name="contentBased">
    /// Determines the response type:
    /// - If true, streams a content-based response using knowledge base data.
    /// - If false, streams a standard AI response without additional knowledge.
    /// </param>
    /// <param name="chatId">The ID of the chat session to which the message belongs.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous enumerable of string chunks representing the streamed AI response.</returns>
    /// <remarks>
    /// The method logs the start and completion of the streaming process.
    /// If the input message is empty or whitespace, the method yields no results and logs a warning.
    /// </remarks>
    public async IAsyncEnumerable<string> StreamAiResponse(
            string message,
            bool contentBased,
            string chatId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.Information("Streaming AI response for {UserIdentifier}", Context.UserIdentifier);

        // Validate the input message
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.Warning("Received empty message from {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        // Validate the chat session ID
        if (!ValidityUtil.IsValidId(chatId))
        {
            _logger.Error("Invalid chat session ID: {ChatId}", chatId);
            yield break;
        }

        // Load chat messages from the database
        List<ChatMessage> chatHistory = new List<ChatMessage>();
        try
        {
            var chatMessages = await _resourceManager.GetChatAsync(c => c.Id == Guid.Parse(chatId), includeProperties: "Messages");
            if (chatMessages == null || chatMessages.UserId != Guid.Parse(Context.UserIdentifier!))
            {
                _logger.Warning("Chat with ID {ChatId} not found or user not authorized", chatId);
                yield break;
            }

            foreach (var messageItem in chatMessages.Messages)
            {
                if (messageItem.MessageRole == MessageRole.User.ToString())
                {
                    chatHistory.Add(new UserChatMessage(messageItem.Content));
                }
                else if (messageItem.MessageRole == MessageRole.Assistant.ToString())
                {
                    chatHistory.Add(new AssistantChatMessage(messageItem.Content));
                }
                else
                {
                    _logger.Warning("Unknown message role {MessageRole} in chat {ChatId}", messageItem.MessageRole, chatId);
                }
            }

        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load messages for chat {ChatId}", chatId);
            yield break;
        }



        // Save the user message to the database
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
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save user message for chat {ChatId}", chatId);
            yield break;
        }


        StringBuilder response = new StringBuilder();
        if (contentBased)
        {
            // Use content-based AI response
            await foreach (var content in StreamContentBasedAiResponse(message, chatHistory, cancellationToken))
            {
                response.Append(content);
                yield return content;
            }
        }
        else
        {
            // Use standard AI response
            await foreach (var content in StreamStandardAiResponse(message, chatHistory, cancellationToken))
            {
                response.Append(content);
                yield return content;
            }
        }

        // Save the AI response to the database
        if (response.Length > 0)
        {
            try
            {
                MessagesCreateDto aiMessage = new MessagesCreateDto
                {
                    SenderId = Guid.Parse(Context.UserIdentifier!),
                    ChatId = Guid.Parse(chatId),
                    MessageRole = MessageRole.Assistant.ToString(),
                    Content = response.ToString()
                };
                await _resourceManager.CreateMessageAsync(aiMessage);
                _logger.Information("AI response saved successfully for chat {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save AI response for chat {ChatId}", chatId);
            }
        }

        _logger.Information("Finished streaming AI response to {UserIdentifier}", Context.UserIdentifier);
    }



    /// <summary>
    /// Streams a response based on content search results for a given query.
    /// </summary>
    /// <param name="query">The search query to process.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the streaming operation.</param>
    /// <param name="chatHistory">The chat history to include in the AI response.</param>
    /// <returns>An asynchronous enumerable of string chunks representing the streaming response.</returns>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Creates a Handlebars prompt template that incorporates vector search results
    /// 2. Invokes the RAG system kernel with the template and query
    /// 3. Yields each content update as it's generated
    /// </remarks>
    private async IAsyncEnumerable<string> StreamContentBasedAiResponse(
        string query,
        List<ChatMessage> chatHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.Information("Content-based Ai initiated with query: {Query}", query);

        float[] embeddingData = await _ragSystem.GenerateEmbedding(query);

        var search = await _ragSystem.QdrantClient.QueryAsync(
            RAGSystem.COLLECTION_NAME,
            query: embeddingData,
            limit: 10
        );

        var data = new
        {
            query,
            content = search.Select(item =>
            {
                var payload = CustomPayload.FromPayload(item.Payload);
                return new
                {
                    Text = payload.ChunkText,
                    link = "/archive?id=" + payload.ResourceId,
                };
            }).ToList()
        };

        var result = Prompts.QuestionAnsweringTemplate(data);

        List<ChatMessage> messages =
        [
            new SystemChatMessage(Prompts.SystemContentBasedAi),
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
    /// <remarks>
    /// This method logs the client's connection using the connection ID
    /// and calls the base implementation to complete the connection process.
    /// </remarks>
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
    /// <remarks>
    /// This method logs the client's disconnection using the connection ID
    /// and calls the base implementation to complete the disconnection process.
    /// </remarks>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.Information("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
