namespace KnowledgeBank.Utils;

/// <summary>
/// Combines a request-count budget and a token-count budget into a single rate limiter, so a caller only
/// ever waits on ONE thing that atomically accounts for both dimensions — avoiding the race where
/// checking two independent limiters separately (wait on the request limiter, then wait on the token
/// limiter, then record against the request limiter) lets more callers through the request check than
/// its budget allows while they're all still waiting on the token check.
///
/// Request-count is reserved immediately by WaitForSlotAsync itself, since "1 request" is always known
/// in advance. Token-count is reactive: WaitForSlotAsync only checks it, since the real amount is only
/// known once a response comes back — call RecordTokens afterward with the actual figure. Configure both
/// budgets with some safety margin below the provider's real limits to absorb that reactive gap.
/// </summary>
public class RequestTokenLimiter(long requestBudget, long tokenBudget, TimeSpan window)
{
    private readonly Lock gate = new();
    private readonly Queue<(DateTime Timestamp, long Amount)> requestEvents = new();
    private readonly Queue<(DateTime Timestamp, long Amount)> tokenEvents = new();
    private long currentRequests;
    private long currentTokens;

    public async Task WaitForSlotAsync(CancellationToken ct = default)
    {
        while (true)
        {
            TimeSpan wait;

            lock (gate)
            {
                Prune();

                if (currentRequests < requestBudget && currentTokens < tokenBudget)
                {
                    requestEvents.Enqueue((DateTime.UtcNow, 1));
                    currentRequests += 1;
                    return;
                }

                TimeSpan requestWait = currentRequests >= requestBudget
                    ? requestEvents.Peek().Timestamp + window - DateTime.UtcNow
                    : TimeSpan.Zero;
                TimeSpan tokenWait = currentTokens >= tokenBudget
                    ? tokenEvents.Peek().Timestamp + window - DateTime.UtcNow
                    : TimeSpan.Zero;

                // Add random jitter to prevent all waiting tasks from waking up at the same time
                wait = (requestWait > tokenWait ? requestWait : tokenWait) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 50));
            }

            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, ct);
        }
    }

    public void RecordTokens(long amount)
    {
        if (amount <= 0) return;

        lock (gate)
        {
            tokenEvents.Enqueue((DateTime.UtcNow, amount));
            currentTokens += amount;
        }
    }

    private void Prune()
    {
        DateTime cutoff = DateTime.UtcNow - window;

        while (requestEvents.Count > 0 && requestEvents.Peek().Timestamp < cutoff)
            currentRequests -= requestEvents.Dequeue().Amount;

        while (tokenEvents.Count > 0 && tokenEvents.Peek().Timestamp < cutoff)
            currentTokens -= tokenEvents.Dequeue().Amount;
    }
}
