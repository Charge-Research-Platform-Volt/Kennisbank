using KnowledgeBank.Data;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace KnowledgeBank.Services.Storage;

/// <summary>
/// Background service that cleans up orphaned objects from storage.
/// Runs daily and deletes objects older than 24 hours that are neither:
/// - A resource file (ID exists in Resources table)
/// - A user avatar (ID exists in Users table)
/// </summary>
public class StorageCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Serilog.ILogger _logger;
    private readonly string _bucketName;

    public StorageCleanupService(IServiceProvider serviceProvider, EnvironmentConfig environmentConfig)
    {
        _serviceProvider = serviceProvider;
        _logger = Log.ForContext<StorageCleanupService>();
        _bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("StorageCleanupService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait until midnight
            await WaitUntilUtils.WaitUntilTime(new TimeSpan(0, 0, 0), stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            _logger.Information("Starting storage cleanup");

            using var scope = _serviceProvider.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>();

            try
            {
                await CleanupOrphanedObjects(storageService, dbFactory, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during storage cleanup");
            }

            _logger.Information("Storage cleanup completed");
        }

        _logger.Information("StorageCleanupService stopped");
    }

    private async Task CleanupOrphanedObjects(
        IStorageService storageService,
        IDbContextFactory<DatabaseContext> dbFactory,
        CancellationToken stoppingToken)
    {
        int deletedCount = 0;
        int skippedCount = 0;
        int errorCount = 0;

        // List all objects in the bucket
        string[] objects = await storageService.ListObjectsAsync(_bucketName);

        if (objects.Length == 0)
        {
            _logger.Information("No objects found in bucket {BucketName}", _bucketName);
            return;
        }

        _logger.Information("Found {Count} objects in bucket {BucketName}", objects.Length, _bucketName);

        await using var context = await dbFactory.CreateDbContextAsync(stoppingToken);

        foreach (string objectName in objects)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                // Check if this is a valid GUID (our object names are resource IDs)
                if (!Guid.TryParse(objectName, out Guid objectId))
                {
                    _logger.Warning("Object {ObjectName} is not a valid GUID, skipping", objectName);
                    skippedCount++;
                    continue;
                }

                // Check upload timestamp from metadata
                var metadata = await storageService.GetObjectMetadataAsync(_bucketName, objectName);

                if (metadata.TryGetValue("uploadTimestamp", out string? timestampStr) &&
                    DateTime.TryParse(timestampStr, out DateTime uploadTime))
                {
                    // Skip if uploaded less than 24 hours ago
                    if (uploadTime > DateTime.UtcNow.AddHours(-24))
                    {
                        skippedCount++;
                        continue;
                    }
                }

                // Check if resource exists in database
                bool isResource = await context.Resources.AnyAsync(r => r.Id == objectId, stoppingToken);
                if (isResource)
                {
                    skippedCount++;
                    continue;
                }

                // Check if this is a user avatar (user ID matches object name)
                bool isUserAvatar = await context.Users.AnyAsync(u => u.Id == objectName, stoppingToken);
                if (isUserAvatar)
                {
                    skippedCount++;
                    continue;
                }

                // Object is orphaned - delete it
                _logger.Information("Deleting orphaned object {ObjectName}", objectName);
                await storageService.DeleteObjectAsync(_bucketName, objectName);
                deletedCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.Error(ex, "Error processing object {ObjectName}", objectName);
            }
        }

        _logger.Information(
            "Storage cleanup finished. Deleted: {Deleted}, Skipped: {Skipped}, Errors: {Errors}",
            deletedCount, skippedCount, errorCount);
    }
}
