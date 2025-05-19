using System.Runtime.CompilerServices;
using KnowledgeBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using SignalRSwaggerGen.Attributes;
using Swashbuckle.AspNetCore.Annotations;

namespace Hubs;


// Todo: implement the SignalRHub and see how it results in the documentation website

[SignalRHub]
[Authorize]
public class Chat : Hub
{
    private readonly Serilog.ILogger _logger;
    private readonly IRAGSystem _ragSystem;



    public Chat(IRAGSystem ragSystem)
    {
        _logger = Serilog.Log.ForContext<Chat>();
        _ragSystem = ragSystem;
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
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous enumerable of string chunks representing the streamed AI response.</returns>
    /// <remarks>
    /// The method logs the start and completion of the streaming process.
    /// If the input message is empty or whitespace, the method yields no results and logs a warning.
    /// </remarks>
    public async IAsyncEnumerable<string> StreamAiResponse(
            string message,
            bool contentBased,
            [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.Information("Streaming AI response for {UserIdentifier}", Context.UserIdentifier);

        // Validate the input message
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.Warning("Received empty message from {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        if (contentBased)
        {
            // Use content-based AI response
            await foreach (var content in StreamContentBasedAiResponse(message, cancellationToken))
            {
                yield return content;
            }
        }
        else
        {
            // Use standard AI response
            await foreach (var content in StreamStandardAiResponse(message, cancellationToken))
            {
                yield return content;
            }
        }

        _logger.Information("Finished streaming AI response to {UserIdentifier}", Context.UserIdentifier);
    }



    /// <summary>
    /// Streams a response based on content search results for a given query.
    /// </summary>
    /// <param name="query">The search query to process.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the streaming operation.</param>
    /// <returns>An asynchronous enumerable of string chunks representing the streaming response.</returns>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Creates a Handlebars prompt template that incorporates vector search results
    /// 2. Invokes the RAG system kernel with the template and query
    /// 3. Yields each content update as it's generated
    /// </remarks>
    private async IAsyncEnumerable<string> StreamContentBasedAiResponse(
        string query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.Information("Content-based Ai initiated with query: {Query}", query);

        // Create a Handlebars prompt template
        string promptTemplate = """
{{#with (SearchPlugin-GetTextSearchResults query)}}  
    {{#each this}}  
    Name: {{Name}}
    Value: {{Value}}
    Link: {{Link}}
    -----------------
    {{/each}}  
{{/with}}  

{{query}}

Include citations to the relevant information where it is referenced in the response.
""";

        KernelArguments arguments = new() { { "query", query } };
        HandlebarsPromptTemplateFactory promptTemplateFactory = new();

        // Stream the response
        IAsyncEnumerable<StreamingKernelContent> streamingResponse;

        try
        {
            streamingResponse = _ragSystem.Kernel.InvokePromptStreamingAsync(
                promptTemplate,
                arguments,
                templateFormat: HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat,
                promptTemplateFactory: promptTemplateFactory,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while getting streaming response for {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        await foreach (var update in streamingResponse)
        {
            if (update.ToString() is string content)
            {
                yield return content;
            }
        }
    }



    /// <summary>
    /// Streams a standard AI response for the provided message.
    /// </summary>
    /// <param name="message">The user's input message to process.</param>
    /// <param name="cancellationToken">A token to cancel the streaming operation.</param>
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
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.Information("Standard Ai initiated with message: {Message}", message);


        // Truncates chat history if it exceeds the threshold
        ChatHistoryTruncationReducer reducer = new ChatHistoryTruncationReducer(targetCount: 10, thresholdCount: 5);

        var chatHistory = new ChatHistory("You are a helpful AI assistant. Answer in Markdown format.");
        chatHistory.AddUserMessage(message);

        IEnumerable<ChatMessageContent>? reducedMessages = await reducer.ReduceAsync(chatHistory);

        if (reducedMessages is not null)
        {
            chatHistory = [.. reducedMessages];
        }

        // Stream the response
        IAsyncEnumerable<StreamingChatMessageContent> streamingResponse;
        try
        {
            streamingResponse = _ragSystem.ChatCompletionService.GetStreamingChatMessageContentsAsync(
                chatHistory,
                kernel: _ragSystem.Kernel,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while getting streaming response for {UserIdentifier}", Context.UserIdentifier);
            yield break;
        }

        await foreach (var update in streamingResponse)
        {
            if (update.Content != null)
            {
                yield return update.Content;
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
