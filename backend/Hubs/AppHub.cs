using KnowledgeBank.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalRSwaggerGen.Attributes;

namespace KnowledgeBank.Hubs;

[SignalRHub]
[Authorize]
public class AppHub(ChatOrchestrationService chatOrchestrationService) : Hub<IAppHubClient>
{
    Serilog.ILogger logger = Serilog.Log.ForContext<AppHub>();

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

    public async Task<Guid> CreateChat(string message, string? projectId)
        => await chatOrchestrationService.CreateChatAsync(Context.UserIdentifier!, Context.ConnectionId, message, projectId);

    public IAsyncEnumerable<string> StreamChatResponse(string message, string chatId, string? projectId, List<string>? attachmentIds, CancellationToken cancellationToken)
        => chatOrchestrationService.StreamResponseAsync(Context.UserIdentifier!, Context.ConnectionId, message, chatId, projectId, attachmentIds, cancellationToken);
}