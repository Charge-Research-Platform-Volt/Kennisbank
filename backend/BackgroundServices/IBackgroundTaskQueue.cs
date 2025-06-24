namespace KnowledgeBank.BackgroundServices;

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
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


