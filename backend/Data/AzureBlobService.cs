using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text;

namespace backend.Data
{
    public enum BLOBRESPONSE { OK, FAILED, NOTFOUND, ALREADYEXISTS }

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
        Task<BLOBRESPONSE> DeleteContainerAsync(string containerName);

        /// <summary>
        /// Uploads a blob to the given container.
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="blobName">The name of the blob</param>
        /// <param name="content">The filestream</param>
        /// <param name="overwrite">Whether or not the file should be overwritten when it already exists</param>
        /// <returns>If the operation was successful</returns>
        Task<BLOBRESPONSE> UploadBlobAsync(string containerName, string blobName, Stream content, bool overwrite = false);

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
        Task<BLOBRESPONSE> DeleteBlobAsync(string containerName, string blobName);

        /// <summary>
        /// Checks if a blob exists in a given container
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="blobName">The name of the blob</param>
        /// <returns>If the blob exists in the given container</returns>
        Task<BLOBRESPONSE> BlobExistsAsync(string containerName, string blobName);


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
        Task<BLOBRESPONSE> CopyBlobAsync(string currentContainername, string currentFileName, string newContainerName, string newFileName, bool overwrite = false, bool surpressLogging = false);


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
        Task<BLOBRESPONSE> MoveBlobAsync(string currentContainername, string currentFileName, string newContainerName, string newFileName, bool overwrite = false, bool surpressLogging = false);

        /// <summary>
        /// Renames a blob in a given container
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="oldFileName">The old name of the blob</param>
        /// <param name="newFileName">The new name of the blob</param>
        /// <returns>If the operation was successful</returns>
        Task<BLOBRESPONSE> RenameBlobAsync(string containerName, string oldFileName, string newFileName);

        /// <summary>
        /// Lists all blobs in a given container
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="prefix">A prefix to filter results</param>
        /// <returns>A list of blobs found in the container</returns>
        Task<string[]> ListBlobsAsync(string containerName, string prefix = "");
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
            try
            {
                BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
                await container.CreateIfNotExistsAsync();

                logger.Information("Container {ContainerName} retrieved or created successfully", containerName);

                return container;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error creating container {ContainerName}", containerName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<BLOBRESPONSE> DeleteContainerAsync(string containerName)
        {
            try
            {
                BlobContainerClient container = blobService.GetBlobContainerClient(containerName);

                bool result = await container.DeleteIfExistsAsync();

                logger.Information("Container {ContainerName} deleted successfully.");

                return result ? BLOBRESPONSE.OK : BLOBRESPONSE.NOTFOUND;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error deleting container {ContainerName}.", containerName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<BLOBRESPONSE> UploadBlobAsync(string containerName, string blobName, Stream content, bool overwrite = false)
        {
            try
            {
                BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
                BlobClient blob = container.GetBlobClient(blobName);

                if (await blob.ExistsAsync() && !overwrite)
                {
                    logger.Information("Blob {BlobName} already exists in {ContainerName} and overwrite is disabled.", blobName, containerName);
                    return BLOBRESPONSE.ALREADYEXISTS;
                }

                await blob.UploadAsync(content, true);
                logger.Information("Blob {BlobName} successfully uploaded to container {ContainerName}.", blobName, containerName);
                return BLOBRESPONSE.OK;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error uploading blob {BlobName} to container {ContainerName}.", blobName, containerName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Stream?> DownloadBlobAsync(string containerName, string blobName)
        {
            try
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
            catch (Exception e)
            {
                logger.Error(e, "Error while downloading blob {BlobName} from container {ContainerName}.", blobName, containerName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<BLOBRESPONSE> DeleteBlobAsync(string containerName, string blobName)
        {
            try
            {
                BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
                BlobClient blob = container.GetBlobClient(blobName);

                bool result = await blob.DeleteIfExistsAsync();

                if (result)
                    logger.Information("Successfully deleted blob {BlobName} in container {ContainerName}.", blobName, containerName);
                else
                    logger.Information("Attempted to delte non-existent blob {BlobName} in container {ContainerName}.", blobName, containerName);

                return result ? BLOBRESPONSE.OK : BLOBRESPONSE.NOTFOUND;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while deleting blob {BlobName} from container {ContainerName}.", blobName, containerName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<BLOBRESPONSE> BlobExistsAsync(string containerName, string blobName)
        {
            try
            {
                BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
                BlobClient blob = container.GetBlobClient(blobName);

                bool result = await blob.ExistsAsync();

                if (result)
                    logger.Information("Blob {BlobName} exists in container {ContainerName}.", blobName, containerName);
                else
                    logger.Information("Blob {BlobName} does not exist in container {ContainerName}.", blobName, containerName);

                return result ? BLOBRESPONSE.OK : BLOBRESPONSE.NOTFOUND;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while checking if blob {BlobName} in container {ContainerName} exists.", blobName, containerName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<BLOBRESPONSE> CopyBlobAsync(string currentContainerName, string currentFileName, string newContainerName, string newFileName, bool overwrite = false, bool surpressLogging = false)
        {
            try
            {
                BlobContainerClient currentContainer = blobService.GetBlobContainerClient(currentContainerName);
                BlobClient currentBlob = currentContainer.GetBlobClient(currentFileName);

                if (!await currentBlob.ExistsAsync())
                {
                    logger.Information("Old blob {OldFileName} not found in container {OldContainerName}", currentFileName, currentContainerName);
                    return BLOBRESPONSE.NOTFOUND;
                }

                BlobContainerClient newContainer = blobService.GetBlobContainerClient(newContainerName);
                BlobClient newBlob = newContainer.GetBlobClient(newFileName);

                bool exists = await newBlob.ExistsAsync();

                if (exists && !overwrite)
                {
                    logger.Information("The destination already contains a file with the given name and overwrite is not enabled.");
                    return BLOBRESPONSE.ALREADYEXISTS;
                }

                if (exists && overwrite)
                    await newBlob.DeleteAsync();

                CopyFromUriOperation copyOperation = await newBlob.StartCopyFromUriAsync(currentBlob.Uri);
                await copyOperation.WaitForCompletionAsync();

                if (!await newBlob.ExistsAsync())
                {
                    logger.Error("Copy operation completed, but destination blob {NewFileName} does not exist in destination container {NewContainerName}", newFileName, newContainerName);
                    return BLOBRESPONSE.FAILED;
                }

                if (!surpressLogging)
                    logger.Information("Successfully copied file {CurrentFileName} from container {CurrentContainerName} to {NewFileName} in container {NewContainerName}", currentFileName, currentContainerName, newContainerName);
                
                return BLOBRESPONSE.OK;
            }
            catch (Exception e)
            {
                if (!surpressLogging)
                    logger.Error(e, "Error while copying file {CurrentFileName} in container {CurrentContainerName} to {NewFileName} in container {NewContainerName}", currentFileName, currentContainerName, newFileName, newContainerName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<BLOBRESPONSE> MoveBlobAsync(string currentContainerName, string currentFileName, string newContainerName, string newFileName, bool overwrite = false, bool surpressLogging = false)
        {
            try
            {
                BLOBRESPONSE result = await CopyBlobAsync(currentContainerName, currentFileName, newContainerName, newFileName, overwrite, surpressLogging);

                if (result == BLOBRESPONSE.OK)
                {
                    BlobContainerClient container = blobService.GetBlobContainerClient(currentContainerName);
                    BlobClient blob = container.GetBlobClient(currentFileName);

                    await blob.DeleteAsync();


                    if (!surpressLogging)
                        logger.Information("Successfully moved file {CurrentFileName} from container {CurrentContainerName} to {NewFileName} in container {NewContainerName}", currentFileName, currentContainerName, newContainerName);
                }

                return result;
            }
            catch (Exception e)
            {
                if (!surpressLogging)
                    logger.Error(e, "Error while moving file {CurrentFileName} in container {CurrentContainerName} to {NewFileName} in container {NewContainerName}", currentFileName, currentContainerName, newFileName, newContainerName);
                throw;

            }
        }

        /// <inheritdoc/>
        public async Task<BLOBRESPONSE> RenameBlobAsync(string containerName, string oldFileName, string newFileName)
        {
            try
            {
                return await MoveBlobAsync(containerName, oldFileName, containerName, newFileName, surpressLogging : true);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while renaming file {OldFileName} to {NewFileName} in container {ContainerName}", oldFileName, newFileName, containerName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string[]> ListBlobsAsync(string containerName, string? prefix = null)
        {
            try
            {
                BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
                List<string> results = new List<string>();

                await foreach (BlobItem blob in container.GetBlobsAsync(prefix: prefix))
                {
                    results.Add(blob.Name);
                }

                logger.Information("Listed {Count} blobs in container {ContainerName}{PrefixInfo}.", results.Count, containerName, prefix != null ? $" with prefix {prefix}" : "");
                return results.ToArray();
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while listing blobs in container {ContainerName}.", containerName);
                throw;
            }
        }

        // TODO: PAGING
    }
}
