using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Background;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalRSwaggerGen.Attributes;
using System.Runtime.CompilerServices;

namespace KnowledgeBank.Hubs;

[SignalRHub]
[Authorize]
public class AppHub(ChatOrchestrationService chatOrchestrationService, PresenceTracker presenceTracker) : Hub<IAppHubClient>
{
    Serilog.ILogger logger = Serilog.Log.ForContext<AppHub>();

    public override async Task OnConnectedAsync()
    {
        logger.Information("Client connected: {ConnectionId}", Context.ConnectionId);
        presenceTracker.Connected(Context.UserIdentifier!);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.Information("Client disconnected: {ConnectionId}", Context.ConnectionId);
        presenceTracker.Disconnected(Context.UserIdentifier!);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<Guid> CreateChat(string message, string? projectId) 
    {
        presenceTracker.Touch(Context.UserIdentifier!);
        return await chatOrchestrationService.CreateChatAsync(Context.UserIdentifier!, Context.ConnectionId, message, projectId);
    }

    public async IAsyncEnumerable<string> StreamChatResponse(string message, string chatId, string? projectId, List<string>? attachmentIds, [EnumeratorCancellation] CancellationToken cancellationToken) 
    {
        presenceTracker.Touch(Context.UserIdentifier!);
        presenceTracker.ChatStreamStarted();
        
        try
        {
            await foreach (string chunk in chatOrchestrationService.StreamResponseAsync(Context.UserIdentifier!, Context.ConnectionId, message, chatId, projectId, attachmentIds, cancellationToken))
                yield return chunk;
        }
        finally
        {
            presenceTracker.ChatStreamEnded();
        }
    }
}