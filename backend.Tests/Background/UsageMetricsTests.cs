using KnowledgeBank.Models;
using KnowledgeBank.Services.Background;

namespace KnowledgeBank.Tests.Background;

public class UsageMetricsTests
{
    private readonly UsageMetrics metrics = new();

    [Fact]
    public void TakePending_ReturnsDailyTotals_AndResetsThem()
    {
        metrics.Record("search.content", duration: TimeSpan.FromMilliseconds(120));
        metrics.Record("search.content", success: false, duration: TimeSpan.FromMilliseconds(80));
        metrics.Record("ocr.pages", count: 12);

        List<UsageCounter> taken = metrics.TakePending();

        UsageCounter search = taken.Single(c => c.Metric == "search.content");
        Assert.Equal(2, search.Count);
        Assert.Equal(1, search.FailureCount);
        Assert.Equal(200, search.TotalDurationMs);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), search.Date);
        Assert.Equal(12, taken.Single(c => c.Metric == "ocr.pages").Count);

        Assert.Empty(metrics.TakePending());
    }

    [Fact]
    public void Restore_PutsTotalsBackForTheNextFlush()
    {
        metrics.Record("files.upload");
        List<UsageCounter> taken = metrics.TakePending();

        foreach (UsageCounter counter in taken)
            metrics.Restore(counter);
        metrics.Record("files.upload");

        Assert.Equal(2, metrics.TakePending().Single().Count);
    }

    [Fact]
    public async Task ConcurrentRecords_AreAllCounted()
    {
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
                metrics.Record("http.request");
        })));

        Assert.Equal(8000, metrics.TakePending().Single().Count);
    }
}
