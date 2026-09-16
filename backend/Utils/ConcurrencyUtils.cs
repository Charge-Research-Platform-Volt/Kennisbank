namespace KnowledgeBank.Utils;

public static class ConcurrencyUtils
{
    /// <summary>
    /// Runs <paramref name="action"/> over every item with at most <paramref name="maxConcurrency"/>
    /// running at once.
    /// </summary>
    public static async Task RunBoundedAsync<T>(IEnumerable<T> items, int maxConcurrency, Func<T, Task> action, CancellationToken cancellationToken = default)
    {
        using SemaphoreSlim semaphore = new(maxConcurrency);

        // Cooperative cancellation: an item already running is left to finish (aborting an in-flight
        // OCR/embedding call mid-flight isn't worth the complexity), but nothing new starts once
        // cancellation is requested — anything still waiting on the semaphore exits immediately.
        IEnumerable<Task> tasks = items.Select(async item =>
        {
            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                await semaphore.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                if (!cancellationToken.IsCancellationRequested)
                    await action(item);
            }
            finally { semaphore.Release(); }
        });

        await Task.WhenAll(tasks);
    }
}
