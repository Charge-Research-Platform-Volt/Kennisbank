using System.Threading.Channels;

namespace KnowledgeBank.Services.Background;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, Task>> queue = Channel.CreateUnbounded<Func<CancellationToken, Task>>();
    private int pendingCount;

    public int PendingCount => pendingCount;

    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        Interlocked.Increment(ref pendingCount);
        queue.Writer.TryWrite(workItem);
    }

    public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task>? workItem = await queue.Reader.ReadAsync(cancellationToken);
        Interlocked.Decrement(ref pendingCount);
        return workItem;
    }
}
