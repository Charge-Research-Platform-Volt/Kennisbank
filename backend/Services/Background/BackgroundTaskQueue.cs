using System.Threading.Channels;

namespace KnowledgeBank.Services.Background;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, Task>> queue = Channel.CreateUnbounded<Func<CancellationToken, Task>>();
    private int pendingCount;
    private int runningCount;

    public int PendingCount => pendingCount;
    public int RunningCount => runningCount;
    public int MarkStarted() => Interlocked.Increment(ref runningCount);
    public void MarkFinished() => Interlocked.Decrement(ref runningCount);

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
