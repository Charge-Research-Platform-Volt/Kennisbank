using System.Security.Claims;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Background;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("[controller]")]
[Authorize(Policy = "RequireOwner")]
public class DashboardController(PresenceTracker presenceTracker, IBackgroundTaskQueue taskQueue, ReembedRunState reembedRunState, ExtractionJobService extractionJobService, MistralModelWatchService modelWatch, DatabaseContext database) : AppControllerBase
{
    public record UsageSeries(string Key, long[] Counts, long[] Failures, double?[] AvgDurationMs);

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

    [HttpGet("usage")]
    [SwaggerOperation(
        Summary = "Daily usage totals for the last N days (UTC)",
        Description = "Counters recorded by the app plus activity derived from stored chats, messages and resources. Only daily totals, nothing per user. Recorded counters reach the database within a minute."
    )]
    [SwaggerResponse(200, "Usage returned")]
    public async Task<IActionResult> Usage([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 1, 365);
        DateOnly from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-(days - 1));
        DateTime fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        List<DateOnly> dates = Enumerable.Range(0, days).Select(from.AddDays).ToList();

        List<UsageCounter> counters = await database.UsageCounters.Where(c => c.Date >= from).ToListAsync();

        List<UsageSeries> series = counters
            .GroupBy(c => c.Metric)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                Dictionary<DateOnly, UsageCounter> byDate = g.ToDictionary(c => c.Date);
                UsageCounter? On(DateOnly date) => byDate.GetValueOrDefault(date);

                return new UsageSeries(
                    g.Key,
                    dates.Select(d => On(d)?.Count ?? 0).ToArray(),
                    dates.Select(d => On(d)?.FailureCount ?? 0).ToArray(),
                    dates.Select(d => On(d) is { Count: > 0, TotalDurationMs: > 0 } c ? c.TotalDurationMs / (double)c.Count : (double?)null).ToArray()
                );
            })
            .ToList();

        // Activity from data the app already stores, reduced to daily totals here
        List<DateTime> chats = await database.Chats.Where(c => c.CreatedOn >= fromUtc).Select(c => c.CreatedOn).ToListAsync();
        List<DateTime> userMessages = await database.Messages
            .Where(m => m.CreatedOn >= fromUtc && m.MessageRole == nameof(MessageRole.User))
            .Select(m => m.CreatedOn)
            .ToListAsync();
        List<DateTime> resources = await database.Resources.Where(r => r.CreatedOn >= fromUtc).Select(r => r.CreatedOn).ToListAsync();

        UsageSeries Daily(string key, List<DateTime> timestamps)
        {
            Dictionary<DateOnly, int> perDay = timestamps.GroupBy(DateOnly.FromDateTime).ToDictionary(g => g.Key, g => g.Count());
            return new UsageSeries(key, dates.Select(d => (long)perDay.GetValueOrDefault(d)).ToArray(), new long[days], new double?[days]);
        }

        series.Add(Daily("activity.chats", chats));
        series.Add(Daily("activity.user_messages", userMessages));
        series.Add(Daily("activity.resources", resources));

        return Ok(new { dates, series });
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