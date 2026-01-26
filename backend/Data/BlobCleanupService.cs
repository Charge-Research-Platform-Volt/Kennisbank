// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Utils;
using Serilog;

/// <summary>
/// A background service that runs daily to clean up orphaned blobs from Azure Blob Storage.
/// Deletes blobs that are older than 24 hours and have no corresponding database entry.
/// </summary>
public class BlobCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Serilog.ILogger _logger;

    /// <summary>
    /// Initializes the cleanup service with the specified service provider.
    /// </summary>
    public BlobCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = Log.ForContext<BlobCleanupService>();
    }

    /// <summary>
    /// Executes the cleanup task once every 24 hours at midnight.
    /// </summary>
    /// <param name="stoppingToken">Token used to cancel execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("BlobCleanupService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitUntilUtils.WaitUntilTime(new TimeSpan(0, 0, 0), stoppingToken); // Run at midnight

            _logger.Information("Starting blob cleanup process");

            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IAzureBlobService blobService = scope.ServiceProvider.GetRequiredService<IAzureBlobService>();
                IDbContextFactory<DatabaseContext> dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>();
                await using var context = await dbFactory.CreateDbContextAsync();
                
                try
                {
                    await CleanupOrphanedBlobs(blobService, context);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error during blob cleanup");
                }
            }

            _logger.Information("Blob cleanup process completed");
        }

        _logger.Information("BlobCleanupService stopped");
    }

    /// <summary>
    /// Cleans up orphaned blobs from the files container.
    /// </summary>
    private async Task CleanupOrphanedBlobs(IAzureBlobService blobService, DatabaseContext context)
    {
        // Determine the age threshold (24 hours)
        DateTime threshold = DateTime.UtcNow.AddHours(-24);
        _logger.Information("Cleaning up blobs older than {Threshold}", threshold);

        int deletedCount = 0;
        int skippedCount = 0;
        int errorCount = 0;

        // List all blobs in the files container
        string[]? blobs = await blobService.ListBlobsAsync("files");

        if (blobs == null || blobs.Length == 0)
        {
            _logger.Information("No blobs found in files container");
            return;
        }

        _logger.Information("Found {Count} blobs in files container", blobs.Length);

        foreach (string blobName in blobs)
        {
            try
            {
                // Download blob to get metadata and check upload timestamp
                var blobResponse = await blobService.DownloadBlobAsync("files", blobName);

                if (blobResponse == null)
                {
                    _logger.Warning("Blob {BlobName} not found when attempting to check metadata", blobName);
                    continue;
                }

                // Check upload timestamp from metadata
                if (blobResponse.Value.Metadata.TryGetValue("uploadTimestamp", out string? timestampStr))
                {
                    if (DateTime.TryParse(timestampStr, out DateTime uploadTime))
                    {
                        if (uploadTime > threshold)
                        {
                            // Blob is too recent, skip it
                            skippedCount++;
                            continue;
                        }
                    }
                    else
                    {
                        _logger.Warning("Failed to parse upload timestamp for blob {BlobName}: {Timestamp}", blobName, timestampStr);
                    }
                }
                else
                {
                    // No timestamp metadata, check if blob exists in database
                    _logger.Warning("Blob {BlobName} has no uploadTimestamp metadata", blobName);
                }

                // Check if resource exists in database
                bool existsInDatabase = await context.Resources
                    .AnyAsync(r => r.Id.ToString() == blobName);

                if (existsInDatabase)
                {
                    // Blob has a corresponding database entry, keep it
                    skippedCount++;
                    _logger.Debug("Blob {BlobName} has database entry, skipping", blobName);
                    continue;
                }

                // Blob is orphaned and older than 24h, delete it
                _logger.Information("Deleting orphaned blob {BlobName}", blobName);
                BLOB_STATUSCODE deleteResult = await blobService.DeleteBlobAsync("files", blobName);

                if (deleteResult == BLOB_STATUSCODE.OK)
                {
                    deletedCount++;
                    _logger.Information("Successfully deleted orphaned blob {BlobName}", blobName);
                }
                else if (deleteResult == BLOB_STATUSCODE.NOTFOUND)
                {
                    _logger.Warning("Blob {BlobName} not found when attempting to delete", blobName);
                }
                else
                {
                    errorCount++;
                    _logger.Error("Failed to delete blob {BlobName}: {Status}", blobName, deleteResult);
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.Error(ex, "Error processing blob {BlobName}", blobName);
            }
        }

        _logger.Information(
            "Blob cleanup completed. Deleted: {Deleted}, Skipped: {Skipped}, Errors: {Errors}",
            deletedCount,
            skippedCount,
            errorCount
        );
    }
}
