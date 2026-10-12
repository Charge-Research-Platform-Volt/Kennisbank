using System.Security.Claims;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Background;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("[controller]")]
[Authorize(Policy = "RequireOwner")]
public class DashboardController(PresenceTracker presenceTracker, IBackgroundTaskQueue taskQueue, ReembedRunState reembedRunState, ExtractionJobService extractionJobService, MistralModelWatchService modelWatch) : AppControllerBase
{
    private static readonly TimeSpan ActiveWindow = TimeSpan.FromMinutes(5);

    [HttpGet("live")]
    [SwaggerOperation(
        Summary = "Live counts of online users and running work",
        Description = "Used to judge wheter it's safe to deploy. Only counts are returned, never user identities. The requesting admin is excluded."
    )]
    [SwaggerResponse(200, "Counts returned")]
    public IActionResult Live()
    {
        var (connected, active, chatStreams) = presenceTracker.Snapshot(ActiveWindow, User.FindFirstValue(ClaimTypes.NameIdentifier));
        int pending = taskQueue.PendingCount;
        int running = taskQueue.RunningCount;
        bool reembedRunning = reembedRunState.IsRunning;
        int extractionJobs = extractionJobService.ActiveJobCount;

        return Ok(new
        {
            connectedUsers = connected,
            activeUsers = active,
            activeWindowMinutes = (int)ActiveWindow.TotalMinutes,
            activeChatStreams = chatStreams,
            queue = new { pending, running },
            reembedRunning,
            extractionJobs,
            safeToDeploy = chatStreams == 0 && pending == 0 && running == 0 && !reembedRunning && extractionJobs == 0
        });
    }

    [HttpGet("models")]
    [SwaggerOperation(Summary = "Result of the last check for newer Mistral model versions")]
    [SwaggerResponse(200, "Model status returned")]
    public IActionResult Models()
        => Ok(new { checkedAt = modelWatch.LastCheckedUtc, error = modelWatch.LastError, families = modelWatch.Current });

    [HttpPost("models/refresh")]
    [SwaggerOperation(Summary = "Check Mistral's model list now instead of waiting for the daily check")]
    [SwaggerResponse(200, "Model status returned")]
    public async Task<IActionResult> RefreshModels()
    {
        await modelWatch.RefreshAsync(HttpContext.RequestAborted);
        return Models();
    }

    [HttpPost("models/acknowledge")]
    [SwaggerOperation(Summary = "Stop flagging the currently new models as new")]
    [SwaggerResponse(200, "Model status returned")]
    public async Task<IActionResult> AcknowledgeModels()
    {
        await modelWatch.AcknowledgeAllAsync(HttpContext.RequestAborted);
        return Models();
    }
}