namespace KnowledgeBank.Services.Background;

/// <summary>
/// Represents a queue for background tasks.
/// </summary>
/// <remarks>
/// This interface provides methods for queueing and dequeuing background work items
/// that can be executed asynchronously.
/// </remarks>
public interface IBackgroundTaskQueue
{
    void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
    Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    int PendingCount { get; }
}
