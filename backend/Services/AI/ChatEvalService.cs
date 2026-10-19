using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using KnowledgeBank.Data;
using KnowledgeBank.Models;

namespace KnowledgeBank.Services.AI;

/// <summary>
/// Runs chat eval cases through the real chat loop and records how each answer went: rounds, timings, tokens,
/// and whether every citation points at something the tools actually returned.
/// </summary>
public class ChatEvalService(IServiceScopeFactory scopeFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Regex SourceMarkerPattern = new(@"\[SRC:[^\]]+\]", RegexOptions.Compiled);
    private static readonly Regex UuidPattern = new(@"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Serilog.ILogger logger = Serilog.Log.ForContext<ChatEvalService>();

    internal static List<ChatEvalCase> ParseCases(string json)
    {
        List<ChatEvalCase> cases = JsonSerializer.Deserialize<List<ChatEvalCase>>(json, JsonOptions) ?? [];

        if (ValidateCases(cases) is string error)
            throw new InvalidDataException(error);

        return cases;
    }

    /// <summary>
    /// Returns why the cases can't be run, or null when they're fine.
    /// </summary>
    public static string? ValidateCases(List<ChatEvalCase> cases)
    {
        if (cases.Count == 0)
            return "Add at least one case.";
        if (cases.Any(c => string.IsNullOrWhiteSpace(c.Id)))
            return "Every case needs an id.";
        // "turns": null in pasted JSON gets past required, which only checks that the key is there
        if (cases.Any(c => c.Turns is null || c.Turns.Count == 0 || c.Turns.Any(string.IsNullOrWhiteSpace)))
            return "Every case needs at least one non-empty turn.";
        if (cases.Select(c => c.Id).Distinct().Count() != cases.Count)
            return "Case ids must be unique.";

        return null;
    }

    /// <summary>
    /// Runs the cases one after another for an existing run row, saving the results after every case so the dashboard
    /// can show progress and a cancel or crash keeps what was finished. Sequential, so turns don't compete for the
    /// rate limit and skew the timings
    /// </summary>
    public async Task ExecuteRunAsync(Guid runId, List<ChatEvalCase> cases, string? modelOverride, CancellationToken ct)
    {
        List<ChatEvalCaseResult> results = [];
        ChatEvalRunStatus status = ChatEvalRunStatus.Failed;

        try
        {
            foreach (ChatEvalCase evalCase in cases)
            {
                ct.ThrowIfCancellationRequested();
                results.Add(await RunCaseAsync(evalCase, modelOverride, ct));
                await SaveRunAsync(runId, results, null);
            }

            status = ChatEvalRunStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            status = ChatEvalRunStatus.Cancelled;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Chat eval run {RunId} failed", runId);
        }

        await SaveRunAsync(runId, results, status);
    }

    private async Task SaveRunAsync(Guid runId, List<ChatEvalCaseResult> results, ChatEvalRunStatus? finalStatus)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        // Deleted from the dashboard while it was still running
        ChatEvalRun? run = await db.ChatEvalRuns.FindAsync(runId);
        if (run == null) return;

        run.CasesDone = results.Count;
        run.Results = JsonSerializer.Serialize(results);

        if (finalStatus is ChatEvalRunStatus status)
        {
            run.Status = status;
            run.CompletedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public static List<ChatEvalCaseResult> ReadResults(ChatEvalRun run)
        => JsonSerializer.Deserialize<List<ChatEvalCaseResult>>(run.Results) ?? [];

    /// <summary>
    /// Averages over turns that got an answer; failed turns are only counted. <paramref name="evalRunning"/> tells
    /// a run that's actually in progress apart from one that was cut off by a restart while marked Running.
    /// </summary>
    public static ChatEvalRunSummaryDto Summarize(ChatEvalRun run, bool evalRunning)
    {
        List<ChatEvalTurnResult> turns = [.. ReadResults(run).SelectMany(c => c.Turns)];
        List<ChatEvalTurnResult> answered = [.. turns.Where(t => t.Error == null)];

        static double? Avg(IEnumerable<double> values)
        {
            List<double> list = [.. values];
            return list.Count > 0 ? list.Average() : null;
        }

        return new ChatEvalRunSummaryDto
        {
            Id = run.Id,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            Model = run.Model,
            Label = run.Label,
            Status = (run.Status == ChatEvalRunStatus.Running && !evalRunning ? ChatEvalRunStatus.Interrupted : run.Status).ToString(),
            CasesTotal = run.CasesTotal,
            CasesDone = run.CasesDone,
            Turns = turns.Count,
            FailedTurns = turns.Count - answered.Count,
            AvgTotalMs = Avg(answered.Select(t => t.TotalMs)),
            AvgFirstTokenMs = Avg(answered.Where(t => t.FirstTokenMs != null).Select(t => t.FirstTokenMs!.Value)),
            AvgRounds = Avg(answered.Select(t => (double)t.Rounds)),
            AvgToolCalls = Avg(answered.Select(t => (double)t.ToolCalls)),
            PromptTokens = answered.Sum(t => (long)t.PromptTokens),
            CompletionTokens = answered.Sum(t => (long)t.CompletionTokens),
            Citations = answered.Sum(t => t.Citations),
            UnsupportedCitations = answered.Sum(t => t.UnsupportedCitations.Count)
        };
    }

    private async Task<ChatEvalCaseResult> RunCaseAsync(ChatEvalCase evalCase, string? modelOverride, CancellationToken ct)
    {
        ChatEvalCaseResult result = new() { Id = evalCase.Id };
        List<object> history = [];

        foreach (string question in evalCase.Turns)
        {
            ChatEvalTurnResult turn = new() { Question = question };
            result.Turns.Add(turn);
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var orchestration = scope.ServiceProvider.GetRequiredService<ChatOrchestrationService>();
                (string answer, ChatTurnStats stats) = await orchestration.RunEvalTurnAsync(question, history, modelOverride, ct);

                turn.Answer = answer;
                turn.Rounds = stats.Rounds;
                turn.ToolCalls = stats.ToolCalls;
                turn.FirstTokenMs = stats.FirstToken?.TotalMilliseconds;
                turn.FinalRoundMs = stats.FinalRound.TotalMilliseconds;
                turn.PromptTokens = stats.PromptTokens;
                turn.CompletionTokens = stats.CompletionTokens;
                turn.ToolResults = stats.ToolResults;
                (turn.Citations, turn.UnsupportedCitations) = CheckCitations(answer, stats.ToolResults);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.Error(ex, "Chat eval case {CaseId} failed", evalCase.Id);
                turn.Error = ex.Message;
            }

            turn.TotalMs = stopwatch.Elapsed.TotalMilliseconds;

            // A follow-up question means nothing without the earlier answer
            if (turn.Error != null) break;

            history.Add(new { role = "user", content = question });
            history.Add(new { role = "assistant", content = turn.Answer });
        }

        return result;
    }

    /// <summary>
    /// Counts the distinct [SRC:uuid] citations in an answer and returns the ones whose id appears nowhere in the turn's tool results.
    /// </summary>
    internal static (int Cited, List<string> Unsupported) CheckCitations(string answer, IEnumerable<string> toolResults)
    {
        HashSet<string> cited = SourceMarkerPattern.Matches(answer)
            .SelectMany(m => UuidPattern.Matches(m.Value))
            .Select(m => m.Value.ToLowerInvariant())
            .ToHashSet();

        HashSet<string> returned = toolResults
            .SelectMany(r => UuidPattern.Matches(r))
            .Select(m => m.Value.ToLowerInvariant())
            .ToHashSet();

        return (cited.Count, [.. cited.Where(id => !returned.Contains(id)).Order()]);
    }
}