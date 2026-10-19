using System.Text.Json;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Background;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("admin/chat-eval")]
[Authorize(Policy = "RequireOwner")]
public class ChatEvalController(
    IBackgroundTaskQueue taskQueue,
    EnvironmentConfig environmentConfig,
    ChatEvalService chatEvalService,
    ChatEvalRunState runState,
    DatabaseContext database) : AppControllerBase
{
    [HttpPost("run")]
    [SwaggerOperation(Summary = "Run the given chat eval cases in the background, optionally with a different model than the chat's default")]
    [SwaggerResponse(202, "Queued, returns the run id")]
    [SwaggerResponse(400, "The cases aren't valid")]
    [SwaggerResponse(409, "A chat eval is already running")]
    public async Task<IActionResult> Run([FromBody] ChatEvalRunRequestDto request)
    {
        // Problem() instead of BadRequest(string), so the frontend can show the reason from the detail field
        if (ChatEvalService.ValidateCases(request.Cases) is string error)
            return Problem(error, statusCode: StatusCodes.Status400BadRequest);

        if (!runState.TryStart(out CancellationToken runToken))
            return Problem("A chat eval is already running.", statusCode: StatusCodes.Status409Conflict);

        string? modelOverride = string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim();
        List<ChatEvalCase> cases = request.Cases;
        ChatEvalRun run = new()
        {
            Model = modelOverride ?? environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_MEDIUM_MODEL_NAME),
            Label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim(),
            CasesTotal = cases.Count,
            Cases = JsonSerializer.Serialize(cases)
        };

        try
        {
            // Only one eval runs at a time, so anything still marked Running was cut off by a restart
            await database.ChatEvalRuns
                .Where(r => r.Status == ChatEvalRunStatus.Running)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, ChatEvalRunStatus.Interrupted));

            database.ChatEvalRuns.Add(run);
            await database.SaveChangesAsync();
        }
        catch
        {
            runState.Complete();
            throw;
        }

        Guid runId = run.Id;
        taskQueue.QueueBackgroundWorkItem(async hostToken =>
        {
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(hostToken, runToken);

            try { await chatEvalService.ExecuteRunAsync(runId, cases, modelOverride, linkedCts.Token); }
            finally { runState.Complete(); }
        });

        return Accepted(new { id = runId });
    }

    [HttpGet("cases/latest")]
    [SwaggerOperation(Summary = "The cases of the most recent run, or an empty list when no run has stored any yet, to start a new run from")]
    [SwaggerResponse(200, "Cases returned")]
    public async Task<IActionResult> LatestCases()
    {
        // Runs from before cases were stored have "[]"
        string? latest = await database.ChatEvalRuns
            .Where(r => r.Cases != "[]")
            .OrderByDescending(r => r.StartedAt)
            .Select(r => r.Cases)
            .FirstOrDefaultAsync();

        return Ok(latest == null ? [] : ChatEvalService.ParseCases(latest));
    }

    [HttpPost("cancel")]
    [SwaggerOperation(Summary = "Cancel the running chat eval, if any. Cases that already finished are kept.")]
    [SwaggerResponse(204, "Cancellation requested")]
    [SwaggerResponse(404, "No chat eval is currently running")]
    public IActionResult Cancel()
    {
        if (!runState.IsRunning) return NotFound();
        runState.Cancel();
        return NoContent();
    }

    [HttpGet("runs")]
    [SwaggerOperation(Summary = "List all chat eval runs, newest first, with their averages")]
    [SwaggerResponse(200, "Runs returned")]
    public async Task<IActionResult> Runs()
    {
        List<ChatEvalRun> runs = await database.ChatEvalRuns.OrderByDescending(r => r.StartedAt).ToListAsync();
        return Ok(runs.Select(r => ChatEvalService.Summarize(r, runState.IsRunning)));
    }

    [HttpGet("runs/{id:guid}")]
    [SwaggerOperation(Summary = "Get one chat eval run with every question, answer and tool result")]
    [SwaggerResponse(200, "Run returned")]
    [SwaggerResponse(404, "Run not found")]
    public async Task<IActionResult> GetRun(Guid id)
    {
        ChatEvalRun? run = await database.ChatEvalRuns.FindAsync(id);
        if (run == null) return NotFound();

        return Ok(new ChatEvalRunDetailDto
        {
            Summary = ChatEvalService.Summarize(run, runState.IsRunning),
            Cases = ChatEvalService.ReadResults(run)
        });
    }

    [HttpDelete("runs/{id:guid}")]
    [SwaggerOperation(Summary = "Delete a chat eval run")]
    [SwaggerResponse(204, "Deleted")]
    [SwaggerResponse(404, "Run not found")]
    [SwaggerResponse(409, "The run is still in progress")]
    public async Task<IActionResult> DeleteRun(Guid id)
    {
        ChatEvalRun? run = await database.ChatEvalRuns.FindAsync(id);
        if (run == null) return NotFound();
        if (run.Status == ChatEvalRunStatus.Running && runState.IsRunning) return Conflict("Cancel the run before deleting it.");

        database.ChatEvalRuns.Remove(run);
        await database.SaveChangesAsync();
        return NoContent();
    }
}
