using System.Threading.Channels;

namespace KnowledgeBank.BackgroundServices;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    // Todo: Concurrency
    private readonly Channel<Func<CancellationToken, Task>> _queue = Channel.CreateUnbounded<Func<CancellationToken, Task>>();

    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        _queue.Writer.TryWrite(workItem);
    }

    public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task>? workItem = await _queue.Reader.ReadAsync(cancellationToken);
        return workItem;
    }
}