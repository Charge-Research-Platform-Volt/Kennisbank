// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Utils;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using System.Text.Json;
using KnowledgeBank.BackgroundServices;
using KnowledgeBank.Services;
using System.Buffers.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using System.Linq.Expressions;
using static Qdrant.Client.Grpc.Conditions;
using Qdrant.Client.Grpc;



namespace KnowledgeBank.Controllers
{


    /// <summary>
    /// This controller is responsible for handling API calls to manage resources and their metadata.
    /// 
    /// Author: Abel Dieterich
    /// </summary>
    /// <param name="resourceManager">The resource manager service for database interactions</param>
    /// <param name="blobService">The Azure Blob Service for file storage</param>
    /// <param name="taskQueue">The background task queue for processing tasks asynchronously</param>
    /// <param name="ragSystem">The RAG system for handling document processing</param>
    /// <param name="dbContext">The database context for database interactions</param>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ResourcesController(ResourceManager resourceManager, IAzureBlobService blobService, IBackgroundTaskQueue taskQueue, RAGSystem ragSystem, DatabaseContext dbContext) : ControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<ResourcesController>();
        private readonly IBackgroundTaskQueue _taskQueue = taskQueue;
        private readonly RAGSystem _ragSystem = ragSystem;
        private readonly DatabaseContext database = dbContext;


        #region New
        /// <summary>
        /// Creates a new resource
        /// </summary>
        /// <param name="uploadDto">The Data Transfer Object</param>
        [HttpPut("new")]
        [SwaggerOperation(Summary = "Create a new resource in the archive.")]
        [SwaggerResponse(200, "Resource was created successfully", typeof(ApiResponse))]
        [SwaggerResponse(409, "Resource already exists", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> New([FromForm] ResourceUploadDto uploadDto)
        {
            ResourceCreateDto? dto = null;

            if (uploadDto.UploadType == "website")
                dto = JsonSerializer.Deserialize<WebsiteCreateDto>(uploadDto.Dto);
            else if (uploadDto.File != null)
                dto = DeserializeWithFile(uploadDto.UploadType, uploadDto.Dto, uploadDto.File);

            if (dto == null)
                return BadRequest(new ApiResponse(false, "Invalid DTO sent"));

            // Check if there is a title
            if (string.IsNullOrEmpty(dto.Title))
                return BadRequest(new ApiResponse(false, "No name was provided."));

            // Check if there is a typeId
            if (string.IsNullOrEmpty(dto.TypeId))
                return BadRequest(new ApiResponse(false, "No type ID was provided."));

            // Check if there is a language code
            if (string.IsNullOrEmpty(dto.LanguageCode))
                return BadRequest(new ApiResponse(false, "No language code was provided"));

            // Check if there is a publication date
            if (dto.PublicationDate == DateTime.MinValue)
                return BadRequest(new ApiResponse(false, "No publication date was provided"));

            // Checks for file
            if (dto is FileResourceCreateDto _fDto)
            {
                // Check if file was empty
                if (_fDto.File == null)
                    return BadRequest(new ApiResponse(false, "No file was uploaded."));

                // Check if the file is empty
                if (_fDto.File.Length == 0)
                    return BadRequest(new ApiResponse(false, "The uploaded file was empty."));

                // Check if the filetype is supported
                if (!Filetype.Supported(Path.GetExtension(_fDto.File.FileName)))
                    return BadRequest(new ApiResponse(false, "Filetype is not supported."));
            }

            // Checks for website
            if (dto is WebsiteCreateDto _wDto)
            {
                // Check if the URL is empty
                if (string.IsNullOrEmpty(_wDto.Url))
                    return BadRequest(new ApiResponse(false, "The URL was empty."));

                // Check if the URL is valid
                if (!ValidityUtil.IsValidUrl(_wDto.Url))
                    return BadRequest(new ApiResponse(false, "The URL was invalid."));
            }

            logger.Information("Creating resource '{Title}'...", dto.Title);

            try
            {
                // Start a transaction on the database, since we are going to perform multiple actions
                await resourceManager.BeginTransaction();

                // Create the resource in the database and retrieve the ID
                Guid id = Guid.Empty;
                switch (uploadDto.UploadType)
                {
                    case "website":
                        id = await resourceManager.CreateWebsiteAsync((WebsiteCreateDto)dto);
                        _taskQueue.QueueBackgroundWorkItem(async token =>
                        {
                            using var scope = HttpContext.RequestServices.CreateScope();
                            var ragManager = scope.ServiceProvider.GetRequiredService<RAGManger>();
                            await ragManager.MainPipline(id: id, chunk: $"{dto.Title}\n{dto.Description}\n{((WebsiteCreateDto)dto).Url}");
                        });
                        break;
                    case "document":
                        id = await resourceManager.CreateDocumentAsync((DocumentCreateDto)dto);
                        break;
                    case "audio":
                        id = await resourceManager.CreateAudioAsync((AudioCreateDto)dto);
                        _taskQueue.QueueBackgroundWorkItem(async token =>
                        {
                            using var scope = HttpContext.RequestServices.CreateScope();
                            var ragManager = scope.ServiceProvider.GetRequiredService<RAGManger>();
                            await ragManager.MainPipline(id: id, chunk: $"{dto.Title}\n{dto.Description}");
                        });

                        break;
                    case "video":
                        id = await resourceManager.CreateVideoAsync((VideoCreateDto)dto);
                        _taskQueue.QueueBackgroundWorkItem(async token =>
                        {
                            using var scope = HttpContext.RequestServices.CreateScope();
                            var ragManager = scope.ServiceProvider.GetRequiredService<RAGManger>();
                            await ragManager.MainPipline(id: id, chunk: $"{dto.Title}\n{dto.Description}");
                        });
                        break;
                    default:
                        id = await resourceManager.CreateResourceAsync(dto);
                        break;
                }


                // If the resource is a file, upload it to storage
                if (dto is FileResourceCreateDto fDto)
                {
                    // Check if file was empty
                    if (fDto.File == null)
                        return BadRequest(new ApiResponse(false, "No file was uploaded."));

                    // Get the extension and filetype
                    string extension = Path.GetExtension(fDto.File.FileName);
                    string fileType = Filetype.ConvertExtensionToFiletype(extension); // Resource type

                    logger.Information("Uploading file '{FileName}' to storage...", fDto.File.FileName);

                    // Create metadata to add to blob, this is used to reconstruct file when downloading
                    Dictionary<string, string> metadata = new() { { "extension", extension } };

                    // Upload the file to storage
                    BLOB_STATUSCODE result = await blobService.UploadBlobAsync(fileType, id.ToString(), metadata, fDto.File.OpenReadStream());

                    switch (result)
                    {
                        // Upload was successfull
                        case BLOB_STATUSCODE.OK:
                            logger.Information("File '{FileName}' uploaded successfully.", fDto.File.FileName);

                            _taskQueue.QueueBackgroundWorkItem(async token =>
                            {
                                using var scope = HttpContext.RequestServices.CreateScope();
                                var ragManager = scope.ServiceProvider.GetRequiredService<RAGManger>();
                                await ragManager.MainPipline(id: id, chunk: $"{dto.Title}\n{dto.Description}", fileType: fileType, fileStream: fDto.File.OpenReadStream());
                            });

                            break;

                        // Container is missing
                        case BLOB_STATUSCODE.NOTFOUND:
                            // Roll back database changes
                            await resourceManager.Rollback();
                            return NotFound(new ApiResponse(false, "Container could not be found."));

                        // File already exists
                        case BLOB_STATUSCODE.ALREADYEXISTS:
                            // Roll back database changes
                            await resourceManager.Rollback();
                            return Conflict(new ApiResponse(false, "File already exists in storage."));

                        // Unknown state, but was not OK, so count it as a fail
                        default:
                            // Roll back database changes
                            await resourceManager.Rollback();
                            return StatusCode(500, new ApiResponse(false, "Unknown Error."));
                    }
                }

                // Commit changes to the database and return success response
                await resourceManager.Commit();
                if (dto is FileResourceCreateDto fileDto && fileDto.File != null)
                {
                    string extension = Path.GetExtension(fileDto.File.FileName).Replace(".", "");
                    await resourceManager.UpdateResourceAsync(id, r => r.FileExt, extension);
                }
                logger.Information("Resource '{Title}' created successfully.", dto.Title);
                return Ok(new ApiResponse(true, "Resource created successfully.", id));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error creating resource {Title}.", dto.Title);
                await resourceManager.Rollback();
                return StatusCode(500, new ApiResponse(false, "Error creating resource", e.Message));
            }
        }
        #endregion

        #region Trash
        /// <summary>
        /// Trashes a resource
        /// </summary>
        /// <param name="id">The ID of the resource</param>
        [HttpPatch("trash/{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Trashes a resource.")]
        [SwaggerResponse(200, "Resource trashed successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Trash(string id)
        {
            // Check if the ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID."));

            try
            {
                // Check if the resource exists
                if (!await resourceManager.ResourceExistsAsync(id))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));

                logger.Information("Archiving resource with ID: {ID}", id);

                // Trash the resource
                await resourceManager.TrashResourceAsync(id);

                logger.Information("Trashed resource with ID '{ID}' successfully.", id);
                return Ok(new ApiResponse(true, "Resource trashed successfully."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error archiving resource with ID {ID}.", id);
                return StatusCode(500, new ApiResponse(false, "Error archiving resource", e.Message));
            }
        }
        #endregion

        #region Untrash
        /// <summary>
        /// Untrashes a resource
        /// </summary>
        /// <param name="id">The ID of the resource</param>
        [HttpPatch("untrash/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Untrashes a resource.")]
        [SwaggerResponse(200, "Resource untrashed successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Untrash(string id)
        {
            // Check if the ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID."));

            try
            {
                // Check if the resource exists
                if (!await resourceManager.ResourceExistsAsync(id))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));

                logger.Information("Unarchiving resource with ID: {ID}", id);

                // Untrash the resource
                await resourceManager.UntrashResourceAsync(id);

                logger.Information("Untrashed resource with ID '{ID}' successfully.", id);
                return Ok(new ApiResponse(true, "Resource untrashed successfully."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error unarchiving resource with ID {ID}.", id);
                return StatusCode(500, new ApiResponse(false, "Error unarchiving resource", e.Message));
            }
        }
        #endregion

        #region Download
        /// <summary>
        /// Downloads a resource
        /// </summary>
        /// <param name="id">The ID of the resource</param>
        [HttpGet("download/{id}")]
        [SwaggerOperation(Summary = "Download a resource from storage.")]
        [SwaggerResponse(200, "The requested file")]
        [SwaggerResponse(404, "Resource not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Download(string id)
        {
            // Check if the ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID."));

            logger.Information("gucci 1");

            try
            {
                // Check if the resource exists
                if (!await resourceManager.ResourceExistsAsync(id))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));

                logger.Information("Downloding resource with ID: {ID}", id);

                // Get the filetype from the database
                string filetype = await resourceManager.GetResourcePropertyAsync(id, "FileType");

                // If website, we cannot download, return BadRequest
                if (filetype.Equals("website", StringComparison.CurrentCultureIgnoreCase))
                    return BadRequest(new ApiResponse(false, "Cannot download website."));

                // Try to retrieve the file
                BlobDownloadResponse? maybeResponse = await blobService.DownloadBlobAsync(filetype, id);
                logger.Information("gucci 2");

                // If response is empty, the file does not exist in storage
                if (maybeResponse == null)
                    return NotFound(new ApiResponse(false, "Resource exists in database, but file could not be found."));

                // Convert to a non-empty response
                BlobDownloadResponse response = (BlobDownloadResponse)maybeResponse;

                // Set the contentType and generate a filename from the title
                string contentType = "application/octet-stream";
                string title = await resourceManager.GetResourcePropertyAsync(id, "Title");
                string extension = response.Metadata["extension"];
                string fileName = SanitizeFileName(title) + extension;
                logger.Information("gucci 3");

                // Try to get contentType from the extension
                if (Path.HasExtension(fileName))
                {
                    FileExtensionContentTypeProvider provider = new();
                    if (provider.TryGetContentType(fileName, out string? type) && !string.IsNullOrEmpty(type))
                        contentType = type;
                }

                // Return the file
                logger.Information("Downloaded file with ID '{ID}' successfully.", id);
                return File(response.FileStream, contentType, fileName);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error downloading resource with ID {ID}.", id);
                return StatusCode(500, new ApiResponse(false, "Error downloading resource", e.Message + e.StackTrace));
            }
        }
        #endregion

        #region Delete
        /// <summary>
        /// Deletes a resource
        /// </summary>
        /// <param name="id">The ID of the resource</param>
        [HttpDelete("delete/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Deletes a resource.")]
        [SwaggerResponse(200, "Resource deleted successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Delete(string id)
        {
            // Check if the ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID."));

            try
            {
                // Check if the resource exists
                if (!await resourceManager.ResourceExistsAsync(id))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));

                logger.Information("Deleting resource with ID: {ID}", id);

                // Get the filetype of the resource
                string filetype = await resourceManager.GetResourcePropertyAsync(id, "new(FileType as FileType)");

                // Delete the file from storage
                BLOB_STATUSCODE result = await blobService.DeleteBlobAsync(filetype, id);

                switch (result)
                {
                    // If result was OK, then it was a file which is now deleted
                    case BLOB_STATUSCODE.OK:
                        logger.Information("Resource was a file and file is now deleted.");
                        break;

                    // If result was NOTFOUND, then it was not a file, just continue
                    case BLOB_STATUSCODE.NOTFOUND:
                        logger.Information("Resource was not found in storage, only deleting in database.");
                        break;

                    // All other cases means an error
                    default:
                        logger.Error("Error deleting file '{ID}' in storage", id);
                        return StatusCode(500, new ApiResponse(false, "Error deleting file in storage."));
                }

                // Delete the resource from the database
                await resourceManager.DeleteResourceAsync(id);

                // Delete the chunks from the vector database
                bool chunkDeleted = await _ragSystem.DeleteAllPointsWithIdAsync(id);
                if (!chunkDeleted)
                {
                    logger.Warning("Something went wrong while deleting chunks for resource with ID '{ID}'", id);
                    return StatusCode(500, new ApiResponse(false, "Error deleting chunks from vector database."));
                }

                logger.Information("Deleted resource with ID '{ID}' successfully", id);
                return Ok(new ApiResponse(true, "Resource deleted successfully."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error deleting resource with ID {ID}.", id);
                return StatusCode(500, new ApiResponse(false, "Error deleting resource", e.Message));
            }
        }
        #endregion

        #region Update
        /// <summary>
        /// Updates the given resource's properties
        /// </summary>
        /// <param name="id">The ID of the resource</param>
        /// <param name="updates">A dictionary with property names and their new values</param>
        [HttpPatch("update/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Updates a resource.")]
        [SwaggerResponse(200, "Resource updated.", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource not found", typeof(ApiResponse))]
        [SwaggerResponse(409, "Already exists", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Update(string id, [FromBody] Dictionary<string, object> updates)
        {
            // Check if the ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID."));

            // Check if updates are provided
            if (updates == null || updates.Count == 0)
                return BadRequest(new ApiResponse(false, "No updates were provided."));

            logger.Information("Updating resource with ID '{ID}'...", id);

            try
            {
                // Check if resource exists
                if (!await resourceManager.ResourceExistsAsync(id))
                    return NotFound(new ApiResponse(false, "The resource does not exist"));

                // Start a database transaction, since we could have multiple updates
                await resourceManager.BeginTransaction();

                List<string> updatedProperties = [];

                // Update the properties
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(Resource), id, updates, ragSystem));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(WebsiteMetadata), id, updates, ragSystem));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(DocumentMetadata), id, updates, ragSystem));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(AudioMetadata), id, updates, ragSystem));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(VideoMetadata), id, updates, ragSystem));

                // No props were found
                if (updatedProperties.Count == 0)
                {
                    await resourceManager.Rollback();
                    return BadRequest(new ApiResponse(false, "None of the props were found."));
                }

                // Commit changes to database
                await resourceManager.Commit();

                // Join all updated properties
                string updatedPropertiesString = string.Join(", ", updatedProperties);

                // If all properties were updated
                if (updatedProperties.Count == updates.Count)
                {
                    logger.Information("Succesfully updated resource with ID '{ID}'. Updated properties: {props}", id, updatedPropertiesString);
                    return Ok(new ApiResponse(true, $"Resource updated successfully.", updatedProperties));
                }

                // If not all properties were updated
                else
                {
                    logger.Information("Partially updated resource with ID '{ID}'. Updated properties: {props}", id, updatedPropertiesString);
                    return Ok(new ApiResponse(true, $"Resource updated partially.", updatedProperties));
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error updating resource with ID {ID}.", id);
                await resourceManager.Rollback();
                return StatusCode(500, new ApiResponse(false, "Error updating resource", e.Message));
            }
        }
        #endregion

        #region Exists
        /// <summary>
        /// Checks if a resource already exists in the database
        /// </summary>
        /// <param name="hash">(Optional) The hash of the resource</param>
        /// <param name="url">(Optional) The URL of the resource</param>
        [EnableCors("AllowFrontend")]
        [HttpGet("exists")]
        [SwaggerOperation(Summary = "Check if a resource exists.")]
        [SwaggerResponse(200, "Response with boolean indicating if resource exists.", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Exists([FromQuery] string? hash, [FromQuery] string? url)
        {
            // Check for null
            if (string.IsNullOrEmpty(hash) && string.IsNullOrEmpty(url))
                return BadRequest(new ApiResponse(false, "No value given."));

            try
            {
                // Retrieve the ID of the resource if it already exists
                object? resourceId = null;

                // Handle hash for files
                if (!string.IsNullOrEmpty(hash))
                    resourceId = await resourceManager.GetResourcePropertyOrDefaultAsync(predicate: r => r.Hash == hash, selector: "Id");

                // Handle URL for websites
                else if (!string.IsNullOrEmpty(url))
                    resourceId = await resourceManager.GetWebsiteMetadataPropertyOrDefaultAsync(predicate: m => m.Url == url, selector: "ResourceId");



                // ID is empty, so no resource was found
                if (resourceId == null)
                    return Ok(new ApiResponse(true, "Resource does not exist", new { exists = false, id = "" }));

                // ID was not empty, so resource already exists, return the ID
                return Ok(new ApiResponse(true, "Resource already exists.", new { exists = true, id = resourceId.ToString() }));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error checking if resource exists.");
                return StatusCode(500, new ApiResponse(false, "Error checking if resource exists", e.Message));
            }
        }
        #endregion

        #region Info
        /// <summary>
        /// Gets the information of the resource (database row)
        /// </summary>
        /// <param name="id">The ID of the resource</param>
        /// <param name="properties">The properties you are trying to receive, separated by comma</param>
        [HttpGet("info/{id}")]
        [SwaggerOperation(Summary = "Get the information of the resource")]
        [SwaggerResponse(200, "Resource Information", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource Not Found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Info(string id, [FromQuery] string? properties)
        {
            // Check if ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "ID is invalid."));

            try
            {
                // Retrieve the resource or the specified properties
                object? resource = string.IsNullOrEmpty(properties) ?
                    await resourceManager.GetResourceAsync(id) :
                    await resourceManager.GetResourcePropertyAsync(id, $"new({properties})");

                // If null, the resource was not found
                if (resource == null)
                    return NotFound(new ApiResponse(false, "The resource was not found."));

                // Return the resource
                return Ok(new ApiResponse(true, "Resource was found.", resource));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error retrieving resource info.");
                return StatusCode(500, new ApiResponse(false, "Error retrieving resource info.", e.Message));
            }
        }
        #endregion

        #region List
        /// <summary>
        /// Retrieves a list or page of all resources
        /// </summary>
        /// <param name="pageIndex">(Optional) The index of the page</param>
        /// <param name="pageSize">(Optional) The size of the page</param>
        /// <param name="properties">(Optional) The properties to select, separated by comma</param>
        /// <param name="trash">(Optional) Whether to show trashed resources or non trashed resources</param>
        /// <param name="searchQuery">(Optional) Filter on search query </param>
        [HttpGet("list")]
        [SwaggerOperation(Summary = "Retrieves a list or page of all resources")]
        [SwaggerResponse(200, "A list or page of all the resources in the archive", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> List(int? pageIndex, int? pageSize, string? properties, string? searchQuery, bool trash = false)
        {
            if (trash && !User.IsInRole("admin"))
                return Unauthorized(new ApiResponse(false, "You are not authorized to view trashed resources."));
            logger.Information("Check 1");

            // Verification
            if (pageIndex != null && pageIndex < 1)
                return BadRequest(new ApiResponse(false, "Page index cannot be lower than 1."));

            if (pageSize != null && pageSize < 1)
                return BadRequest(new ApiResponse(false, "Page size cannot be lower than 1"));

            // Set defaults
            if (pageIndex != null && pageSize == null) pageSize = 100;
            if (pageSize != null && pageIndex == null) pageIndex = 1;

            try
            {
                // All resources to be returned
                object[] resources = [];

                string projectionString = $"new({properties})";

                Expression<Func<Resource, bool>>? predicate = searchQuery != null ? r => EF.Functions.TrigramsAreSimilar(r.Title, searchQuery) ||
                                                                                            EF.Functions.ILike(r.Title, $"{searchQuery}%") ||
                                                                                            EF.Functions.ILike(r.Title, $"%{searchQuery}%")
                                                                                            && r.Trashed == trash
                                                                                  : r => r.Trashed == trash;

                // No paging requested, list all resources
                if (pageIndex == null || pageSize == null)
                    resources = string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourcesAsync(predicate: predicate) :
                        await resourceManager.GetAllResourcesAsync(projection: projectionString, predicate: predicate);


                // Paging requested, retrieve resources on that page
                else
                    resources = string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetResourcePageAsync((int)pageIndex, (int)pageSize, predicate: predicate) :
                        await resourceManager.GetResourcePageAsync(projectionString, (int)pageIndex, (int)pageSize, predicate: predicate);

                // Return found resources
                return Ok(new ApiResponse(true, $"Found {resources.Length} resources", resources));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error listing resources.");
                return StatusCode(500, new ApiResponse(false, "Error listing resources.", e.Message));
            }
        }
        #endregion

        #region Types New
        /// <summary>
        /// Creates a new resource type
        /// </summary>m
        [HttpPut("types/new")]
        [SwaggerOperation(Summary = "Creates a new resource type")]
        [SwaggerResponse(200, "Resource type created successfully", typeof(ApiResponse))]
        [SwaggerResponse(409, "Resource type already exists", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> TypesNew([FromBody] ResourceTypeCreateDto dto)
        {
            // Validation
            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new ApiResponse(false, "Invalid name"));

            try
            {
                // Check if resource type already exists
                if (await resourceManager.ResourceTypeExistsAsync(rt => rt.Name == dto.Name))
                    return Conflict(new ApiResponse(false, "Resource type already exists"));

                logger.Information("Creating resource type with name '{Name}'", dto.Name);

                // Create resource type and return ID
                Guid id = await resourceManager.CreateResourceTypeAsync(dto);

                logger.Information("Resource type with name '{Name}' created successfully", dto.Name);

                return Ok(new ApiResponse(true, "Resource type created successfully", id));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error creating resource type.");
                return StatusCode(500, new ApiResponse(false, "Error creating resource type.", e.Message));
            }
        }
        #endregion

        #region Types Fetch
        /// <summary>
        /// Retrieves a list of all resource types
        /// </summary>
        [HttpGet("types/list")]
        [SwaggerOperation(Summary = "Retrieves a list of all resource types")]
        [SwaggerResponse(200, "A list of all the resource types", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> TypesFetch()
        {
            try
            {
                // Fetch the resource types
                ResourceType[] types = await resourceManager.GetAllResourceTypesAsync();

                // Return the resource types
                return Ok(new ApiResponse(true, $"Found {types.Length} resource types", types));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error listing resource types");
                return StatusCode(500, new ApiResponse(false, "Error listing resource types", e.Message));
            }
        }
        #endregion

        #region Filetype Support fetch
        /// <summary>
        /// Retrieves a dictionary of supported extensions per upload type
        /// </summary>
        /// <returns>A dictionary of supported extensions per upload type</returns>
        [HttpGet("supported_extensions")]
        [SwaggerOperation(Summary = "Retrieves a dictionary of all supported file extensions per uploadtype")]
        [SwaggerResponse(200, "A dictionary of all supported file extensions per upload type", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public IActionResult FiletypeSupportFetch()
        {
            try
            {
                // Return the dictionary
                return Ok(new ApiResponse(true, "Fetch successfull", Filetype.SupportedExtensions));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error fetching supported extensions");
                return StatusCode(500, new ApiResponse(false, "Error fetching supported extensions", e.Message));
            }
        }
        #endregion

        #region Relation fetches
        /// <summary>
        /// Retrieves all relations of the given type for the given resource ID
        /// </summary>
        /// <param name="relation">The relation to retrieve</param>
        /// <param name="id">The ID of the resource</param>
        /// <param name="properties">(Optional) The properties to select from the result</param>
        [HttpGet("{id}/relations/{relation}")]
        [SwaggerOperation(Summary = "Retrieves all relations of the given type for the given resource ID")]
        [SwaggerResponse(200, "The relations", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Relations(string relation, string id, string? properties)
        {
            // Check if relation is filled in
            if (string.IsNullOrEmpty(relation))
                return BadRequest(new ApiResponse(false, "Invalid relation"));

            // Check ID
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID"));

            try
            {
                if (relation == "resource-related-resources")
                {
                    return await GetRelatedResources(id);
                }

                object? result = relation switch
                {
                    // Authors
                    "authors" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceAuthorRelationsAsync(r => r.ResourceId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceAuthorRelationsAsync(predicate: r => r.ResourceId == Guid.Parse(id), projection: $"new({properties})"),

                    // Organisations
                    "organisations" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceOrganisationRelationsAsync(r => r.ResourceId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceOrganisationRelationsAsync(predicate: r => r.ResourceId == Guid.Parse(id), projection: $"new({properties})"),

                    // Regions
                    "regions" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceRegionRelationsAsync(r => r.ResourceId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceRegionRelationsAsync(predicate: r => r.ResourceId == Guid.Parse(id), projection: $"new({properties})"),

                    // Related Organisations
                    "related-organisations" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceRelatedOrganisationRelationsAsync(r => r.ResourceId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceRelatedOrganisationRelationsAsync(predicate: r => r.ResourceId == Guid.Parse(id), projection: $"new({properties})"),

                    // Related Persons
                    "related-persons" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceRelatedPersonRelationsAsync(r => r.ResourceId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceRelatedPersonRelationsAsync(predicate: r => r.ResourceId == Guid.Parse(id), projection: $"new({properties})"),

                    // Sources
                    "sources" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceSourceRelationsAsync(r => r.ResourceId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceSourceRelationsAsync(predicate: r => r.ResourceId == Guid.Parse(id), projection: $"new({properties})"),

                    // Sources
                    "related-sources" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceRelatedSourceRelationsAsync(r => r.ResourceId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceRelatedSourceRelationsAsync(predicate: r => r.ResourceId == Guid.Parse(id), projection: $"new({properties})"),

                    // Tags
                    "tags" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceTagRelationsAsync(r => r.ResourceId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceTagRelationsAsync(predicate: r => r.ResourceId == Guid.Parse(id), projection: $"new({properties})"),

                    // Tags
                    "website" => await resourceManager.GetWebsiteMetadataAsync(r => r.ResourceId == Guid.Parse(id)),

                    // Default
                    _ => null
                };

                if (result == null)
                    return NotFound(new ApiResponse(false, "ID or relation not found"));

                return Ok(new ApiResponse(true, "Successfully retrieved relations", result));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error retrieving relation '{Relation}' for resource with ID '{Id}'", relation, id);
                return StatusCode(500, new ApiResponse(false, "Internal Server Error", e.Message));
            }
        }
        #endregion

        #region Add Relations
        /// <summary>
        /// Adds a relation for this resource
        /// </summary>
        /// <param name="id">The ID of the resource</param>
        /// <param name="relation">The relation to be made</param>
        /// <param name="targetId">The ID of the other item in the relation</param>
        /// <param name="relationInfo">(Optional) Extra information over the relation</param>
        [HttpGet("{id}/relations/add/{relation}/{targetId}")]
        [SwaggerOperation(Summary = "Adds a relation to the resource")]
        [SwaggerResponse(200, "Successfully added relation", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> AddRelation(string id, string relation, string targetId, [FromQuery] string? relationInfo)
        {
            // Check if relation is filled in
            if (string.IsNullOrEmpty(relation))
                return BadRequest(new ApiResponse(false, "Invalid relation"));

            // Check if ids are valid
            if (!ValidityUtil.IsValidId(id)) return BadRequest(new ApiResponse(false, "Invalid ID"));
            if (!ValidityUtil.IsValidId(targetId) && !ValidityUtil.IsValidUrl(targetId)) return BadRequest(new ApiResponse(false, "Invalid target ID/URL"));

            try
            {
                switch (relation)
                {
                    // Authors
                    case "authors":
                        await resourceManager.AddAuthorToResourceAsync(id, targetId);
                        break;

                    // Organisations
                    case "organisations":
                        await resourceManager.AddOrganisationToResourceAsync(id, targetId, relationInfo ?? "");
                        break;

                    // Regions
                    case "regions":
                        await resourceManager.AddRegionToResourceAsync(id, targetId);
                        break;

                    // Related organisations
                    case "related-organisations":
                        await resourceManager.AddRelatedOrganisationToResourceAsync(id, targetId, relationInfo ?? "");
                        break;

                    // Related persons
                    case "related-persons":
                        await resourceManager.AddRelatedPersonToResourceAsync(id, targetId, relationInfo);
                        break;

                    // Sources
                    case "sources":
                        await resourceManager.AddSourceToResourceAsync(id, System.Net.WebUtility.UrlDecode(targetId));
                        break;

                    // Related Sources
                    case "related-sources":
                        await resourceManager.AddRelatedSourceToResourceAsync(id, System.Net.WebUtility.UrlDecode(targetId));
                        break;

                    // Tags
                    case "tags":
                        await resourceManager.AddTagToResourceAsync(id, targetId);
                        break;

                    // Default
                    default:
                        return BadRequest(new ApiResponse(false, "Invalid relation"));
                }

                return Ok(new ApiResponse(true, "Relation added successfully"));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error creating relation '{Relation}' for person with ID '{Id}'", relation, id);
                return StatusCode(500, new ApiResponse(false, "Internal Server Error", e.Message));
            }
        }
        #endregion

        #region Remove Relation

        /// <summary>
        /// Adds a relation for this resource
        /// </summary>
        /// <param name="id">The ID of the resource</param>
        /// <param name="relation">The relation to be removed</param>
        /// <param name="targetId">The ID of the other item in the relation</param>
        [HttpGet("{id}/relations/remove/{relation}/{targetId}")]
        [SwaggerOperation(Summary = "Removes a relation to the resource")]
        [SwaggerResponse(200, "Successfully removed relation", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> RemoveRelation(string id, string relation, string targetId)
        {
            // Check if relation is filled in
            if (string.IsNullOrEmpty(relation))
                return BadRequest(new ApiResponse(false, "Invalid relation"));

            // Check if ids are valid
            if (!ValidityUtil.IsValidId(id)) return BadRequest(new ApiResponse(false, "Invalid ID"));
            if (!ValidityUtil.IsValidId(targetId) && !ValidityUtil.IsValidUrl(targetId)) return BadRequest(new ApiResponse(false, "Invalid target ID/URL"));

            try
            {
                switch (relation)
                {
                    // Authors
                    case "authors":
                        await resourceManager.RemoveAuthorFromResourceAsync(id, targetId);
                        break;

                    // Organisations
                    case "organisations":
                        await resourceManager.RemoveOrganisationFromResourceAsync(id, targetId);
                        break;

                    // Regions
                    case "regions":
                        await resourceManager.RemoveRegionFromResourceAsync(id, targetId);
                        break;

                    // Related organisations
                    case "related-organisations":
                        await resourceManager.RemoveRelatedOrganisationFromResourceAsync(id, targetId);
                        break;

                    // Related persons
                    case "related-persons":
                        await resourceManager.RemoveRelatedPersonFromResourceAsync(id, targetId);
                        break;

                    // Sources
                    case "sources":
                        await resourceManager.RemoveSourceFromResourceAsync(id, System.Net.WebUtility.UrlDecode(targetId));
                        break;

                    // Related Sources
                    case "related-sources":
                        await resourceManager.RemoveRelatedSourceFromResourceAsync(id, System.Net.WebUtility.UrlDecode(targetId));
                        break;

                    // Tags
                    case "tags":
                        await resourceManager.RemoveTagFromResourceAsync(id, targetId);
                        break;

                    // Default
                    default:
                        return BadRequest(new ApiResponse(false, "Invalid relation"));
                }

                return Ok(new ApiResponse(true, "Relation removed successfully"));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error removing relation '{Relation}' for person with ID '{Id}'", relation, id);
                return StatusCode(500, new ApiResponse(false, "Internal Server Error", e.Message));
            }
        }
        #endregion

        #region Large File Upload

        /// <summary>
        /// Initializes a large file upload session
        /// </summary>
        /// <param name="uploadDto">DTO containing resource metadata and file info</param>
        /// <returns>Resource ID if successful</returns>
        [HttpPut("large/init")]
        [SwaggerOperation(Summary = "Initialize a large file upload session")]
        [SwaggerResponse(200, "Upload session initialized", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> InitLargeFileUpload([FromForm] ResourceUploadDto uploadDto)
        {
            ResourceCreateDto? dto = null;

            if (uploadDto.UploadType == "website")
                dto = JsonSerializer.Deserialize<WebsiteCreateDto>(uploadDto.Dto);
            else if (uploadDto.File != null)
                dto = DeserializeWithFile(uploadDto.UploadType, uploadDto.Dto, uploadDto.File);

            if (dto == null)
                return BadRequest(new ApiResponse(false, "Invalid DTO sent"));

            // Check if there is a title
            if (string.IsNullOrEmpty(dto.Title))
                return BadRequest(new ApiResponse(false, "No name was provided."));

            // Check if there is a typeId
            if (string.IsNullOrEmpty(dto.TypeId))
                return BadRequest(new ApiResponse(false, "No type ID was provided."));

            // Check if there is a language code
            if (string.IsNullOrEmpty(dto.LanguageCode))
                return BadRequest(new ApiResponse(false, "No language code was provided"));

            // Check if there is a publication date
            if (dto.PublicationDate == DateTime.MinValue)
                return BadRequest(new ApiResponse(false, "No publication date was provided"));

            logger.Information("Creating resource '{Title}'...", dto.Title);

            try
            {
                // Start a transaction on the database, since we are going to perform multiple actions
                await resourceManager.BeginTransaction();

                // Create the resource in the database and retrieve the ID
                Guid id = uploadDto.UploadType switch
                {
                    "document" => await resourceManager.CreateDocumentAsync((DocumentCreateDto)dto),
                    "audio" => await resourceManager.CreateAudioAsync((AudioCreateDto)dto),
                    "video" => await resourceManager.CreateVideoAsync((VideoCreateDto)dto),
                    _ => await resourceManager.CreateResourceAsync(dto)
                };

                await resourceManager.Commit();

                // Return the resource Id
                return Ok(new ApiResponse(true, "Upload session initialized", id));
            }
            catch (Exception e)
            {
                await resourceManager.Rollback();
                logger.Error(e, "Error initializing large file upload for resource {Title}.", dto.Title);
                return StatusCode(500, new ApiResponse(false, "Error initializing upload session", e.Message));
            }
        }

        /// <summary>
        /// Uploads a chunk (block) of a large file
        /// </summary>
        /// <param name="blockId">Unique ID for this block (must be base64 encoded)</param>
        /// <param name="resourceId">Resource ID from initialization</param>
        /// <param name="fileType">File type container name</param>
        [HttpPost("large/chunk/{resourceId}/{fileType}/{blockId}")]
        [SwaggerOperation(Summary = "Upload a chunk of a large file")]
        [SwaggerResponse(200, "Chunk uploaded successfully", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> UploadChunk(string resourceId, string fileType, string blockId)
        {
            if (Request.Body == null)
                return BadRequest(new ApiResponse(false, "No chunk data was provided."));

            if (string.IsNullOrEmpty(resourceId) || !ValidityUtil.IsValidId(resourceId))
                return BadRequest(new ApiResponse(false, "Invalid resource ID."));

            if (string.IsNullOrEmpty(fileType))
                return BadRequest(new ApiResponse(false, "No file type was provided."));

            if (string.IsNullOrEmpty(blockId))
                return BadRequest(new ApiResponse(false, "No block ID was provided."));

            try
            {
                Guid parsedResourceId = Guid.Parse(resourceId);

                // Make sure blockId is base64 encoded
                string base64BlockId = blockId;
                if (!Base64.IsValid(blockId))
                {
                    base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));
                }

                // Get container
                BlobContainerClient container = await blobService.GetOrCreateContainerAsync(fileType);

                // Get block blob client
                BlockBlobClient blockBlobClient = container.GetBlockBlobClient(resourceId);

                // Parse request body to a memorystream
                MemoryStream memoryStream = new MemoryStream();
                await Request.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Stage the current chunk
                await blockBlobClient.StageBlockAsync(base64BlockId, memoryStream);

                logger.Information("Chunk {BlockId} uploaded for resource {ResourceId}", blockId, resourceId);

                return Ok(new ApiResponse(true, "Chunk uploaded successfully"));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error uploading chunk {BlockId} for resource {ResourceId}", blockId, resourceId);
                return StatusCode(500, new ApiResponse(false, "Error uploading chunk", e.Message));
            }
        }

        /// <summary>
        /// Finalizes a large file upload by committing all uploaded blocks
        /// </summary>
        /// <param name="finalizeDto">Finalization information and resource metadata</param>
        [HttpPut("large/finalize")]
        [SwaggerOperation(Summary = "Finalize a large file upload")]
        [SwaggerResponse(200, "File upload finalized successfully", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> FinalizeLargeFileUpload([FromForm] LargeFileFinalizeDto finalizeDto)
        {
            if (string.IsNullOrEmpty(finalizeDto.ResourceId) || !ValidityUtil.IsValidId(finalizeDto.ResourceId))
                return BadRequest(new ApiResponse(false, "Invalid resource ID."));

            if (string.IsNullOrEmpty(finalizeDto.FileType))
                return BadRequest(new ApiResponse(false, "No file type was provided."));

            if (string.IsNullOrEmpty(finalizeDto.FileName))
                return BadRequest(new ApiResponse(false, "No file name was provided."));

            if (finalizeDto.BlockIds == null || finalizeDto.BlockIds.Count == 0)
                return BadRequest(new ApiResponse(false, "No block IDs were provided."));

            // Get the resource ID
            Guid resourceId = Guid.Parse(finalizeDto.ResourceId);

            try
            {
                logger.Information("Finalizing large file upload for resource {ResourceId}", resourceId);

                string extension = Path.GetExtension(finalizeDto.FileName);
                string fileType = Filetype.ConvertExtensionToFiletype(extension);

                // Convert blockIds to base64 if needed
                List<string> base64BlockIds = finalizeDto.BlockIds.Select(id =>
                    Base64.IsValid(id) ? id : Convert.ToBase64String(Encoding.UTF8.GetBytes(id))).ToList();

                // Create metadata to add to blob
                Dictionary<string, string> metadata = new() { { "extension", extension } };

                BLOB_STATUSCODE code = await blobService.CommitBlockListAsync(resourceId.ToString(), fileType, base64BlockIds, metadata);

                logger.Information("Large file upload finalized for resource {ResourceId}", resourceId);

                return Ok(new ApiResponse(true, "File upload finalized successfully", resourceId));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error finalizing large file upload for resource {ResourceId}", resourceId);
                await resourceManager.Rollback();
                return StatusCode(500, new ApiResponse(false, "Error finalizing upload", e.Message));
            }
        }

        /// <summary>
        /// Cleans up a failed large file upload by deleting database entry. Cleaning of block blobs
        /// is automatically handled, uncommitted blocks are deleted.
        /// </summary>
        /// <param name="resourceId">Resource ID to clean up</param>
        [HttpDelete("large/cleanup/{resourceId}")]
        [SwaggerOperation(Summary = "Clean up a failed large file upload")]
        [SwaggerResponse(200, "Upload cleaned up successfully", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> CleanupFailedUpload(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId) || !ValidityUtil.IsValidId(resourceId))
                return BadRequest(new ApiResponse(false, "Invalid resource ID."));

            Guid parsedResourceId;
            try
            {
                parsedResourceId = Guid.Parse(resourceId);
            }
            catch (Exception)
            {
                return BadRequest(new ApiResponse(false, "Invalid resource ID format."));
            }

            try
            {
                // Delete the database entry
                await resourceManager.BeginTransaction();
                string? fileType = await resourceManager.GetResourcePropertyOrDefaultAsync(parsedResourceId.ToString(), "FileType");
                if (fileType is not null)
                {
                    BLOB_STATUSCODE code = await blobService.CommitBlockListAsync(resourceId.ToString(), fileType, [], new());
                    if (code == BLOB_STATUSCODE.OK)
                    {
                        await blobService.DeleteBlobAsync(fileType, resourceId.ToString());
                        await resourceManager.DeleteResourceAsync(parsedResourceId.ToString());
                        logger.Information("Successfully cleaned up resource {ResourceId}", resourceId);
                        return Ok(new ApiResponse(true, "Upload cleaned up successfully"));
                    }
                    else if (code == BLOB_STATUSCODE.NOTFOUND)
                    {
                        await resourceManager.DeleteResourceAsync(parsedResourceId.ToString());
                        logger.Warning("Blob {RecourseId} not found during cleanup", resourceId);
                        return Ok(new ApiResponse(true, "Blob not found, but database entry removed, cleanup completed"));
                    }
                    else
                    {
                        logger.Error("Error deleting blob {ResourceId} in storage", resourceId);
                        return StatusCode(500, new ApiResponse(false, "Error deleting blob in storage"));
                    }

                }
                else
                {
                    logger.Warning("Resource {ResourceId} not found during cleanup", resourceId);
                    return Ok(new ApiResponse(true, "Resource not found, but cleanup completed"));
                }
            }
            catch (Exception e)
            {
                await resourceManager.Rollback();
                logger.Error(e, "Error cleaning up for resource {ResourceId}", resourceId);
                return StatusCode(500, new ApiResponse(false, "Error cleaning up upload", e.Message));
            }
        }

        #endregion

        #region Archive Grid
        /// <summary>
        /// Retrieves the resource grid items which are displayed on the archive page
        /// </summary>
        /// <param name="request">The request DTO</param>
        [HttpPost("grid")]
        [SwaggerOperation(Summary = "Retrieves the resource grid items which are displayed on the archive page")]
        [SwaggerResponse(200, "The list of ResourceGridItems", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> GetGrid([FromBody] GridRequest request)
        {
            // Check page settings
            if (request.PageIndex < 1)
                return BadRequest(new ApiResponse(false, "Page index cannot be lower than 1"));

            if (request.PageSize < 1)
                return BadRequest(new ApiResponse(false, "Page size cannot be lower than 1"));

            try
            {
                // Execute the search
                GridSearchTemplate searchResult = await resourceManager.SearchResourceGridAsync(request);

                // Return result
                return Ok(new ApiResponse(true, $"Found {searchResult.TotalCount} total items", searchResult));
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to fetch resource grid");
                return StatusCode(500, new ApiResponse(false, "Internal Server Error", e.Message));
            }
        }
        #endregion


        #region Helper Functions
        // ---------------------------
        // Helper functions
        // ---------------------------

        // Helper function to add file to dto
        private FileResourceCreateDto? DeserializeWithFile(string uploadType, string jsonDto, IFormFile file)
        {
            return uploadType switch
            {
                "document" => DeserializeAndAssignFile<DocumentCreateDto>(jsonDto, file),
                "audio" => DeserializeAndAssignFile<AudioCreateDto>(jsonDto, file),
                "video" => DeserializeAndAssignFile<VideoCreateDto>(jsonDto, file),
                _ => DeserializeAndAssignFile<FileResourceCreateDto>(jsonDto, file)
            };
        }

        private T? DeserializeAndAssignFile<T>(string jsonDto, IFormFile file) where T : FileResourceCreateDto
        {
            T? dto = JsonSerializer.Deserialize<T>(jsonDto);

            if (dto != null)
                dto.File = file;

            return dto;
        }

        // Helper method to update a property
        private async Task UpdateProperty<TSet, TProperty>(string id, string propertyName, TProperty newValue) where TSet : class
        {
            Type setType = typeof(TSet);

            // Update the appropiate property based on the type
            await (setType switch
            {
                // If type is Resource
                Type t when t == typeof(Resource) => resourceManager.UpdateResourceAsync(id, PropertyUpdateUtil.CreatePropertySelector<Resource, TProperty>(propertyName), newValue),

                // If type is WebsiteMetadata
                Type t when t == typeof(WebsiteMetadata) => resourceManager.UpdateWebsiteMetadataAsync(id, PropertyUpdateUtil.CreatePropertySelector<WebsiteMetadata, TProperty>(propertyName), newValue),

                // If type is DocumentMetadata
                Type t when t == typeof(DocumentMetadata) => resourceManager.UpdateDocumentMetadataAsync(id, PropertyUpdateUtil.CreatePropertySelector<DocumentMetadata, TProperty>(propertyName), newValue),

                // If type is VideoMetadata
                Type t when t == typeof(VideoMetadata) => resourceManager.UpdateVideoMetadataAsync(id, PropertyUpdateUtil.CreatePropertySelector<VideoMetadata, TProperty>(propertyName), newValue),

                // If type is AudioMetadata
                Type t when t == typeof(AudioMetadata) => resourceManager.UpdateAudioMetadataAsync(id, PropertyUpdateUtil.CreatePropertySelector<AudioMetadata, TProperty>(propertyName), newValue),

                // Default
                _ => throw new ArgumentException($"Unsupported type: {setType.Name}")
            });
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

        private async Task<IActionResult> GetRelatedResources(string id)
        {
            try
            {
                // Check if the resource exists
                if (!await resourceManager.ResourceExistsAsync(id))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));

                // Get vector points for the current resource
                IReadOnlyList<ScoredPoint> pointsIds = await _ragSystem.QdrantClient.QueryAsync(
                    RAGSystem.COLLECTION_NAME,
                    filter: MatchKeyword("resourceId", id)
                );

                if (pointsIds.Count == 0)
                    return Ok(new ApiResponse(true, "No vector points found for resource", Array.Empty<Resource>()));

                // Find related resources using vector similarity
                IReadOnlyList<PointGroup> results = await _ragSystem.QdrantClient.RecommendGroupsAsync(
                    RAGSystem.COLLECTION_NAME,
                    groupBy: "resourceId",
                    positive: pointsIds.Select(p => p.Id).ToArray(),
                    filter: !MatchKeyword("resourceId", id), // Exclude the current resource
                    limit: 5
                );

                // Retrieve the actual resource objects
                if (results.Count == 0)
                    return Ok(new ApiResponse(true, "No related resources found", Array.Empty<Resource>()));

                //ResourceIds ids
                var resourceIds = results.Select(result => Guid.Parse(result.Id.StringValue)).ToList();

                // Retrieve resources based on the found IDs
                var query = await resourceManager.GetAllResourcesAsync(
                    predicate: r => resourceIds.Contains(r.Id)
                );

                // Check if any resources were found
                if (query.Count() == 0)
                    return NotFound(new ApiResponse(true, "No related resources found", Array.Empty<Resource>()));

                return Ok(new ApiResponse(true, "Related resources found", query.ToArray()));
            }

            catch (Exception ex)
            {
                logger.Error(ex, "Error retrieving relation 'resource-related-resources' for resource with ID '{Id}'", id);
                return StatusCode(500, new ApiResponse(false, "Internal Server Error: Related Resources", ex.Message));
            }
        }


        #endregion
    }
}