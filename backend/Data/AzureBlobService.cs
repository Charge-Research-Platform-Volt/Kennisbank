using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text;

namespace backend.Data
{
    public enum BLOB_STATUSCODE { OK, FAILED, NOTFOUND, ALREADYEXISTS, INVALID }

    public struct BlobPageResponse
    {
        public BLOB_STATUSCODE Status { get; }
        public string Message { get; }
        public string? ContinuationToken { get; }
        public string[] Blobs { get; }

        public BlobPageResponse(BLOB_STATUSCODE status, string message, string? continuationToken, string[] blobs)
        {
            this.Status             = status;
            this.Message            = message;
            this.ContinuationToken  = continuationToken;
            this.Blobs              = blobs;
        }
    }

    /// <summary>
    /// Service for interacting with Azure Blob Storage
    /// </summary>
    public interface IAzureBlobService
    {
        /// <summary>
        /// Gets or creates a container in the Blob Storage.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        /// <returns>The container client</returns>
        Task<BlobContainerClient> GetOrCreateContainerAsync(string containerName);

        /// <summary>
        /// Deletes a container in the Blob Storage. <br></br><br></br><br></br>
        /// Note: <br></br>
        /// This action does not require verification and deletes all blobs inside the container. This action is irreversable.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        /// <returns>If the operation was successful.</returns>
        Task<BLOB_STATUSCODE> DeleteContainerAsync(string containerName);

        /// <summary>
        /// Uploads a blob to the given container.
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="blobName">The name of the blob</param>
        /// <param name="content">The filestream</param>
        /// <param name="overwrite">Whether or not the file should be overwritten when it already exists</param>
        /// <returns>If the operation was successful</returns>
        Task<BLOB_STATUSCODE> UploadBlobAsync(string containerName, string blobName, Stream content, bool overwrite = false);

        /// <summary>
        /// Downloads a blob from the given container
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="blobName">The name of the blob</param>
        /// <returns>A stream if the file exists, null if it doesn't</returns>
        Task<Stream?> DownloadBlobAsync(string containerName, string blobName);

        /// <summary>
        /// Deletes a blob from the given container
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="blobName">The name of the blob</param>
        /// <returns>If the operation was successful</returns>
        Task<BLOB_STATUSCODE> DeleteBlobAsync(string containerName, string blobName);

        /// <summary>
        /// Checks if a blob exists in a given container
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="blobName">The name of the blob</param>
        /// <returns>If the blob exists in the given container</returns>
        Task<BLOB_STATUSCODE> BlobExistsAsync(string containerName, string blobName);


        /// <summary>
        /// Copies a blob to a new location
        /// </summary>
        /// <param name="currentContainername">The name of the current container</param>
        /// <param name="currentFileName">The name of the current file/blob</param>
        /// <param name="newContainerName">The name of the new container</param>
        /// <param name="newFileName">The name of the new file/blob</param>
        /// <param name="overwrite">Whether or not we overwrite the destination file if it already exists</param>
        /// <param name="surpressLogging">Whether or not logging should be surpressed for when you use this function inside another.</param>
        /// <returns>If the operation was successful</returns>
        Task<BLOB_STATUSCODE> CopyBlobAsync(string currentContainername, string currentFileName, string newContainerName, string newFileName, bool overwrite = false, bool surpressLogging = false);


        /// <summary>
        /// Moves a blob to a new location
        /// </summary>
        /// <param name="currentContainername">The name of the current container</param>
        /// <param name="currentFileName">The name of the current file/blob</param>
        /// <param name="newContainerName">The name of the new container</param>
        /// <param name="newFileName">The name of the new file/blob</param>
        /// <param name="overwrite">Whether or not we overwrite the destination file if it already exists</param>
        /// <param name="surpressLogging">Whether or not logging should be surpressed for when you use this function inside another.</param>
        /// <returns>If the operation was successful</returns>
        Task<BLOB_STATUSCODE> MoveBlobAsync(string currentContainername, string currentFileName, string newContainerName, string newFileName, bool overwrite = false, bool surpressLogging = false);

        /// <summary>
        /// Renames a blob in a given container
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="oldFileName">The old name of the blob</param>
        /// <param name="newFileName">The new name of the blob</param>
        /// <returns>If the operation was successful</returns>
        Task<BLOB_STATUSCODE> RenameBlobAsync(string containerName, string oldFileName, string newFileName);

        /// <summary>
        /// Lists all blobs in a given container <br></br><br></br><br></br>
        /// Note:<br></br>
        /// With large containers this can give a lot of strain on the server.
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="prefix">A prefix to filter results</param>
        /// <returns>A list of blobs found in the container</returns>
        Task<string[]?> ListBlobsAsync(string containerName, string prefix = "");

        /// <summary>
        /// Lists all blobs in a given container on a certain page
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="pageSize">The size of the page</param>
        /// <param name="prefix">A prefix to filter results</param>
        /// <param name="page">The page to be retrieved<br></br><br></br><br></br>
        /// Note:<br></br>
        /// The page count starts at 1 for easier UI integration</param>
        /// <returns>A BlobPage which contains the current page, the total amount of pages and the list of blobs on the current page</returns>
        Task<BlobPageResponse> ListBlobsPagedAsync(string containerName, int pageSize, string? continuationToken = null, string prefix = "");
    }

    /// <summary>
    /// Implementation of the Azure Blob Storage service
    /// </summary>
    public class AzureBlobService : IAzureBlobService
    {
        private readonly BlobServiceClient blobService;
        private readonly Serilog.ILogger logger;

        /// <summary>
        /// Initializes a new instance of AzureBlobService
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <exception cref="InvalidOperationException">Thown when no Azure Storage connection string is configured</exception>
        public AzureBlobService(IConfiguration configuration)
        {
            string? connectionString = configuration["AZURE_STORAGE_CONNECTION_STRING"];

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Azure Storage connection string not configured.");

            this.blobService = new BlobServiceClient(connectionString);
            this.logger = Log.ForContext<AzureBlobService>();
        }

        /// <inheritdoc/>
        public async Task<BlobContainerClient> GetOrCreateContainerAsync(string containerName)
        {
            BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync();

            logger.Information("Container {ContainerName} retrieved or created successfully", containerName);

            return container;
        }

        /// <inheritdoc/>
        public async Task<BLOB_STATUSCODE> DeleteContainerAsync(string containerName)
        {
            BlobContainerClient container = blobService.GetBlobContainerClient(containerName);

            bool result = await container.DeleteIfExistsAsync();

            logger.Information("Container {ContainerName} deleted successfully.");

            return result ? BLOB_STATUSCODE.OK : BLOB_STATUSCODE.NOTFOUND;
        }

        /// <inheritdoc/>
        public async Task<BLOB_STATUSCODE> UploadBlobAsync(string containerName, string blobName, Stream content, bool overwrite = false)
        {
            BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
            BlobClient blob = container.GetBlobClient(blobName);

            if (!await container.ExistsAsync())
            {
                logger.Information("The container {ContainerName} does not exist.", containerName);
                return BLOB_STATUSCODE.NOTFOUND;
            }

            if (await blob.ExistsAsync() && !overwrite)
            {
                logger.Information("Blob {BlobName} already exists in {ContainerName} and overwrite is disabled.", blobName, containerName);
                return BLOB_STATUSCODE.ALREADYEXISTS;
            }

            await blob.UploadAsync(content, true);
            logger.Information("Blob {BlobName} successfully uploaded to container {ContainerName}.", blobName, containerName);
            return BLOB_STATUSCODE.OK;
        }

        /// <inheritdoc/>
        public async Task<Stream?> DownloadBlobAsync(string containerName, string blobName)
        {
            BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
            BlobClient blob = container.GetBlobClient(blobName);

            if (!await blob.ExistsAsync())
            {
                logger.Information("Blob {BlobName} not found in container {ContainerName}.", blobName, containerName);
                return null;
            }

            MemoryStream stream = new MemoryStream();
            await blob.DownloadToAsync(stream);
            stream.Position = 0;

            logger.Information("Blob {BlobName} downloaded from container {ContainerName}.", blobName, containerName);
            return stream;
        }

        /// <inheritdoc/>
        public async Task<BLOB_STATUSCODE> DeleteBlobAsync(string containerName, string blobName)
        {
            BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
            BlobClient blob = container.GetBlobClient(blobName);

            bool result = await blob.DeleteIfExistsAsync();

            if (result)
                logger.Information("Successfully deleted blob {BlobName} in container {ContainerName}.", blobName, containerName);
            else
                logger.Information("Attempted to delte non-existent blob {BlobName} in container {ContainerName}.", blobName, containerName);

            return result ? BLOB_STATUSCODE.OK : BLOB_STATUSCODE.NOTFOUND;
        }

        /// <inheritdoc/>
        public async Task<BLOB_STATUSCODE> BlobExistsAsync(string containerName, string blobName)
        {
            BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
            BlobClient blob = container.GetBlobClient(blobName);

            bool result = await blob.ExistsAsync();

            if (result)
                logger.Information("Blob {BlobName} exists in container {ContainerName}.", blobName, containerName);
            else
                logger.Information("Blob {BlobName} does not exist in container {ContainerName}.", blobName, containerName);

            return result ? BLOB_STATUSCODE.OK : BLOB_STATUSCODE.NOTFOUND;
        }

        /// <inheritdoc/>
        public async Task<BLOB_STATUSCODE> CopyBlobAsync(string currentContainerName, string currentFileName, string newContainerName, string newFileName, bool overwrite = false, bool surpressLogging = false)
        {
            BlobContainerClient currentContainer = blobService.GetBlobContainerClient(currentContainerName);
            BlobClient currentBlob = currentContainer.GetBlobClient(currentFileName);

            if (!await currentBlob.ExistsAsync())
            {
                logger.Information("Old blob {OldFileName} not found in container {OldContainerName}", currentFileName, currentContainerName);
                return BLOB_STATUSCODE.NOTFOUND;
            }

            // Copy file extension if newFileName does not have it
            if (Path.GetExtension(newFileName) == null)
                newFileName += Path.GetExtension(currentFileName);

            BlobContainerClient newContainer = blobService.GetBlobContainerClient(newContainerName);
            BlobClient newBlob = newContainer.GetBlobClient(newFileName);

            bool exists = await newBlob.ExistsAsync();

            if (exists && !overwrite)
            {
                logger.Information("The destination already contains a file with the given name and overwrite is not enabled.");
                return BLOB_STATUSCODE.ALREADYEXISTS;
            }

            if (exists && overwrite)
                await newBlob.DeleteAsync();

            CopyFromUriOperation copyOperation = await newBlob.StartCopyFromUriAsync(currentBlob.Uri);
            await copyOperation.WaitForCompletionAsync();

            if (!await newBlob.ExistsAsync())
            {
                logger.Error("Copy operation completed, but destination blob {NewFileName} does not exist in destination container {NewContainerName}", newFileName, newContainerName);
                return BLOB_STATUSCODE.FAILED;
            }

            if (!surpressLogging)
                logger.Information("Successfully copied file {CurrentFileName} from container {CurrentContainerName} to {NewFileName} in container {NewContainerName}", currentFileName, currentContainerName, newContainerName);

            return BLOB_STATUSCODE.OK;
        }

        /// <inheritdoc/>
        public async Task<BLOB_STATUSCODE> MoveBlobAsync(string currentContainerName, string currentFileName, string newContainerName, string newFileName, bool overwrite = false, bool surpressLogging = false)
        {
            BLOB_STATUSCODE result = await CopyBlobAsync(currentContainerName, currentFileName, newContainerName, newFileName, overwrite, surpressLogging);

            if (result == BLOB_STATUSCODE.OK)
            {
                BlobContainerClient container = blobService.GetBlobContainerClient(currentContainerName);
                BlobClient blob = container.GetBlobClient(currentFileName);

                await blob.DeleteAsync();


                if (!surpressLogging)
                    logger.Information("Successfully moved file {CurrentFileName} from container {CurrentContainerName} to {NewFileName} in container {NewContainerName}", currentFileName, currentContainerName, newContainerName);
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<BLOB_STATUSCODE> RenameBlobAsync(string containerName, string oldFileName, string newFileName)
        {
            return await MoveBlobAsync(containerName, oldFileName, containerName, newFileName, surpressLogging: true);
        }

        /// <inheritdoc/>
        public async Task<string[]?> ListBlobsAsync(string containerName, string? prefix = null)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                logger.Error("Containername cannot be null or empty.");
                return null;
            }

            BlobContainerClient container = blobService.GetBlobContainerClient(containerName);

            if (!await container.ExistsAsync())
            {
                logger.Warning("Container {ContainerName} does not exist.", containerName);
                return null;
            }

            List<string> results = new List<string>();

            await foreach (BlobItem blob in container.GetBlobsAsync(prefix: prefix))
            {
                results.Add(blob.Name);
            }

            logger.Information("Listed {Count} blobs in container {ContainerName}{PrefixInfo}.", results.Count, containerName, prefix != null ? $" with prefix {prefix}" : "");
            return results.ToArray();
        }

        /// <inheritdoc/>
        public async Task<BlobPageResponse> ListBlobsPagedAsync(string containerName, int pageSize, string? continuationToken = null, string prefix = "")
        {
            if (string.IsNullOrEmpty(containerName))
            {
                logger.Error("Containername cannot be null or empty");
                return new BlobPageResponse(BLOB_STATUSCODE.INVALID, "Container name cannot be null or empty.", null, Array.Empty<string>());
            }

            if (pageSize < 1)
            {
                logger.Error("Pagesize cannot be smaller then 1.");
                return new BlobPageResponse(BLOB_STATUSCODE.INVALID, "Page size cannot be smaller than 1.", null, Array.Empty<string>());
            }

            BlobContainerClient container = blobService.GetBlobContainerClient(containerName);

            if (!await container.ExistsAsync())
            {
                logger.Warning("Container {ContainerName} does not exist.", containerName);
                return new BlobPageResponse(BLOB_STATUSCODE.NOTFOUND, $"The container {containerName} does not exist.", null, Array.Empty<string>());
            }

            Page<BlobItem>? page = await container.GetBlobsAsync(prefix: prefix).AsPages(continuationToken, pageSize).FirstOrDefaultAsync();

            if (page == null)
            {
                logger.Information("Page was empty.");
                return new BlobPageResponse(BLOB_STATUSCODE.OK, "The page was empty.", null, Array.Empty<string>());
            }

            List<string> blobs = new List<string>();

            foreach (BlobItem blob in page.Values)
            {
                blobs.Add(blob.Name);
            }

            logger.Information("Listed {Count} blobs in container {ContainerName}{PrefixInfo}", blobs.Count, containerName, prefix != null ? $" with prefix {prefix}" : "");
            return new BlobPageResponse(BLOB_STATUSCODE.OK, $"Listed {blobs.Count} blobs.", page.ContinuationToken, blobs.ToArray());
        }
    }
}
