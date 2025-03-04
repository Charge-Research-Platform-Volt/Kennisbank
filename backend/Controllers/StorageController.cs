using Microsoft.AspNetCore.Mvc;
using backend.Data;
using Microsoft.AspNetCore.StaticFiles;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using backend.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using System.Text;

namespace backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class StorageController : ControllerBase
    {
        private readonly IAzureBlobService blobService;
        private readonly Serilog.ILogger logger;
        private readonly DatabaseContext database;

        public StorageController(IAzureBlobService blobService, DatabaseContext databaseContext)
        {
            this.blobService = blobService;
            this.logger = Log.ForContext<StorageController>();
            this.database = databaseContext;
        }

        [HttpPut("upload")]
        [SwaggerOperation(
            Summary = "Upload a file to storage.",
            Description = "Uploads a file to Azure Blob Storage and returns metadata."
        )]
        [SwaggerResponse(200, "File was uploaded successfully", typeof(StorageResponse))]
        [SwaggerResponse(404, "Container does not exist", typeof(StorageResponse))]
        [SwaggerResponse(409, "File already exists", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid file", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> UploadFile([FromForm] StorageUploadDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new StorageResponse("No file was uploaded."));

            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new StorageResponse("No name was provided."));

            if (string.IsNullOrEmpty(dto.Description))
                return BadRequest(new StorageResponse("No description was provided."));

            string extension = Path.GetExtension(dto.File.FileName);

            if (!Filetype.Supported(extension))
                return BadRequest(new StorageResponse("Filetype is not supported."));

            string fileType = Filetype.ConvertExtensionToFiletype(extension);

            Guid id = Guid.NewGuid();

            try
            {
                BLOB_STATUSCODE result = await blobService.UploadBlobAsync(fileType, id.ToString(), extension, dto.File.OpenReadStream(), dto.Overwrite);

                switch (result)
                {
                    case BLOB_STATUSCODE.OK:

                        FileItem drive = new()
                        {
                            Id = id,
                            Name = dto.Name,
                            Description = dto.Description,
                            FileType = fileType,
                        };

                        await database.Files.AddAsync(drive);
                        await database.SaveChangesAsync();

                        return Ok(new FileUploadResult(id.ToString(), fileType, dto.File.Length));

                    case BLOB_STATUSCODE.NOTFOUND:
                        return NotFound(new ContainerResponse("Container could not be found", fileType));

                    case BLOB_STATUSCODE.ALREADYEXISTS:
                        return Conflict(new FileResponse("File already exists and overwrite is disabled.", id.ToString(), fileType));

                    default:
                        return StatusCode(500, "Error uploading file.");
                }
                    
            }
            catch (Exception e)
            {
                logger.Error(e, "Error uploading file {FileName}.", dto.File.FileName);
                return StatusCode(500, "Error uploading file.");
            }
        }

        [HttpGet("download/{fileType}/{id}")]
        [SwaggerOperation(
            Summary = "Download a file from storage.",
            Description = "Downloads a given blob from the given container in the Azure Blob Storage."
        )]
        [SwaggerResponse(200, "File found and returned")]
        [SwaggerResponse(404, "Invalid file or container name", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid location.", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> DownloadFile(string fileType, string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new StorageResponse("Invalid id."));

            if (string.IsNullOrEmpty(fileType))
                return BadRequest(new StorageResponse("Invalid filetype."));

            try
            {
                BlobDownloadResponse? maybeResponse = await blobService.DownloadBlobAsync(fileType, id);

                if (maybeResponse == null)
                    return NotFound(new FileResponse("File could not be found", id, fileType));

                BlobDownloadResponse response = (BlobDownloadResponse)maybeResponse;

                FileItem? item = await database.Files.FindAsync(Guid.Parse(id));

                if (item == null)
                    return NotFound(new StorageResponse("Id not found in the database."));

                string contentType = "application/octet-stream";
                string fileName = sanitizeFileName(item.Name) + response.Metadata["extension"];

                if (Path.HasExtension(fileName))
                {
                    FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
                    if (provider.TryGetContentType(fileName, out string? type) && !string.IsNullOrEmpty(type))
                        contentType = type;
                }

                return File(response.FileStream, contentType, fileName);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error downloading file {FileName} from container {ContainerName}.", id, fileType);
                return StatusCode(500, "Error downloading file.");
            }
        }

        [HttpDelete("delete/{fileType}/{id}")]
        [SwaggerOperation(
            Summary = "Delete a file from storage.",
            Description = "Deletes the given file from the given container in the Azure Blob Storage."
        )]
        [SwaggerResponse(200, "File deleted successfully.", typeof(StorageResponse))]
        [SwaggerResponse(404, "File not found.", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid filename", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> DeleteFile(string fileType, string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new StorageResponse("Invalid id."));

            if (string.IsNullOrEmpty(fileType))
                return BadRequest(new StorageResponse("Invalid filetype."));

            try
            {
                BLOB_STATUSCODE result = await blobService.DeleteBlobAsync(fileType, id);

                switch (result)
                {
                    case BLOB_STATUSCODE.OK:

                        FileItem? item = await database.Files.FindAsync(Guid.Parse(id));

                        if (item == null)
                            return NotFound(new StorageResponse("ID was not found in database. File was deleted succesfully."));

                        database.Files.Remove(item);
                        await database.SaveChangesAsync();

                        return Ok(new FileResponse("File deleted successfully", id, fileType));

                    case BLOB_STATUSCODE.NOTFOUND:
                        return NotFound(new FileResponse("File not found", id, fileType));

                    default:
                        return StatusCode(500, "Error while deleting file.");
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while deleting file {FileName} from container {ContainerName}.", id, fileType);
                return StatusCode(500, "Error while deleting file.");
            }
        }

        [HttpPatch("rename")]
        [SwaggerOperation(
            Summary = "Renames a file in storage.",
            Description = "Rename the given file to the new location in the Azure Blob Storage. The destination file name will be made unique by this function."
        )]
        [SwaggerResponse(200, "File renamed successfully", typeof(StorageResponse))]
        [SwaggerResponse(404, "File not found", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid name or ID", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> Rename([FromBody] StorageRenameDto dto)
        {
            if (string.IsNullOrEmpty(dto.Id))
                return BadRequest(new StorageResponse("Invalid ID."));

            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new StorageResponse("Invalid name."));

            try
            {
                FileItem? item = await database.Files.FindAsync(Guid.Parse(dto.Id));

                if (item == null)
                    return NotFound(new StorageResponse("ID was not found in database."));

                item.Name = dto.Name;

                await database.SaveChangesAsync();

                return Ok(new StorageResponse("File renamed succesfully."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while renaming file with ID {Id}.", dto.Id);
                return StatusCode(500, "Error while renaming file.");
            }
        }

        /*

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

        */

        [HttpGet("exists")]
        [SwaggerOperation(
            Summary = "Check if a file exists.",
            Description = "Checks if the given file exists in the given container."
        )]
        [SwaggerResponse(200, "Response with boolean indicating if file exists.", typeof(bool))]
        [SwaggerResponse(400, "Invalid filename", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error")]
        public async Task<IActionResult> Exists(string fileName, string containerName)
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
        public async Task<IActionResult> Page(string containerName, int pageSize = 1, string? continuationToken = null, string prefix = "")
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

        private string sanitizeFileName(string fileName, bool preserveSpaces = true)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            string extension = Path.GetExtension(fileName);
            fileName = Path.GetFileNameWithoutExtension(fileName);

            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder();

            foreach (char c in fileName)
            {
                if (c == ' ' && preserveSpaces)
                    sb.Append(c);

                else if (!invalidChars.Contains(c))
                    sb.Append(c);

                else
                    sb.Append('_');
            }

            string result = sb.ToString().Trim();

            if (result.StartsWith('.'))
                result = "_" + result.TrimStart('.');

            if (string.IsNullOrEmpty(result))
                return "unnamed" + extension;

            int maxLength = 255 - extension.Length;
            if (result.Length > maxLength)
                result = result.Substring(0, maxLength);

            return result + extension;
        }
    }
}
