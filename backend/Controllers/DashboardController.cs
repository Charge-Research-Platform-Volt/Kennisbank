using System.Security.Claims;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Background;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("[controller]")]
[Authorize(Policy = "RequireOwner")]
public class DashboardController(PresenceTracker presenceTracker, IBackgroundTaskQueue taskQueue, ReembedRunState reembedRunState, ExtractionJobService extractionJobService) : AppControllerBase
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
}