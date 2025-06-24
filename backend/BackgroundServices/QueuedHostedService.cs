namespace KnowledgeBank.BackgroundServices;

public class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queued Hosted Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);

            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing the work item.");
            }
        }

        _logger.LogInformation("Queued Hosted Service is stopping.");
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


