using KnowledgeBank.Utils;

namespace KnowledgeBank.Tests.Utils;

public class ConcurrencyUtilsTests
{
    [Fact]
    public async Task RunsEveryItem_WithoutExceedingMaxConcurrency()
    {
        int running = 0, maxRunning = 0, completed = 0;

        await ConcurrencyUtils.RunBoundedAsync(Enumerable.Range(0, 20), maxConcurrency: 3, async _ =>
        {
            int now = Interlocked.Increment(ref running);
            InterlockedMax(ref maxRunning, now);
            await Task.Delay(10);
            Interlocked.Decrement(ref running);
            Interlocked.Increment(ref completed);
        });

        Assert.Equal(20, completed);
        Assert.InRange(maxRunning, 1, 3);
    }

    [Fact]
    public async Task AfterCancellation_NoNewItemsStart()
    {
        using CancellationTokenSource cts = new();
        int started = 0;

        await ConcurrencyUtils.RunBoundedAsync(Enumerable.Range(0, 10), maxConcurrency: 1, _ =>
        {
            Interlocked.Increment(ref started);
            cts.Cancel();
            return Task.CompletedTask;
        }, cts.Token);

        Assert.Equal(1, started);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while ((current = Volatile.Read(ref target)) < value && Interlocked.CompareExchange(ref target, value, current) != current) { }
    }
}
