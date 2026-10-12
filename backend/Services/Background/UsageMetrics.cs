using System.Collections.Concurrent;
using KnowledgeBank.Models;

namespace KnowledgeBank.Services.Background;

/// <summary>
/// Counts app usage as daily totals per metric. Nothing about who did something is recorded, only how often,
/// how often it failed and how long it took. Totals live in memory until UsageFlushService writes them to the database.
/// </summary>
public class UsageMetrics
{
    private class Bucket
    {
        public long Count;
        public long FailureCount;
        public long TotalDurationMs;
    }

    private readonly ConcurrentDictionary<(DateOnly Date, string Metric), Bucket> buckets = new();

    public void Record(string metric, bool success = true, TimeSpan? duration = null, long count = 1)
    {
        Bucket bucket = buckets.GetOrAdd((DateOnly.FromDateTime(DateTime.UtcNow), metric), _ => new Bucket());
        Interlocked.Add(ref bucket.Count, count);
        if (!success) Interlocked.Add(ref bucket.FailureCount, count);
        if (duration is TimeSpan d) Interlocked.Add(ref bucket.TotalDurationMs, (long)d.TotalMilliseconds);
    }

    /// <summary>
    /// Takes the totals recorded since the last call. Each field is swapped to zero atomically, so anything recorded
    /// meanwhile stays for the next call instead of being lost.
    /// </summary>
    public List<UsageCounter> TakePending()
    {
        List<UsageCounter> taken = [];

        foreach (((DateOnly date, string metric), Bucket bucket) in buckets)
        {
            UsageCounter counter = new()
            {
                Date = date,
                Metric = metric,
                Count = Interlocked.Exchange(ref bucket.Count, 0),
                FailureCount = Interlocked.Exchange(ref bucket.FailureCount, 0),
                TotalDurationMs = Interlocked.Exchange(ref bucket.TotalDurationMs, 0)
            };

            if (counter.Count > 0 || counter.FailureCount > 0 || counter.TotalDurationMs > 0)
                taken.Add(counter);
        }

        // Past days only get recordings right around midnight, so their buckets can go once emptied
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach ((DateOnly Date, string Metric) key in buckets.Keys.Where(k => k.Date < today))
            buckets.TryRemove(key, out _);

        return taken;
    }

    /// <summary>
    /// Adds totals back after a failed write, so the next flush retries them.
    /// </summary>
    public void Restore(UsageCounter counter)
    {
        Bucket bucket = buckets.GetOrAdd((counter.Date, counter.Metric), _ => new Bucket());
        Interlocked.Add(ref bucket.Count, counter.Count);
        Interlocked.Add(ref bucket.FailureCount, counter.FailureCount);
        Interlocked.Add(ref bucket.TotalDurationMs, counter.TotalDurationMs);
    }
}
