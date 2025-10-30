// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.StaticFiles;
using System.Text;

namespace KnowledgeBank.Controllers
{
    /// <summary>
    /// Controller for handling file upload operations to Azure Blob Storage.
    /// Uses chunked upload strategy for all files regardless of size.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class FilesController(IAzureBlobService blobService, ResourceManager resourceManager) : ControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<FilesController>();

        #region Upload Init
        /// <summary>
        /// Initializes a file upload session and returns a GUID for the upload.
        /// This endpoint must be called before uploading chunks.
        /// </summary>
        /// <param name="dto">File initialization information</param>
        /// <returns>Upload session information including GUID</returns>
        [HttpPost("upload/init")]
        [SwaggerOperation(Summary = "Initialize a file upload session")]
        [SwaggerResponse(200, "Upload session initialized", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public IActionResult UploadInit([FromBody] FileUploadInitDto dto)
        {
            // Validation
            if (string.IsNullOrEmpty(dto.FileName))
                return BadRequest(new ApiResponse(false, "File name is required."));

            if (dto.FileSize <= 0)
                return BadRequest(new ApiResponse(false, "Invalid file size."));

            try
            {
                // Generate a unique GUID for this upload
                Guid uploadGuid = Guid.NewGuid();

                // Extract file information
                string extension = Path.GetExtension(dto.FileName);

                if (string.IsNullOrEmpty(extension))
                    return BadRequest(new ApiResponse(false, "File must have an extension."));

                // Check if the filetype is supported
                if (!Filetype.Supported(extension))
                    return BadRequest(new ApiResponse(false, $"File type '{extension}' is not supported."));

                logger.Information("Upload session initialized for file '{FileName}' with GUID {Guid}", dto.FileName, uploadGuid);

                // Return upload session information
                return Ok(new ApiResponse(true, "Upload session initialized successfully.", new
                {
                    guid = uploadGuid,
                    fileName = dto.FileName,
                    fileSize = dto.FileSize,
                    extension
                }));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error initializing upload session for file '{FileName}'", dto.FileName);
                return StatusCode(500, new ApiResponse(false, "Error initializing upload session.", e.Message));
            }
        }
        #endregion

        #region Upload Chunk
        /// <summary>
        /// Uploads a single chunk of a file. Can be called multiple times for large files,
        /// or once for small files. Each chunk is staged with a unique block ID.
        /// </summary>
        /// <param name="guid">Upload session GUID from initialization</param>
        /// <param name="blockId">Unique block ID (hex encoded, will be converted to base64)</param>
        [HttpPost("upload/chunk/{guid}/{blockId}")]
        [SwaggerOperation(Summary = "Upload a chunk of a file")]
        [SwaggerResponse(200, "Chunk uploaded successfully", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> UploadChunk(string guid, string blockId)
        {
            // Validation
            if (Request.Body == null)
                return BadRequest(new ApiResponse(false, "No chunk data was provided."));

            if (string.IsNullOrEmpty(guid) || !ValidityUtil.IsValidId(guid))
                return BadRequest(new ApiResponse(false, "Invalid upload GUID."));

            if (string.IsNullOrEmpty(blockId))
                return BadRequest(new ApiResponse(false, "Block ID is required."));

            try
            {
                // Convert hex blockId to base64 (Azure requires base64)
                string base64BlockId = Convert.ToBase64String(Convert.FromHexString(blockId));

                // Get or create the files container
                BlobContainerClient container = await blobService.GetOrCreateContainerAsync("files");

                // Get block blob client
                BlockBlobClient blockBlobClient = container.GetBlockBlobClient(guid);

                // Read request body into memory stream
                using MemoryStream memoryStream = new MemoryStream();
                await Request.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Stage the chunk
                await blockBlobClient.StageBlockAsync(base64BlockId, memoryStream);

                logger.Information("Chunk {BlockId} uploaded for GUID {Guid} ({Size} bytes)", blockId, guid, memoryStream.Length);

                return Ok(new ApiResponse(true, "Chunk uploaded successfully."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error uploading chunk {BlockId} for GUID {Guid}", blockId, guid);
                return StatusCode(500, new ApiResponse(false, "Error uploading chunk.", e.Message));
            }
        }
        #endregion

        #region Upload Finalize
        /// <summary>
        /// Finalizes a file upload by committing all uploaded chunks in order.
        /// This creates the final blob from all staged chunks.
        /// </summary>
        /// <param name="dto">Finalization information including block IDs</param>
        [HttpPost("upload/finalize")]
        [SwaggerOperation(Summary = "Finalize a file upload")]
        [SwaggerResponse(200, "File upload finalized successfully", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> UploadFinalize([FromBody] FileUploadFinalizeDto dto)
        {
            // Validation
            if (string.IsNullOrEmpty(dto.Guid) || !ValidityUtil.IsValidId(dto.Guid))
                return BadRequest(new ApiResponse(false, "Invalid upload GUID."));

            if (string.IsNullOrEmpty(dto.FileName))
                return BadRequest(new ApiResponse(false, "File name is required."));

            if (dto.BlockIds == null || dto.BlockIds.Count == 0)
                return BadRequest(new ApiResponse(false, "No block IDs were provided."));

            try
            {
                logger.Information("Finalizing file upload for GUID {Guid} with {BlockCount} blocks", dto.Guid, dto.BlockIds.Count);

                string extension = Path.GetExtension(dto.FileName);

                // Convert hex block IDs to base64
                List<string> base64BlockIds;
                try
                {
                    base64BlockIds = dto.BlockIds
                        .Select(id => Convert.ToBase64String(Convert.FromHexString(id)))
                        .ToList();
                }
                catch (Exception hexException)
                {
                    logger.Error(hexException, "Failed to convert block IDs from hex to base64 for GUID {Guid}", dto.Guid);
                    return BadRequest(new ApiResponse(false, "Invalid block ID format. Unable to convert from hex."));
                }

                // Create metadata to attach to the blob
                Dictionary<string, string> metadata = new()
                {
                    { "extension", extension },
                    { "originalFileName", dto.FileName },
                    { "uploadTimestamp", DateTime.UtcNow.ToString("o") }
                };

                // Commit the block list
                BLOB_STATUSCODE code = await blobService.CommitBlockListAsync(
                    dto.Guid,
                    "files",
                    base64BlockIds,
                    metadata
                );

                // Handle response
                switch (code)
                {
                    case BLOB_STATUSCODE.OK:
                        logger.Information("File upload finalized successfully for GUID {Guid}", dto.Guid);
                        return Ok(new ApiResponse(true, "File upload finalized successfully.", new
                        {
                            guid = dto.Guid,
                            fileName = dto.FileName,
                            extension
                        }));

                    case BLOB_STATUSCODE.FAILED:
                        logger.Error("Block list commit failed for GUID {Guid}. Some blocks may be missing or invalid.", dto.Guid);
                        return Conflict(new ApiResponse(false, "Failed to commit block list. Some uploaded blocks may be missing or invalid."));

                    default:
                        logger.Error("Unexpected blob service response {StatusCode} for GUID {Guid}", code, dto.Guid);
                        return StatusCode(500, new ApiResponse(false, "Unexpected error during finalization."));
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error finalizing file upload for GUID {Guid}", dto.Guid);
                return StatusCode(500, new ApiResponse(false, "Error finalizing upload.", e.Message));
            }
        }
        #endregion

        #region Upload Cancel
        /// <summary>
        /// Cancels an upload by deleting all staged chunks.
        /// Note: Uncommitted chunks are automatically cleaned up by Azure after 7 days,
        /// but this endpoint allows immediate cleanup.
        /// </summary>
        /// <param name="guid">Upload session GUID to cancel</param>
        [HttpDelete("upload/cancel/{guid}")]
        [SwaggerOperation(Summary = "Cancel an upload session")]
        [SwaggerResponse(200, "Upload cancelled successfully", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> UploadCancel(string guid)
        {
            // Validation
            if (string.IsNullOrEmpty(guid) || !ValidityUtil.IsValidId(guid))
                return BadRequest(new ApiResponse(false, "Invalid upload GUID."));

            try
            {
                logger.Information("Cancelling upload for GUID {Guid}", guid);

                // Try to commit an empty block list, which will clear any uncommitted blocks
                BLOB_STATUSCODE code = await blobService.CommitBlockListAsync(
                    guid,
                    "files",
                    [],
                    []
                );

                if (code == BLOB_STATUSCODE.OK)
                {
                    // Now delete the empty blob
                    await blobService.DeleteBlobAsync("files", guid);
                    logger.Information("Upload cancelled successfully for GUID {Guid}", guid);
                    return Ok(new ApiResponse(true, "Upload cancelled successfully."));
                }
                else if (code == BLOB_STATUSCODE.NOTFOUND)
                {
                    logger.Information("No upload found for GUID {Guid}, nothing to cancel", guid);
                    return Ok(new ApiResponse(true, "No upload found, nothing to cancel."));
                }
                else
                {
                    logger.Warning("Unexpected response when cancelling upload for GUID {Guid}: {Code}", guid, code);
                    return StatusCode(500, new ApiResponse(false, "Error cancelling upload."));
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error cancelling upload for GUID {Guid}", guid);
                return StatusCode(500, new ApiResponse(false, "Error cancelling upload.", e.Message));
            }
        }
        #endregion
        
        #region Download
        /// <summary>
        /// Downloads a file by its GUID. Uses the resource title from database as the filename.
        /// </summary>
        /// <param name="id">The GUID of the file (resource ID)</param>
        [HttpGet("download/{id}")]
        [SwaggerOperation(Summary = "Download a file from storage")]
        [SwaggerResponse(200, "The requested file")]
        [SwaggerResponse(404, "File not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Download(string id)
        {
            // Check if ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID"));

            try
            {
                logger.Information("Downloading file with ID: {ID}", id);

                // Try to retrieve the file from the files container
                BlobDownloadResponse? maybeResponse = await blobService.DownloadBlobAsync("files", id);

                // If response is empty, the file does not exist in storage
                if (maybeResponse == null)
                    return NotFound(new ApiResponse(false, "File not found in storage."));

                // Convert to a non-empty response
                BlobDownloadResponse response = (BlobDownloadResponse)maybeResponse;

                // Get the extension from blob metadata
                string extension = response.Metadata["extension"];

                // Get the title from the database to use as filename
                // This provides a better user experience than using originalFileName
                string? title = await resourceManager.GetResourcePropertyOrDefaultAsync(id, "Title");
                if (string.IsNullOrEmpty(title))
                {
                    // Fallback to original filename if resource not found in database
                    title = response.Metadata.TryGetValue("originalFileName", out string? origName)
                        ? Path.GetFileNameWithoutExtension(origName)
                        : "download";
                }
                string fileName = $"{SanitizeFileName(title)}{extension}";

                // Set the content type
                string contentType = "application/octet-stream";
                if (!string.IsNullOrEmpty(extension))
                {
                    FileExtensionContentTypeProvider provider = new();
                    if (provider.TryGetContentType(fileName, out string? type) && !string.IsNullOrEmpty(type))
                        contentType = type;
                }

                logger.Information("Streaming file with ID '{ID}' to client.", id);

                // Determine whether to inline (open in browser) or download
                const long maxFileSize = 500 * 1024 * 1024; // 500 MB max for inline
                bool canBeOpened = (
                    contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
                    contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
                    contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                    contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase));

                string contentDisposition;
                if (canBeOpened && response.ContentLength <= maxFileSize)
                    contentDisposition = $"inline; filename=\"{fileName}\"";
                else
                    contentDisposition = $"attachment; filename=\"{fileName}\"";

                Response.ContentType = contentType;
                Response.Headers.Append("Content-Disposition", contentDisposition);

                // Stream the file directly to the response body (no buffering)
                await response.FileStream.CopyToAsync(Response.Body);

                logger.Information("File with ID '{ID}' downloaded successfully.", id);
                return new EmptyResult();
            }
            catch (Exception e)
            {
                logger.Error(e, "Error downloading file with ID {ID}.", id);
                return StatusCode(500, new ApiResponse(false, "Error downloading file", e.Message));
            }
        }

        // Helper method to sanitize filenames
        private static string SanitizeFileName(string fileName, bool preserveSpaces = true)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            // Get invalid chars
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder sb = new();

            // Define problematic characters to replace
            char[] problematicChars = { '\u2018', '\u2019', '\u201C', '\u201D', '\u2014', '\u2013', '\u2026' };

            foreach (char c in fileName)
            {
                // If space and preserve spaces is on, append
                if (c == ' ' && preserveSpaces)
                    sb.Append(c);
                // If space and preserve spaces is not on, put underscore
                else if (c == ' ' && !preserveSpaces)
                    sb.Append('_');
                // If char is valid, append
                else if (!invalidChars.Contains(c) && !problematicChars.Contains(c))
                    sb.Append(c);
                // If char is invalid, put underscore
                else
                    sb.Append('_');
            }

            // Build string and trim
            string result = sb.ToString().Trim();

            // Remove the '.' at start
            if (result.StartsWith('.'))
                result = "_" + result.TrimStart('.');

            // If resulting string is empty put in unnamed name
            if (string.IsNullOrEmpty(result))
                return "unnamed";

            // Limit length of name
            const int maxLength = 255;
            if (result.Length > maxLength)
                result = result[..maxLength];

            return result;
        }
        #endregion

        #region Migration
        /// <summary>
        /// Migrates all blobs from legacy containers (document, audio, video) to the new unified 'files' container.
        /// This is a one-time migration for existing deployments.
        /// </summary>
        [HttpPost("migrate")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Migrate blobs from legacy containers to 'files' container")]
        [SwaggerResponse(200, "Migration completed successfully", typeof(ApiResponse))]
        [SwaggerResponse(207, "Partial success - some blobs failed to migrate", typeof(ApiResponse))]
        [SwaggerResponse(500, "Migration failed completely", typeof(ApiResponse))]
        public async Task<IActionResult> MigrateBlobsToFilesContainer()
        {
            try
            {
                logger.Information("Starting blob migration from legacy containers to 'files' container");

                // Ensure the 'files' container exists before migration
                await blobService.GetOrCreateContainerAsync("files");
                logger.Information("Files container verified/created");

                string[] legacyContainers = { "document", "audio", "video" };
                int totalMigrated = 0;
                int totalFailed = 0;
                List<string> errors = new();

                foreach (string containerName in legacyContainers)
                {
                    logger.Information("Processing container: {ContainerName}", containerName);

                    // List all blobs in the legacy container
                    string[]? blobs = await blobService.ListBlobsAsync(containerName);

                    if (blobs == null || blobs.Length == 0)
                    {
                        logger.Information("No blobs found in container {ContainerName}", containerName);
                        continue;
                    }

                    logger.Information("Found {Count} blobs in {ContainerName}", blobs.Length, containerName);

                    // Copy each blob to the files container
                    foreach (string blobName in blobs)
                    {
                        try
                        {
                            // Copy blob from legacy container to files container
                            BLOB_STATUSCODE result = await blobService.CopyBlobAsync(
                                containerName,
                                blobName,
                                "files",
                                blobName,
                                overwrite: false,
                                surpressLogging: true
                            );

                            if (result == BLOB_STATUSCODE.OK)
                            {
                                totalMigrated++;
                                logger.Information("Migrated blob {BlobName} from {Container}", blobName, containerName);
                            }
                            else if (result == BLOB_STATUSCODE.ALREADYEXISTS)
                            {
                                logger.Information("Blob {BlobName} already exists in files container, skipping", blobName);
                                totalMigrated++; // Count as migrated since it's already there
                            }
                            else
                            {
                                string error = $"Failed to migrate {blobName} from {containerName}: {result}";
                                errors.Add(error);
                                totalFailed++;
                                logger.Warning(error);
                            }
                        }
                        catch (Exception ex)
                        {
                            string error = $"Error migrating {blobName} from {containerName}: {ex.Message}";
                            errors.Add(error);
                            totalFailed++;
                            logger.Error(ex, error);
                        }
                    }
                }

                string message = $"Migration completed. Migrated: {totalMigrated}, Failed: {totalFailed}";

                // If all migrations failed, return error
                if (totalMigrated == 0 && totalFailed > 0)
                {
                    logger.Error("Migration failed completely - no blobs were migrated");
                    return StatusCode(500, new ApiResponse(false, "Migration failed - no blobs were migrated.", new
                    {
                        migrated = totalMigrated,
                        failed = totalFailed,
                        errors
                    }));
                }

                // If some migrations failed, return partial success with warning
                if (totalFailed > 0)
                {
                    logger.Warning(message);
                    return StatusCode(207, new ApiResponse(true, message + " (Partial success - some blobs failed)", new
                    {
                        migrated = totalMigrated,
                        failed = totalFailed,
                        errors
                    }));
                }

                // All succeeded
                logger.Information(message);
                return Ok(new ApiResponse(true, message, new
                {
                    migrated = totalMigrated,
                    failed = totalFailed
                }));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error during blob migration");
                return StatusCode(500, new ApiResponse(false, "Error during migration", e.Message));
            }
        }

        /// <summary>
        /// Verifies the migration by checking if all blobs from legacy containers exist in 'files' container.
        /// </summary>
        [HttpGet("migrate/verify")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Verify blob migration status")]
        [SwaggerResponse(200, "Verification completed", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> VerifyMigration()
        {
            try
            {
                logger.Information("Verifying blob migration");

                string[] legacyContainers = { "document", "audio", "video" };
                int totalLegacyBlobs = 0;
                int foundInFiles = 0;
                int notFoundInFiles = 0;
                List<string> missingBlobs = new();

                foreach (string containerName in legacyContainers)
                {
                    string[]? blobs = await blobService.ListBlobsAsync(containerName);

                    if (blobs == null || blobs.Length == 0)
                        continue;

                    totalLegacyBlobs += blobs.Length;

                    foreach (string blobName in blobs)
                    {
                        BLOB_STATUSCODE exists = await blobService.BlobExistsAsync("files", blobName);

                        if (exists == BLOB_STATUSCODE.OK)
                        {
                            foundInFiles++;
                        }
                        else
                        {
                            notFoundInFiles++;
                            missingBlobs.Add($"{containerName}/{blobName}");
                        }
                    }
                }

                bool allMigrated = notFoundInFiles == 0;
                string message = allMigrated
                    ? $"All {totalLegacyBlobs} blobs have been migrated successfully."
                    : $"Migration incomplete: {foundInFiles}/{totalLegacyBlobs} blobs migrated. {notFoundInFiles} blobs missing.";

                logger.Information(message);

                return Ok(new ApiResponse(allMigrated, message, new
                {
                    totalLegacyBlobs,
                    foundInFiles,
                    notFoundInFiles,
                    missingBlobs = missingBlobs.Take(100).ToArray() // Limit to first 100
                }));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error verifying migration");
                return StatusCode(500, new ApiResponse(false, "Error verifying migration", e.Message));
            }
        }

        /// <summary>
        /// Deletes legacy containers after successful migration.
        /// WARNING: This is irreversible! Verify migration first.
        /// </summary>
        [HttpDelete("migrate/cleanup")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Delete legacy containers after migration")]
        [SwaggerResponse(200, "Cleanup completed", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> CleanupLegacyContainers([FromQuery] bool confirm = false)
        {
            if (!confirm)
            {
                return BadRequest(new ApiResponse(false, "This action is irreversible. Set confirm=true to proceed."));
            }

            try
            {
                logger.Warning("Deleting legacy containers - this action is irreversible");

                string[] legacyContainers = { "document", "audio", "video" };
                int deleted = 0;
                List<string> results = new();

                foreach (string containerName in legacyContainers)
                {
                    BLOB_STATUSCODE result = await blobService.DeleteContainerAsync(containerName);

                    if (result == BLOB_STATUSCODE.OK)
                    {
                        deleted++;
                        results.Add($"Deleted container: {containerName}");
                        logger.Information("Deleted container {ContainerName}", containerName);
                    }
                    else if (result == BLOB_STATUSCODE.NOTFOUND)
                    {
                        results.Add($"Container not found: {containerName}");
                        logger.Information("Container {ContainerName} not found, already deleted", containerName);
                    }
                    else
                    {
                        results.Add($"Failed to delete container: {containerName}");
                        logger.Warning("Failed to delete container {ContainerName}", containerName);
                    }
                }

                string message = $"Cleanup completed. Deleted {deleted} legacy containers.";
                logger.Information(message);

                return Ok(new ApiResponse(true, message, results));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error during cleanup");
                return StatusCode(500, new ApiResponse(false, "Error during cleanup", e.Message));
            }
        }
        #endregion
    }
}
