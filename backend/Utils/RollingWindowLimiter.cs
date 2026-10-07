namespace KnowledgeBank.Utils;

/// <summary>
/// Tracks how much of some quantity (requests, tokens, pages) has occurred in a trailing time window,
/// and can wait until there's headroom under a budget before proceeding.
///
/// Used two ways: preventively for request counts (a call is always exactly "1 request", known before
/// it happens, so WaitForCapacityAsync + RecordUsage(1) can bracket the call itself); and reactively for
/// usage-based limits like tokens or pages, where the real amount is only known once a response comes
/// back. WaitForCapacityAsync checks against what's already happened, and RecordUsage is called
/// afterward with the real figure. That means a request that's already in flight when capacity is
/// checked can still push the total slightly over budget once its usage lands; callers should configure
/// the budget with some safety margin below the provider's real limit to absorb that.
/// </summary>
public class RollingWindowLimiter(long budget, TimeSpan window, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider time = timeProvider ?? TimeProvider.System;
    private readonly Lock gate = new();
    private readonly Queue<(DateTime Timestamp, long Amount)> events = new();
    private long currentTotal;

    public async Task WaitForCapacityAsync(CancellationToken ct = default)
    {
        while (true)
        {
            TimeSpan wait;

            lock (gate)
            {
                Prune();

                if (currentTotal < budget)
                    return;

                // Add random jitter to prevent all waiting tasks from waking up at the same time
                wait = events.Peek().Timestamp + window - time.GetUtcNow().UtcDateTime + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 50));
            }

            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, time, ct);
        }
    }

    public void RecordUsage(long amount)
    {
        if (amount <= 0) return;

        lock (gate)
        {
            events.Enqueue((time.GetUtcNow().UtcDateTime, amount));
            currentTotal += amount;
        }
    }

    private void Prune()
    {
        DateTime cutoff = time.GetUtcNow().UtcDateTime - window;
        while (events.Count > 0 && events.Peek().Timestamp < cutoff)
            currentTotal -= events.Dequeue().Amount;
    }
}
