using System.Threading.Channels;

namespace KnowledgeBank.Services.Background;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, Task>> queue = Channel.CreateUnbounded<Func<CancellationToken, Task>>();

    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        queue.Writer.TryWrite(workItem);
    }

    public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task>? workItem = await queue.Reader.ReadAsync(cancellationToken);
        return workItem;
    }
}
