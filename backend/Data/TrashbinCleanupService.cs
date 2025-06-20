using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Utils;
using KnowledgeBank.Models;

/// <summary>
/// A background service that runs daily to clean up archived resources from the database.
/// </summary>
public class TrashbinCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes the cleanup service with the specified service provider.
    /// </summary>
    public TrashbinCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Executes the cleanup task once every 24 hours or at a scheduled time.
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
                ResourceManager resourceManager = scope.ServiceProvider.GetRequiredService<ResourceManager>();

                // Determine threshold
                DateTime threshold = DateTime.UtcNow.AddDays(-30);

                // Start transaction
                await resourceManager.BeginTransaction();
                
                try 
                {   
                    // Fetch all IDs
                    string[] resources = await resourceManager.GetAllResourcesAsync(predicate: r => r.TrashDate < threshold, projection: "Id") as string[] ?? [];
                    string[] persons = await resourceManager.GetAllPersonsAsync(predicate: p => p.TrashDate > threshold, projection: "Id") as string[] ?? [];
                    string[] organisations = await resourceManager.GetAllOrganisationsAsync(predicate: o => o.TrashDate > threshold, projection: "Id") as string[] ?? [];

                    // Delete all found IDs through resourcemanager to delete all relations as well
                    foreach (string id in resources)
                        await resourceManager.DeleteResourceAsync(id);
                    
                    foreach (string id in persons)
                        await resourceManager.DeletePersonAsync(id);
                    
                    foreach (string id in organisations)
                        await resourceManager.DeleteOrganisationAsync(id);

                    // Commit transaction
                    await resourceManager.Commit();
                }
                catch 
                {   
                    // On fail: roll back transaction
                    await resourceManager.Rollback();
                }
            }

            await WaitUntilUtils.WaitUntilTime(new TimeSpan(0, 0, 0), stoppingToken); 
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


