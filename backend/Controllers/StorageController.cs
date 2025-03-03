using Microsoft.AspNetCore.Mvc;
using backend.Data;
using Microsoft.AspNetCore.StaticFiles;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;

namespace backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class StorageController : ControllerBase
    {
        private readonly IAzureBlobService blobService;
        private readonly Serilog.ILogger logger;

        public StorageController(IAzureBlobService blobService)
        {
            this.blobService = blobService;
            this.logger = Log.ForContext<StorageController>();
        }

        [HttpPost("upload")]
        [SwaggerOperation(
            Summary = "Upload a file to storage.",
            Description = "Uploads a file to Azure Blob Storage and returns metadata."
        )]
        [SwaggerResponse(200, "File was uploaded successfully", typeof(FileUploadResult))]
        [SwaggerResponse(400, "Invalid file")]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> UploadFile(IFormFile file, string containerName = "knowledgebank", bool overwrite = false)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            try
            {
                string uniqueBlobName = generateUniqueBlobName(file.FileName);

                BLOBRESPONSE result = await blobService.UploadBlobAsync(containerName, uniqueBlobName, file.OpenReadStream(), overwrite);

                switch (result)
                {
                    case BLOBRESPONSE.OK:
                        return Ok(new FileUploadResult(uniqueBlobName, containerName, file.Length));

                    case BLOBRESPONSE.ALREADYEXISTS:
                        return Conflict("File already exists and overwrite is disabled.");

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
        [SwaggerResponse(404, "Invalid file or container name", typeof(FileResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> DownloadFile(string fileName, string containerName = "knowledgebank")
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(containerName))
                return BadRequest(new FileResponse("Invalid file or container name", fileName, containerName));

            try
            {
                Stream? stream = await blobService.DownloadBlobAsync(containerName, fileName);

                if (stream == null)
                    return NotFound();

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

        [HttpGet("delete/{fileName}")]
        [SwaggerOperation(
            Summary = "Delete a file from storage.",
            Description = "Deletes the given file from the given container in the Azure Blob Storage."
        )]
        [SwaggerResponse(200, "File deleted successfully.", typeof(FileResponse))]
        [SwaggerResponse(404, "File not found.", typeof(FileResponse))]
        [SwaggerResponse(400, "Invalid filename", typeof(FileResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> DeleteFile(string fileName, string containerName = "knowledgebank")
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(containerName))
                return BadRequest(new FileResponse("Invalid file or container name", fileName, containerName));

            try
            {
                BLOBRESPONSE result = await blobService.DeleteBlobAsync(containerName, fileName);

                switch (result)
                {
                    case BLOBRESPONSE.OK:
                        return Ok(new FileResponse("File deleted successfully", fileName, containerName));

                    case BLOBRESPONSE.NOTFOUND:
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

        [HttpGet("copy")]
        [SwaggerOperation(
            Summary = "Copies a file in storage.",
            Description = "Copies the given file to the new location in the Azure Blob Storage. The destination file name will be made unique by this function."
        )]
        [SwaggerResponse(200, "File copied successfully", typeof(FileResponse))]
        [SwaggerResponse(404, "File not found", typeof(FileResponse))]
        [SwaggerResponse(409, "File already exists", typeof(FileResponse))]
        [SwaggerResponse(400, "Invalid file/container names", typeof(FileResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> CopyFile(string currentFileName, string destinationFileName, string currentContainerName = "knowledgebank", string destinationContainerName = "knowledgebank", bool overwrite = false)
        {
            if (string.IsNullOrEmpty(currentContainerName) || string.IsNullOrEmpty(currentFileName))
                return BadRequest(new FileResponse("Invalid source file or container name", currentFileName, currentContainerName));

            if (string.IsNullOrEmpty(destinationContainerName) || string.IsNullOrEmpty(destinationFileName))
                return BadRequest(new FileResponse("Invalid destination file or container name", destinationFileName, destinationContainerName));

            try
            {
                string uniqueBlobName = generateUniqueBlobName(destinationFileName);

                BLOBRESPONSE result = await blobService.CopyBlobAsync(currentContainerName, currentFileName, destinationContainerName, uniqueBlobName, overwrite);

                switch (result)
                {
                    case BLOBRESPONSE.OK:
                        return Ok(new FileResponse("File copied successfully", uniqueBlobName, destinationContainerName));

                    case BLOBRESPONSE.ALREADYEXISTS:
                        return Conflict(new FileResponse("File already exists and overwrite is not enabled", uniqueBlobName, destinationContainerName));

                    case BLOBRESPONSE.NOTFOUND:
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

        [HttpGet("move")]
        [SwaggerOperation(
            Summary = "Moves a file in storage.",
            Description = "Moves the given file to the new location in the Azure Blob Storage. The destination file name will be made unique by this function."
        )]
        [SwaggerResponse(200, "File moved successfully", typeof(FileResponse))]
        [SwaggerResponse(404, "File not found", typeof(FileResponse))]
        [SwaggerResponse(409, "File already exists", typeof(FileResponse))]
        [SwaggerResponse(400, "Invalid file/container names", typeof(FileResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> MoveFile(string currentFileName, string destinationFileName, string currentContainerName = "knowledgebank", string destinationContainerName = "knowledgebank",  bool overwrite = false)
        {
            if (string.IsNullOrEmpty(currentContainerName) || string.IsNullOrEmpty(currentFileName))
                return BadRequest(new FileResponse("Invalid source file or container name", currentFileName, currentContainerName));

            if (string.IsNullOrEmpty(destinationContainerName) || string.IsNullOrEmpty(destinationFileName))
                return BadRequest(new FileResponse("Invalid destination file or container name", destinationFileName, destinationContainerName));

            try
            {
                string uniqueBlobName = generateUniqueBlobName(destinationFileName);

                BLOBRESPONSE result = await blobService.MoveBlobAsync(currentContainerName, currentFileName, destinationContainerName, uniqueBlobName, overwrite);

                switch (result)
                {
                    case BLOBRESPONSE.OK:
                        return Ok(new FileResponse("File moved successfully", uniqueBlobName, destinationContainerName));

                    case BLOBRESPONSE.ALREADYEXISTS:
                        return Conflict(new FileResponse("File already exists and overwrite is not enabled", uniqueBlobName, destinationContainerName));

                    case BLOBRESPONSE.NOTFOUND:
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

        [HttpGet("rename")]
        [SwaggerOperation(
            Summary = "Renames a file in storage.",
            Description = "Rename the given file to the new location in the Azure Blob Storage. The destination file name will be made unique by this function."
        )]
        [SwaggerResponse(200, "File renamed successfully", typeof(FileResponse))]
        [SwaggerResponse(404, "File not found", typeof(FileResponse))]
        [SwaggerResponse(409, "File already exists", typeof(FileResponse))]
        [SwaggerResponse(400, "Invalid file/container names", typeof(FileResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> MoveFile(string currentFileName, string newFileName, string containerName = "knowledgebank")
        {
            if (string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(currentFileName))
                return BadRequest(new FileResponse("Invalid source file or container name", currentFileName, containerName));

            if (string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(newFileName))
                return BadRequest(new FileResponse("Invalid destination file or container name", newFileName, containerName));

            try
            {
                string uniqueBlobName = generateUniqueBlobName(newFileName);

                BLOBRESPONSE result = await blobService.RenameBlobAsync(containerName, currentFileName, uniqueBlobName);

                switch (result)
                {
                    case BLOBRESPONSE.OK:
                        return Ok(new FileResponse("File renamed successfully", uniqueBlobName, containerName));

                    case BLOBRESPONSE.ALREADYEXISTS:
                        return Conflict(new FileResponse("File already exists and overwrite is not enabled", uniqueBlobName, containerName));

                    case BLOBRESPONSE.NOTFOUND:
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

        [HttpGet("create-container/{containerName}")]
        [SwaggerOperation(
            Summary = "Create a container in storage.",
            Description = "Creates a container in the Azure Blob Storage."
        )]
        [SwaggerResponse(200, "Container was created successfully", typeof(ContainerResponse))]
        [SwaggerResponse(400, "Invalid container name", typeof(ContainerResponse))]
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

        [HttpGet("delete-container/{containerName}")]
        [SwaggerOperation(
            Summary = "Delete a container in storage.",
            Description = "Deletes a container in the Azure Blob Storage."
        )]
        [SwaggerResponse(200, "Container was deleted successfully", typeof(ContainerResponse))]
        [SwaggerResponse(400, "Invalid container name", typeof(ContainerResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> DeleteContainer(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
                return BadRequest(new ContainerResponse("Invalid container name", containerName));

            try
            {
                BLOBRESPONSE result = await blobService.DeleteContainerAsync(containerName);

                switch (result)
                {
                    case BLOBRESPONSE.OK:
                        return Ok(new ContainerResponse("Container was removed successfully", containerName));

                    case BLOBRESPONSE.NOTFOUND:
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
        [SwaggerResponse(400, "Invalid filename", typeof(FileResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> Exists(string fileName, string containerName = "knowledgebank")
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(containerName))
                return BadRequest(new FileResponse("Invalid file or container name", fileName, containerName));

            try
            {
                BLOBRESPONSE result = await blobService.BlobExistsAsync(containerName, fileName);
                
                switch (result)
                {
                    case BLOBRESPONSE.OK:
                        return Ok(true);

                    case BLOBRESPONSE.NOTFOUND:
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

        [HttpGet("list")]
        [SwaggerOperation(
            Summary = "Lists files.",
            Description = "Lists files in given container."
        )]
        [SwaggerResponse(200, "List of files", typeof(string[]))]
        [SwaggerResponse(400, "Invalid container name.", typeof(ContainerResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> List(string containerName = "knowledgebank", string? prefix = null)
        {
            if (string.IsNullOrEmpty(containerName))
                return BadRequest(new ContainerResponse("Invalid container name", containerName));

            try
            {
#pragma warning disable CS8604 // Possible null reference argument.
                return Ok(await blobService.ListBlobsAsync(containerName, prefix));
#pragma warning restore CS8604 // Possible null reference argument.
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
    }

    /// <summary>
    /// Result of a file upload operation
    /// </summary>
    public class FileUploadResult
    {
        /// <summary>
        /// Name of the uploaded file
        /// </summary>
        public string FileName { get; set; }

        public string ContainerName { get; set; }

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long Size { get; set; }

        public FileUploadResult(string fileName, string containerName, long size)
        {
            this.FileName = fileName;
            this.ContainerName = containerName;
            this.Size = size;
        }
    }

    public class FileResponse
    {
        public string Message { get; set; }

        public string FileName { get; set; }

        public string ContainerName { get; set; }

        public FileResponse(string message, string fileName, string containerName)
        {
            this.Message = message;
            this.FileName = fileName;
            this.ContainerName = containerName;
        }
    }

    public class ContainerResponse
    {
        public string Message { get; set; }

        public string ContainerName { get; set; }

        public ContainerResponse(string message, string containerName)
        {
            this.Message = message;
            this.ContainerName = containerName;
        }
    }
}
