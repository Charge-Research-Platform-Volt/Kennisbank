using KnowledgeBank.Data;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace KnowledgeBank.Services.Storage;

/// <summary>
/// Background service that cleans up orphaned objects from storage.
/// Runs daily and deletes objects older than 48 hours (based on lastModified) that are neither:
/// - A resource file (ID exists in Resources table)
/// - A user avatar (ID exists in Users table)
/// - A OCR cache (ID exists in Resources table)
/// - A chat attachment (ID exists in chat attachments)
/// </summary>
public class StorageCleanupService(IServiceProvider serviceProvider, EnvironmentConfig environmentConfig) : BackgroundService
{
    private readonly Serilog.ILogger logger = Log.ForContext<StorageCleanupService>();
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);
    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(48);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.Information("StorageCleanupService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.Information("Starting storage cleanup");

            using var scope = serviceProvider.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>();

            try
            {
                await CleanupOrphanedObjects(storageService, dbFactory, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during storage cleanup");
            }

            logger.Information("Storage cleanup completed");

            await WaitUntilUtils.WaitUntilTime(new TimeSpan(0, 0, 0), stoppingToken); // Next midnight
        }

        logger.Information("StorageCleanupService stopped");
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
        StorageObjectInfo[] objects = await storageService.ListObjectsAsync(bucketName);

        if (objects.Length == 0)
        {
            logger.Information("No objects found in bucket {BucketName}", bucketName);
            return;
        }

        logger.Information("Found {Count} objects in bucket {BucketName}", objects.Length, bucketName);

        DateTime cutoff = DateTime.UtcNow - GracePeriod;

        await ConcurrencyUtils.RunBoundedAsync(objects, 10, async obj =>
        {
            string objectName = obj.Key;

            // Anything recently written may belong to an upload whose db row does not exist yet
            // (user still in upload form). Dont treat these as orphaned. Same for OCR cache objects
            // A missing timestamp is treated as "too young" so we fail towards keeping the file
            if (obj.LastModifiedUtc is not DateTime lastModified || lastModified > cutoff)
            {
                Interlocked.Increment(ref skippedCount);
                return;
            }

            await using var context = await dbFactory.CreateDbContextAsync(stoppingToken);

            try
            {
                // OCR cache objects are named "{resourceId}-ocr" — strip the suffix to recover the owning resource ID
                bool isOcrCache = objectName.EndsWith("-ocr", StringComparison.Ordinal);
                string guidPart = isOcrCache ? objectName[..^"-ocr".Length] : objectName;

                // Check if this is a valid GUID (our object names are resource IDs)
                if (!Guid.TryParse(guidPart, out Guid objectId))
                {
                    logger.Warning("Object {ObjectName} is not a valid GUID, skipping", objectName);
                    Interlocked.Increment(ref skippedCount);
                    return;
                }

                // Check if resource exists in database or if it is a chat attachment
                bool isResource = await context.Resources.AnyAsync(r => r.Id == objectId, stoppingToken);
                bool isMessageAttachment = await context.MessageAttachments.AnyAsync(a => a.Id == objectId, stoppingToken);
                if (isResource || isMessageAttachment)
                {
                    Interlocked.Increment(ref skippedCount);
                    return;
                }

                // OCR cache for a resource that no longer exists — falls through to delete below
                if (!isOcrCache)
                {
                    // Check if this is a user avatar (user ID matches object name)
                    bool isUserAvatar = await context.Users.AnyAsync(u => u.Id == objectName, stoppingToken);
                    if (isUserAvatar)
                    {
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }
                }

                // Object is orphaned - delete it
                logger.Information("Deleting orphaned object {ObjectName}", objectName);
                await storageService.DeleteObjectAsync(bucketName, objectName);
                Interlocked.Increment(ref deletedCount);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref errorCount);
                logger.Error(ex, "Error processing object {ObjectName}", objectName);
            }
        }, stoppingToken);

        logger.Information(
            "Storage cleanup finished. Deleted: {Deleted}, Skipped: {Skipped}, Errors: {Errors}",
            deletedCount, skippedCount, errorCount);
    }
}
