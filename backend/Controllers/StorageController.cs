using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using System.Text.Json;
using KnowledgeBank.BackgroundServices;
using KnowledgeBank.Services;

namespace KnowledgeBank.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class StorageController(IAzureBlobService blobService, ResourceManager resourceManager, IBackgroundTaskQueue taskQueue, ITextExtractionService textExtractionService) : ControllerBase
    {
        private readonly IAzureBlobService blobService = blobService;
        private readonly Serilog.ILogger logger = Log.ForContext<StorageController>();
        private readonly ResourceManager resourceManager = resourceManager;
        private readonly IBackgroundTaskQueue _taskQueue = taskQueue;
        private readonly ITextExtractionService _textExtractionService = textExtractionService;


        [HttpPut("upload")]
        [SwaggerOperation(
            Summary = "Upload a file to storage.",
            Description = "Uploads a file to Azure Blob Storage and returns metadata."
        )]
        [SwaggerResponse(200, "File was uploaded successfully", typeof(StorageResponse))]
        [SwaggerResponse(404, "Container does not exist", typeof(StorageResponse))]
        [SwaggerResponse(409, "File already exists", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid file", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error", typeof(StorageResponse))]
        public async Task<IActionResult> UploadFile([FromForm] FileResourceCreateDto dto)
        {
            if (dto.File == null)
                return BadRequest(new StorageResponse("No file was uploaded."));

            if (dto.File.Length == 0)
                return BadRequest(new StorageResponse("The uploaded file was empty."));

            if (string.IsNullOrEmpty(dto.Title))
                return BadRequest(new StorageResponse("No name was provided."));

            if (string.IsNullOrEmpty(dto.TypeId))
                return BadRequest(new StorageResponse("No type ID was provided."));

            if (string.IsNullOrEmpty(dto.LanguageCode))
                return BadRequest(new StorageResponse("No language code was provided"));

            if (dto.PublicationDate == DateTime.MinValue)
                return BadRequest(new StorageResponse("No publication date was provided"));

            string extension = Path.GetExtension(dto.File.FileName);

            if (!Filetype.Supported(extension))
                return BadRequest(new StorageResponse("Filetype is not supported."));

            string fileType = Filetype.ConvertExtensionToFiletype(extension);

            await resourceManager.BeginTransaction();

            Guid id = await resourceManager.CreateResourceAsync(dto);

            try
            {
                logger.Information("Adding file '{FileName}' to blob storage...", dto.File.FileName);
                Dictionary<string, string> metadata = new() { { "extension", extension } };
                BLOB_STATUSCODE result = await blobService.UploadBlobAsync(fileType, id.ToString(), metadata, dto.File.OpenReadStream());

                switch (result)
                {
                    case BLOB_STATUSCODE.OK:

                        logger.Information("Adding file '{FileName}' to database...", dto.File.FileName);

                        // Save changes to database since file upload succeeded
                        await resourceManager.Commit();

                        logger.Information("File '{FileName}' added successfully", dto.File.FileName);

                        // If the file is a PDF, queue it for text extraction (This will not block the request and will be done in the background)
                        if (fileType.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            logger.Information("Queueing PDF {Id} for text extraction", id);

                            _taskQueue.QueueBackgroundWorkItem(async token =>
                            {
                                await _textExtractionService.ProcessDocumentAsync(id.ToString(), fileType);
                            });
                        }


                        return Ok(new FileUploadResult(id.ToString(), fileType, dto.File.Length));

                    case BLOB_STATUSCODE.NOTFOUND:
                        // Roll back changes to database since file upload failed
                        await resourceManager.Rollback();
                        return NotFound(new ContainerResponse("Container could not be found", fileType));

                    case BLOB_STATUSCODE.ALREADYEXISTS:
                        // Roll back changes to database since file upload failed
                        await resourceManager.Rollback();
                        return Conflict(new FileResponse("File already exists and overwrite is disabled.", id.ToString(), fileType));

                    default:
                        // Roll back changes to database since file upload failed
                        await resourceManager.Rollback();
                        return StatusCode(500, new StorageResponse("Error uploading file."));
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error uploading file {FileName}.", dto.File.FileName);
                return StatusCode(500, new StorageResponse("Error uploading file."));
            }
        }


        // private async Task ExtractFileContentAsync()
        // {
        //     logger.Information("Extracting file content...");
        //     await Task.Delay(120000);
        //     logger.Information("File content extracted successfully.");
        // }

        [HttpGet("download/{id}")]
        [SwaggerOperation(Summary = "Download a file from storage.", Description = "Downloads a given blob from the given container in the Azure Blob Storage.")]
        [SwaggerResponse(200, "File found and returned")]
        [SwaggerResponse(404, "File not found", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid location.", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error", typeof(StorageResponse))]
        public async Task<IActionResult> DownloadFile(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new StorageResponse("Invalid ID."));

            try
            {
                if (!await resourceManager.ResourceExistsAsync(id))
                    return NotFound(new StorageResponse("ID not found in the database."));

                string filetype = await resourceManager.GetResourcePropertyAsync(id, resource => resource.FileType);

                BlobDownloadResponse? maybeResponse = await blobService.DownloadBlobAsync(filetype, id);

                if (maybeResponse == null)
                    return NotFound(new FileResponse("File could not be found but exists in database.", id, filetype));

                BlobDownloadResponse response = (BlobDownloadResponse)maybeResponse;

                string contentType = "application/octet-stream";
                string fileName = SanitizeFileName(await resourceManager.GetResourcePropertyAsync(id, resource => resource.Title)) + response.Metadata["extension"];

                if (Path.HasExtension(fileName))
                {
                    FileExtensionContentTypeProvider provider = new();
                    if (provider.TryGetContentType(fileName, out string? type) && !string.IsNullOrEmpty(type))
                        contentType = type;
                }

                return File(response.FileStream, contentType, fileName);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error downloading file with ID {Id}.", id);
                return StatusCode(500, new StorageResponse("Error downloading file."));
            }
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(
            Summary = "Delete a file from storage.",
            Description = "Deletes the given file from the given container in the Azure Blob Storage."
        )]
        [SwaggerResponse(200, "File deleted successfully.", typeof(StorageResponse))]
        [SwaggerResponse(404, "File not found.", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid filename", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error", typeof(StorageResponse))]
        public async Task<IActionResult> DeleteFile(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new StorageResponse("Invalid ID."));

            try
            {
                if (await resourceManager.ResourceExistsAsync(id))
                    return NotFound(new StorageResponse("ID was not found in database. File was deleted succesfully."));

                string filetype = await resourceManager.GetResourcePropertyAsync(id, resource => resource.FileType);

                BLOB_STATUSCODE result = await blobService.DeleteBlobAsync(filetype, id);

                switch (result)
                {
                    case BLOB_STATUSCODE.OK:

                        await resourceManager.DeleteResourceAsync(id);

                        return Ok(new FileResponse("File deleted successfully", id, filetype));

                    case BLOB_STATUSCODE.NOTFOUND:
                        return NotFound(new FileResponse("File not found but exists in database.", id, filetype));

                    default:
                        return StatusCode(500, new StorageResponse("Error while deleting file."));
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while deleting file with ID {Id}", id);
                return StatusCode(500, new StorageResponse("Error while deleting file."));
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
        [SwaggerResponse(500, "Server error", typeof(StorageResponse))]
        public async Task<IActionResult> Rename([FromBody] ResourceRenameDto dto)
        {
            if (string.IsNullOrEmpty(dto.Id))
                return BadRequest(new StorageResponse("Invalid ID."));

            if (string.IsNullOrEmpty(dto.Title))
                return BadRequest(new StorageResponse("Invalid name."));

            try
            {
                if (!await resourceManager.UpdateResourceAsync(dto.Id, r => r.Title, dto.Title))
                    return NotFound(new StorageResponse("ID was not found in database."));

                return Ok(new StorageResponse("File renamed succesfully."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while renaming file with ID {Id}.", dto.Id);
                return StatusCode(500, new StorageResponse("Error while renaming file."));
            }
        }

        [EnableCors("AllowFrontend")]
        [HttpGet("exists/{hash}")]
        [SwaggerOperation(
            Summary = "Check if a file exists.",
            Description = "Checks if the given file exists based on its hash."
        )]
        [SwaggerResponse(200, "Response with boolean indicating if file exists.", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid filename", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error", typeof(StorageResponse))]
        public async Task<IActionResult> Exists(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return BadRequest(new StorageResponse("Invalid hash."));

            try
            {
                Guid resourceId = await resourceManager.GetResourcePropertyOrDefaultAsync(predicate: r => r.Hash == hash, selector: r => r.Id);

                if (resourceId == Guid.Empty)
                    return Ok(new ExistsResponse("File does not exist.", false, ""));

                return Ok(new ExistsResponse("File already exists", true, ((Guid)resourceId).ToString()));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error checking if file with hash {Hash} exists.", hash);
                return StatusCode(500, new StorageResponse("Error while checking if file exists."));
            }
        }

        [HttpGet("file/{id}")]
        [SwaggerOperation(
            Summary = "Get information of file.",
            Description = "Retrieve the database information of the given file."
        )]
        [SwaggerResponse(200, "File information.", typeof(StorageResponse))]
        [SwaggerResponse(404, "File not found.", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid ID.", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error.", typeof(StorageResponse))]
        public async Task<IActionResult> FileInfo(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new StorageResponse("Invalid ID."));

            try
            {
                Resource? item = await resourceManager.GetResourceAsync(id);

                if (item == null)
                    return NotFound(new StorageResponse("File not found."));

                return Ok(new ResourceInfoResponse("File found.", item));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error retrieving info of file with ID {Id}.", id);
                return StatusCode(500, new StorageResponse("Error retrieving file info."));
            }
        }

        [HttpGet("list-all")]
        [SwaggerOperation(
            Summary = "List all files in storage.",
            Description = "Lists all files in the storage, sorted by creation date (newest first)."
        )]
        [SwaggerResponse(200, "A list of all files in the storage", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error", typeof(StorageResponse))]
        public async Task<IActionResult> ListAll()
        {
            try
            {
                Resource[]? items = await resourceManager.GetAllResourcesAsync(includeProperties: ["ResourceTagRelations.Tag"]);

                if (items == null)
                    return Ok(new PageResponse("No files in database.", 0, 0, Array.Empty<Resource>()));

                object[] dtos = new object[items.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    dtos[i] = DtoGenerator.ToDto(items[i], includeProperties: ["ResourceTagRelations.Tag"]);
                }

                return Ok(new PageResponse($"{items.Length} files found.", 0, 0, dtos));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error listing all files.");
                return StatusCode(500, new StorageResponse("Error listing all files."));
            }
        }

        [HttpGet("list-paged")]
        [SwaggerOperation(
            Summary = "Lists files paged.",
            Description = "Lists files on a certain page of certain size."
        )]
        [SwaggerResponse(200, "A specified page of files of a specified size.", typeof(StorageResponse))]
        [SwaggerResponse(400, "Invalid page index or page size", typeof(StorageResponse))]
        [SwaggerResponse(500, "Server error", typeof(StorageResponse))]
        public async Task<IActionResult> ListPaged(int pageIndex = 1, int pageSize = 100)
        {
            if (pageIndex < 1)
                return BadRequest(new StorageResponse("Page index cannot be lower than 1."));

            if (pageSize < 1)
                return BadRequest(new StorageResponse("Page size cannot be lower than 1."));

            try
            {
                Resource[]? items = await resourceManager.GetResourcePageAsync(pageIndex, pageSize);

                if (items == null)
                    return Ok(new PageResponse("No files on this page.", pageIndex, pageSize, Array.Empty<Resource>()));

                return Ok(new PageResponse($"{items.Length} files found.", pageIndex, pageSize, items));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error listing files on page {PageIndex} of size {PageSize}.", pageIndex, pageSize);
                return StatusCode(500, new StorageResponse("Error listing files."));
            }
        }

        // Makes a valid filename
        private static string SanitizeFileName(string fileName, bool preserveSpaces = true)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            // Get extension
            string extension = Path.GetExtension(fileName);
            fileName = Path.GetFileNameWithoutExtension(fileName);

            // Retrieve invalid chars
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder sb = new();

            foreach (char c in fileName)
            {
                // If space and preserve spaces is on, append
                if (c == ' ' && preserveSpaces)
                    sb.Append(c);

                // If space and preserve spaces is not on, put underscore
                else if (c == ' ' && !preserveSpaces)
                    sb.Append('_');

                // If char is valid, append
                else if (!invalidChars.Contains(c))
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

            // If resulting string is empty put in unnamed name and return with extension
            if (string.IsNullOrEmpty(result))
                return "unnamed" + extension;

            // Limit length of name
            int maxLength = 255 - extension.Length;
            if (result.Length > maxLength)
                result = result[..maxLength];

            // Return result + extension
            return result + extension;
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


