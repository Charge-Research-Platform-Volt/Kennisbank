using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;

namespace KnowledgeBank.Services.Background;

/// <summary>
/// Writes the in-memory usage totals to the database every minute, and once more on shutdown so a deploy
/// doesn't drop the last minute. Registered as a singleton so tests can flush on demand.
/// </summary>
public class UsageFlushService(UsageMetrics usageMetrics, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMinutes(1);

    private readonly Serilog.ILogger logger = Log.ForContext<UsageFlushService>();
    private readonly SemaphoreSlim flushLock = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(FlushInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await FlushAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FlushAsync(cancellationToken);
    }

    public async Task FlushAsync(CancellationToken ct = default)
    {
        await flushLock.WaitAsync(ct);
        List<UsageCounter> pending = [];

        try
        {
            pending = usageMetrics.TakePending();
            if (pending.Count == 0) return;

            using IServiceScope scope = scopeFactory.CreateScope();
            DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            // One transaction, so a failure halfway can be retried in full without counting the first rows twice
            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct);

            foreach (UsageCounter c in pending)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO usage_counters ("date", "metric", "count", "failure_count", "total_duration_ms")
                    VALUES ({c.Date}, {c.Metric}, {c.Count}, {c.FailureCount}, {c.TotalDurationMs})
                    ON CONFLICT ("date", "metric") DO UPDATE SET
                        "count" = usage_counters."count" + EXCLUDED."count",
                        "failure_count" = usage_counters."failure_count" + EXCLUDED."failure_count",
                        "total_duration_ms" = usage_counters."total_duration_ms" + EXCLUDED."total_duration_ms"
                    """, ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            foreach (UsageCounter c in pending)
                usageMetrics.Restore(c);

            logger.Warning(ex, "Failed to write usage totals, will retry on the next flush");
        }
        finally
        {
            flushLock.Release();
        }
    }
}
