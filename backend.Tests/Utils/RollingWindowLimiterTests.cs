using KnowledgeBank.Utils;
using Microsoft.Extensions.Time.Testing;

namespace KnowledgeBank.Tests.Utils;

public class RollingWindowLimiterTests
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MaxJitter = TimeSpan.FromMilliseconds(50);

    private readonly FakeTimeProvider time = new();
    private readonly RollingWindowLimiter limiter;

    public RollingWindowLimiterTests() => limiter = new RollingWindowLimiter(budget: 10, Window, time);

    [Fact]
    public void UnderBudget_ProceedsImmediately()
    {
        limiter.RecordUsage(9);

        Assert.True(limiter.WaitForCapacityAsync().IsCompletedSuccessfully);
    }

    [Fact]
    public async Task AtBudget_WaitsUntilOldestUsageLeavesTheWindow()
    {
        limiter.RecordUsage(10);

        Task wait = limiter.WaitForCapacityAsync();
        Assert.False(wait.IsCompleted);

        time.Advance(Window / 2);
        Assert.False(wait.IsCompleted);

        time.Advance(Window / 2 + MaxJitter + TimeSpan.FromMilliseconds(1));
        await wait.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UsageOlderThanTheWindow_NoLongerCounts()
    {
        limiter.RecordUsage(10);
        time.Advance(Window + TimeSpan.FromSeconds(1));

        Assert.True(limiter.WaitForCapacityAsync().IsCompletedSuccessfully);
    }

    [Fact]
    public void ZeroOrNegativeUsage_IsIgnored()
    {
        limiter.RecordUsage(0);
        limiter.RecordUsage(-50);
        limiter.RecordUsage(9);

        Assert.True(limiter.WaitForCapacityAsync().IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Waiting_CanBeCancelled()
    {
        limiter.RecordUsage(10);
        using CancellationTokenSource cts = new();

        Task wait = limiter.WaitForCapacityAsync(cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }
}
