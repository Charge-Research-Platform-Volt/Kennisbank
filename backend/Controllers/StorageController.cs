using Microsoft.AspNetCore.Mvc;
using backend.Data;
using Microsoft.AspNetCore.StaticFiles;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using backend.Responses;

namespace backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class StorageController : ControllerBase
    {
        private readonly IAzureBlobService blobService;
        private readonly Serilog.ILogger logger;

        private const string DEFAULT_CONTAINER_NAME = "archive";

        public StorageController(IAzureBlobService blobService)
        {
            this.blobService = blobService;
            this.logger = Log.ForContext<StorageController>();

            blobService.GetOrCreateContainerAsync(DEFAULT_CONTAINER_NAME);
        }

        [HttpPut("upload")]
        [SwaggerOperation(
            Summary = "Upload a file to storage.",
            Description = "Uploads a file to Azure Blob Storage and returns metadata."
        )]
        [SwaggerResponse(200, "File was uploaded successfully", typeof(StorageResponse))]
        [SwaggerResponse(404, "Container does not exist", typeof(StorageResponse))]
        [SwaggerResponse(409, "File already exists", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid file")]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> UploadFile(IFormFile file, string containerName = DEFAULT_CONTAINER_NAME, bool overwrite = false)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            try
            {
                string uniqueBlobName = generateUniqueBlobName(file.FileName);

                BLOB_STATUSCODE result = await blobService.UploadBlobAsync(containerName, uniqueBlobName, file.OpenReadStream(), overwrite);

                switch (result)
                {
                    case BLOB_STATUSCODE.OK:
                        return Ok(new FileUploadResult(uniqueBlobName, containerName, file.Length));

                    case BLOB_STATUSCODE.NOTFOUND:
                        return NotFound(new ContainerResponse("Container could not be found", containerName));

                    case BLOB_STATUSCODE.ALREADYEXISTS:
                        return Conflict(new FileResponse("File already exists and overwrite is disabled.", uniqueBlobName, containerName));

                    default:
                        return StatusCode(500, "Error uploading file.");
                }
                    
            }
            catch (Exception e)
            {
                logger.Error(e, "Error uploading file {FileName}.", file.FileName);
                return StatusCode(500, "Error uploading file.");
            }
        }

        [HttpGet("download/{fileName}")]
        [SwaggerOperation(
            Summary = "Download a file from storage.",
            Description = "Downloads a given blob from the given container in the Azure Blob Storage."
        )]
        [SwaggerResponse(200, "File found and returned")]
        [SwaggerResponse(404, "Invalid file or container name", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> DownloadFile(string fileName, string containerName = DEFAULT_CONTAINER_NAME)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(containerName))
                return BadRequest(new FileResponse("Invalid file or container name", fileName, containerName));

            try
            {
                Stream? stream = await blobService.DownloadBlobAsync(containerName, fileName);

                if (stream == null)
                    return NotFound(new FileResponse("File could not be found", fileName, containerName));

                string contentType = "application/octet-stream";

                if (Path.HasExtension(fileName))
                {
                    FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
                    if (provider.TryGetContentType(fileName, out string? type) && !string.IsNullOrEmpty(type))
                        contentType = type;
                }

                return File(stream, contentType);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error downloading file {FileName} from container {ContainerName}.", fileName, containerName);
                return StatusCode(500, "Error downloading file.");
            }
        }

        [HttpDelete("delete/{fileName}")]
        [SwaggerOperation(
            Summary = "Delete a file from storage.",
            Description = "Deletes the given file from the given container in the Azure Blob Storage."
        )]
        [SwaggerResponse(200, "File deleted successfully.", typeof(StorageResponse))]
        [SwaggerResponse(404, "File not found.", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid filename", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> DeleteFile(string fileName, string containerName = DEFAULT_CONTAINER_NAME)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(containerName))
                return BadRequest(new FileResponse("Invalid file or container name", fileName, containerName));

            try
            {
                BLOB_STATUSCODE result = await blobService.DeleteBlobAsync(containerName, fileName);

                switch (result)
                {
                    case BLOB_STATUSCODE.OK:
                        return Ok(new FileResponse("File deleted successfully", fileName, containerName));

                    case BLOB_STATUSCODE.NOTFOUND:
                        return NotFound(new FileResponse("File not found", fileName, containerName));

                    default:
                        return StatusCode(500, "Error while deleting file.");
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while deleting file {FileName} from container {ContainerName}.", fileName, containerName);
                return StatusCode(500, "Error while deleting file.");
            }
        }

        [HttpPost("copy")]
        [SwaggerOperation(
            Summary = "Copies a file in storage.",
            Description = "Copies the given file to the new location in the Azure Blob Storage. The destination file name will be made unique by this function."
        )]
        [SwaggerResponse(200, "File copied successfully", typeof(StorageResponse))]
        [SwaggerResponse(404, "File not found", typeof(StorageResponse))]
        [SwaggerResponse(409, "File already exists", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid file/container names", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> CopyFile(string currentFileName, string destinationFileName, string currentContainerName = DEFAULT_CONTAINER_NAME, string destinationContainerName = DEFAULT_CONTAINER_NAME, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(currentContainerName) || string.IsNullOrEmpty(currentFileName))
                return BadRequest(new FileResponse("Invalid source file or container name", currentFileName, currentContainerName));

            if (string.IsNullOrEmpty(destinationContainerName) || string.IsNullOrEmpty(destinationFileName))
                return BadRequest(new FileResponse("Invalid destination file or container name", destinationFileName, destinationContainerName));

            try
            {
                destinationFileName = copyFileExtensionWhenMissing(currentFileName, destinationFileName);
                string uniqueBlobName = generateUniqueBlobName(destinationFileName);

                BLOB_STATUSCODE result = await blobService.CopyBlobAsync(currentContainerName, currentFileName, destinationContainerName, uniqueBlobName, overwrite);

                switch (result)
                {
                    case BLOB_STATUSCODE.OK:
                        return Ok(new FileResponse("File copied successfully", uniqueBlobName, destinationContainerName));

                    case BLOB_STATUSCODE.ALREADYEXISTS:
                        return Conflict(new FileResponse("File already exists and overwrite is not enabled", uniqueBlobName, destinationContainerName));

                    case BLOB_STATUSCODE.NOTFOUND:
                        return NotFound(new FileResponse("File not found", currentFileName, currentContainerName));

                    default:
                        return StatusCode(500, "Server error");
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while copying file {CurrentFileName} from container {CurrentContainerName} to {DestinationFileName} in {DestinationContainerName}.", currentFileName, currentContainerName, destinationFileName, destinationFileName);
                return StatusCode(500, "Error while copying file.");
            }
        }

        [HttpPost("move")]
        [SwaggerOperation(
            Summary = "Moves a file in storage.",
            Description = "Moves the given file to the new location in the Azure Blob Storage. The destination file name will be made unique by this function."
        )]
        [SwaggerResponse(200, "File moved successfully", typeof(StorageResponse))]
        [SwaggerResponse(404, "File not found", typeof(StorageResponse))]
        [SwaggerResponse(409, "File already exists", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid file/container names", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> MoveFile(string currentFileName, string destinationFileName, string currentContainerName = DEFAULT_CONTAINER_NAME, string destinationContainerName = DEFAULT_CONTAINER_NAME,  bool overwrite = false)
        {
            if (string.IsNullOrEmpty(currentContainerName) || string.IsNullOrEmpty(currentFileName))
                return BadRequest(new FileResponse("Invalid source file or container name", currentFileName, currentContainerName));

            if (string.IsNullOrEmpty(destinationContainerName) || string.IsNullOrEmpty(destinationFileName))
                return BadRequest(new FileResponse("Invalid destination file or container name", destinationFileName, destinationContainerName));

            try
            {
                destinationFileName = copyFileExtensionWhenMissing(currentFileName, destinationFileName);
                string uniqueBlobName = generateUniqueBlobName(destinationFileName);

                BLOB_STATUSCODE result = await blobService.MoveBlobAsync(currentContainerName, currentFileName, destinationContainerName, uniqueBlobName, overwrite);

                switch (result)
                {
                    case BLOB_STATUSCODE.OK:
                        return Ok(new FileResponse("File moved successfully", uniqueBlobName, destinationContainerName));

                    case BLOB_STATUSCODE.ALREADYEXISTS:
                        return Conflict(new FileResponse("File already exists and overwrite is not enabled", uniqueBlobName, destinationContainerName));

                    case BLOB_STATUSCODE.NOTFOUND:
                        return NotFound(new FileResponse("File not found", currentFileName, currentContainerName));

                    default:
                        return StatusCode(500, "Server error");
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while moving file {CurrentFileName} from container {CurrentContainerName} to {DestinationFileName} in {DestinationContainerName}.", currentFileName, currentContainerName, destinationFileName, destinationFileName);
                return StatusCode(500, "Error while copying file.");
            }
        }

        [HttpPatch("rename")]
        [SwaggerOperation(
            Summary = "Renames a file in storage.",
            Description = "Rename the given file to the new location in the Azure Blob Storage. The destination file name will be made unique by this function."
        )]
        [SwaggerResponse(200, "File renamed successfully", typeof(StorageResponse))]
        [SwaggerResponse(404, "File not found", typeof(StorageResponse))]
        [SwaggerResponse(409, "File already exists", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid file/container names", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> Rename(string currentFileName, string newFileName, string containerName = DEFAULT_CONTAINER_NAME)
        {
            if (string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(currentFileName))
                return BadRequest(new FileResponse("Invalid source file or container name", currentFileName, containerName));

            if (string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(newFileName))
                return BadRequest(new FileResponse("Invalid destination file or container name", newFileName, containerName));

            try
            {
                newFileName = copyFileExtensionWhenMissing(currentFileName, newFileName);
                string uniqueBlobName = generateUniqueBlobName(newFileName);

                BLOB_STATUSCODE result = await blobService.RenameBlobAsync(containerName, currentFileName, uniqueBlobName);

                switch (result)
                {
                    case BLOB_STATUSCODE.OK:
                        return Ok(new FileResponse("File renamed successfully", uniqueBlobName, containerName));

                    case BLOB_STATUSCODE.ALREADYEXISTS:
                        return Conflict(new FileResponse("File already exists and overwrite is not enabled", uniqueBlobName, containerName));

                    case BLOB_STATUSCODE.NOTFOUND:
                        return NotFound(new FileResponse("File not found", currentFileName, containerName));

                    default:
                        return StatusCode(500, "Server error");
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while renaming file {CurrentFileName} from container {CurrentContainerName} to {DestinationFileName}.", currentFileName, containerName, newFileName);
                return StatusCode(500, "Error while copying file.");
            }
        }

        [HttpPut("create-container/{containerName}")]
        [SwaggerOperation(
            Summary = "Create a container in storage.",
            Description = "Creates a container in the Azure Blob Storage."
        )]
        [SwaggerResponse(200, "Container was created successfully", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid container name", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> CreateContainer(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
                return BadRequest(new ContainerResponse("Invalid container name", containerName));

            try
            {
                await blobService.GetOrCreateContainerAsync(containerName);

                return Ok(new ContainerResponse("Successfully created container", containerName));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error creating container {ContainerName}.", containerName);
                return StatusCode(500, "Error creating container.");
            }
        }

        [HttpDelete("delete-container/{containerName}")]
        [SwaggerOperation(
            Summary = "Delete a container in storage.",
            Description = "Deletes a container in the Azure Blob Storage."
        )]
        [SwaggerResponse(200, "Container was deleted successfully", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid container name", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> DeleteContainer(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
                return BadRequest(new ContainerResponse("Invalid container name", containerName));

            try
            {
                BLOB_STATUSCODE result = await blobService.DeleteContainerAsync(containerName);

                switch (result)
                {
                    case BLOB_STATUSCODE.OK:
                        return Ok(new ContainerResponse("Container was removed successfully", containerName));

                    case BLOB_STATUSCODE.NOTFOUND:
                        return NotFound(new ContainerResponse("The container does not exist", containerName));

                    default:
                        return StatusCode(500, "Error deleting container.");
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error deleting container {ContainerName}.", containerName);
                return StatusCode(500, "Error deleting container.");
            }
        }

        [HttpGet("exists")]
        [SwaggerOperation(
            Summary = "Check if a file exists.",
            Description = "Checks if the given file exists in the given container."
        )]
        [SwaggerResponse(200, "Response with boolean indicating if file exists.", typeof(bool))]
        [SwaggerResponse(400, "Invalid filename", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> Exists(string fileName, string containerName = DEFAULT_CONTAINER_NAME)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(containerName))
                return BadRequest(new FileResponse("Invalid file or container name", fileName, containerName));

            try
            {
                BLOB_STATUSCODE result = await blobService.BlobExistsAsync(containerName, fileName);
                
                switch (result)
                {
                    case BLOB_STATUSCODE.OK:
                        return Ok(true);

                    case BLOB_STATUSCODE.NOTFOUND:
                        return Ok(false);

                    default:
                        return StatusCode(500, "Error while checking if file exists.");
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error checking if file {FileName} in container {ContainerName} exists.", fileName, containerName);
                return StatusCode(500, "Error while checking if file exists.");
            }
        }

        [HttpGet("page")]
        [SwaggerOperation(
            Summary = "Lists a page of files",
            Description = "Lists a page of files in a given container."
        )]
        [SwaggerResponse(200, "List of files", typeof(StorageResponse))]
        [SwaggerResponse(404, "Container does not exist", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid input", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> Page(string containerName = DEFAULT_CONTAINER_NAME, int pageSize = 1, string? continuationToken = null, string prefix = "")
        {
            try
            {
                BlobPageResponse result = await blobService.ListBlobsPagedAsync(containerName, pageSize, continuationToken, prefix);

                PageResponse response = new PageResponse(result.Message, containerName, result.ContinuationToken, result.Blobs, pageSize);

                switch (result.Status)
                {
                    case BLOB_STATUSCODE.OK:
                        return Ok(response);

                    case BLOB_STATUSCODE.INVALID:
                        return BadRequest(response);

                    case BLOB_STATUSCODE.NOTFOUND:
                        return NotFound(response);

                    default:
                        return StatusCode(500, "Error listing files.");
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while listing files in container {ContainerName}.", containerName);
                return StatusCode(500, "Error listing files.");
            }
        }

        private string generateUniqueBlobName(string fileName)
        {
            return $"{Guid.NewGuid()}-{fileName}";
        }

        /// <summary>
        /// Copies the file extension of the source filename to the destination filename, when the destination filename does not have one
        /// </summary>
        /// <param name="source">Source file name</param>
        /// <param name="dest">Destination filename</param>
        /// <returns>Destination filename with file extension</returns>
        private string copyFileExtensionWhenMissing(string source, string dest)
        {
            // Copy file extension if newFileName does not have it
            if (string.IsNullOrEmpty(Path.GetExtension(dest)))
                dest += Path.GetExtension(source);

            return dest;
        }
    }
}
