namespace KnowledgeBank.Services.Background;

public class QueuedHostedService(IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger) : BackgroundService
{
    private const int WorkerCount = 5;

    private int busyWorkers;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Queued Hosted Service is starting.");

        await Task.WhenAll(Enumerable.Range(0, WorkerCount).Select(_ => RunWorkerAsync(stoppingToken)));

        logger.LogInformation("Queued Hosted Service is stopping.");
    }

    private async Task RunWorkerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<CancellationToken, Task> workItem;

            try
            {
                workItem = await taskQueue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            int busy = Interlocked.Increment(ref busyWorkers);
            logger.LogInformation("Processing background work item ({Busy}/{WorkerCount} workers busy, {Pending} still queued)", busy, WorkerCount, taskQueue.PendingCount);

            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing the work item.");
            }
            finally
            {
                Interlocked.Decrement(ref busyWorkers);
            }
        }
    }
}
