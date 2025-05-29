using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Utils;
using KnowledgeBank.Models;

/// <summary>
/// A background service that runs daily to clean up archived resources from the database.
/// 
/// Author: Rens van Moorsel
/// </summary>
public class TrashbinCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes the cleanup service with the specified service provider.
    /// 
    /// Author: Rens van Moorsel
    /// </summary>
    public TrashbinCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Executes the cleanup task once every 24 hours or at a scheduled time.
    /// 
    /// Author: Rens van Moorsel
    /// </summary>
    /// <param name="stoppingToken">Token used to cancel execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitUntilUtils.WaitUntilTime(new TimeSpan(0, 0, 0), stoppingToken); // Run at midnight

            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                DatabaseContext dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                DateTime threshold = DateTime.UtcNow.AddDays(-30);

                List<Resource> oldResources = await dbContext.Resources
                    .Where(r => r.ArchiveDate < threshold)
                    .ToListAsync(stoppingToken);

                if (oldResources.Any())
                {
                    dbContext.Resources.RemoveRange(oldResources);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }

            await WaitUntilUtils.WaitUntilTime(new TimeSpan(0, 0, 0), stoppingToken); 
        }
    }
}
