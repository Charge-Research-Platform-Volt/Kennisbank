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
    /// <param name="ragManager">The RAG manager for metadata updates and query processing</param>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ResourcesController(ResourceManager resourceManager, IAzureBlobService blobService, IBackgroundTaskQueue taskQueue, RAGSystem ragSystem, RAGManager ragManager) : ControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<ResourcesController>();
        private readonly IBackgroundTaskQueue _taskQueue = taskQueue;


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
                            var ragManager = scope.ServiceProvider.GetRequiredService<RAGManager>();
                            await ragManager.MainPipeline(id: id, chunk: $"{dto.Title}\n{dto.Description}\n{((WebsiteCreateDto)dto).Url}");
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
                            var ragManager = scope.ServiceProvider.GetRequiredService<RAGManager>();
                            await ragManager.MainPipeline(id: id, chunk: $"{dto.Title}\n{dto.Description}");
                        });

                        break;
                    case "video":
                        id = await resourceManager.CreateVideoAsync((VideoCreateDto)dto);
                        _taskQueue.QueueBackgroundWorkItem(async token =>
                        {
                            using var scope = HttpContext.RequestServices.CreateScope();
                            var ragManager = scope.ServiceProvider.GetRequiredService<RAGManager>();
                            await ragManager.MainPipeline(id: id, chunk: $"{dto.Title}\n{dto.Description}");
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
                                var ragManager = scope.ServiceProvider.GetRequiredService<RAGManager>();
                                await ragManager.MainPipeline(id: id, chunk: $"{dto.Title}\n{dto.Description}", fileType: fileType, fileStream: fDto.File.OpenReadStream());
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

                logger.Information("Trashing resource with ID: {ID}", id);
                    
                // Trash the resource
                await resourceManager.TrashResourceAsync(id);

                logger.Information("Trashed resource with ID '{ID}' successfully.", id);
                return Ok(new ApiResponse(true, "Resource trashed successfully."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error trashing resource with ID {ID}.", id);
                return StatusCode(500, new ApiResponse(false, "Error trashing resource", e.Message));
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

                logger.Information("Untrashing resource with ID: {ID}", id);
                    
                // Untrash the resource
                await resourceManager.UntrashResourceAsync(id);

                logger.Information("Untrashed resource with ID '{ID}' successfully.", id);
                return Ok(new ApiResponse(true, "Resource untrashed successfully."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error untrashing resource with ID {ID}.", id);
                return StatusCode(500, new ApiResponse(false, "Error untrashing resource", e.Message));
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
                string filetype = await resourceManager.GetResourcePropertyAsync(id, "FileType");

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
                bool chunkDeleted = await ragSystem.DeleteAllPointsWithIdAsync(id);
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
        [Authorize]
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

                // Update the properties (pass ragManager for rich metadata updates)
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(Resource), id, updates, ragSystem, ragManager));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(WebsiteMetadata), id, updates, ragSystem, ragManager));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(DocumentMetadata), id, updates, ragSystem, ragManager));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(AudioMetadata), id, updates, ragSystem, ragManager));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(VideoMetadata), id, updates, ragSystem, ragManager));

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
                 
                Expression<Func<Resource, bool>>? predicate = searchQuery != null ? r =>    (/*EF.Functions.TrigramsAreSimilar(r.Title, searchQuery) || {NOT ALLOWED IN AZURE POSTGRES} */
                                                                                            EF.Functions.ILike(r.Title, $"{searchQuery}%") ||
                                                                                            EF.Functions.ILike(r.Title, $"%{searchQuery}%"))
                                                                                            && r.Trashed == trash
                                                                                  : r =>    r.Trashed == trash;


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
                if (relation == "resource-similar-resources")
                {
                    return await GetSimilarResources(id);
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

                    // Related Persons
                    "related-persons" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceRelatedPersonRelationsAsync(r => r.ResourceId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceRelatedPersonRelationsAsync(predicate: r => r.ResourceId == Guid.Parse(id), projection: $"new({properties})"),

                    // Sources
                    "sources" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceSourceRelationsAsync(r => r.ResourceId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceSourceRelationsAsync(predicate: r => r.ResourceId == Guid.Parse(id), projection: $"new({properties})"),

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

                    // Related persons
                    case "related-persons":
                        await resourceManager.AddRelatedPersonToResourceAsync(id, targetId, relationInfo);
                        break;

                    // Sources
                    case "sources":
                        await resourceManager.AddSourceToResourceAsync(id, System.Net.WebUtility.UrlDecode(targetId));
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

                    // Related persons
                    case "related-persons":
                        await resourceManager.RemoveRelatedPersonFromResourceAsync(id, targetId);
                        break;

                    // Sources
                    case "sources":
                        await resourceManager.RemoveSourceFromResourceAsync(id, System.Net.WebUtility.UrlDecode(targetId));
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

        #region Update Relation Role
        /// <summary>
        /// Updates the role/relation in a relation between this resource and another entity
        /// </summary>
        /// <param name="id">The ID of the resource</param>
        /// <param name="relation">The relation type (organisations, related-persons)</param>
        /// <param name="targetId">The ID of the related entity</param>
        /// <param name="newRole">The new role/relation value</param>
        [HttpPatch("{id}/relations/update-role/{relation}/{targetId}")]
        [Authorize]
        [SwaggerOperation(Summary = "Updates the role/relation in a relationship")]
        [SwaggerResponse(200, "Role updated successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource or relation not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> UpdateRelationRole(string id, string relation, string targetId, [FromQuery] string newRole)
        {
            // Validation
            if (string.IsNullOrEmpty(relation))
                return BadRequest(new ApiResponse(false, "Invalid relation"));

            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID"));

            if (!ValidityUtil.IsValidId(targetId))
                return BadRequest(new ApiResponse(false, "Invalid target ID"));

            try
            {
                // Use the utility to update the role
                bool success = await RelationUpdateUtil.UpdateRelationRole(
                    resourceManager,
                    "resources",
                    id,
                    relation,
                    targetId,
                    newRole ?? ""
                );

                if (!success)
                    return NotFound(new ApiResponse(false, "Relation not found or invalid relation type"));

                return Ok(new ApiResponse(true, "Role updated successfully"));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error updating role in relation '{Relation}' for resource with ID '{Id}'", relation, id);
                return StatusCode(500, new ApiResponse(false, "Internal Server Error", e.Message));
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

        #region Archive Trash Grid
        /// <summary>
        /// Retrieves the trashed resource grid items which are displayed in the trash section of the archive page
        /// </summary>
        [HttpGet("trash-grid")]
        [SwaggerOperation(Summary = "Retrieves the resource trash items which are displayed on the archive page")]
        [SwaggerResponse(200, "The list of ResourceTrashItems", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> GetTrash() 
        {
            try 
            {
                // Fetch trash
                ResourceTrashItem[] items = await resourceManager.GetAllResourceTrashItemsAsync();

                // Return result
                return Ok(new ApiResponse(true, $"Found {items.Length} items", items));
            }
            catch (Exception e) 
            {
                logger.Error(e, "Failed to fetch resource trash");
                return StatusCode(500, new ApiResponse(false, "Internal Server Error", e.Message));
            }
        }
        #endregion
        
        #region Get Metadata Type
        [HttpGet("{id}/metadata-type")]
        [SwaggerOperation(Summary = "Get the metadata type from the materialized view (resource/person/organisation)")]
        [SwaggerResponse(200, "The metadata type", typeof(ApiResponse))]
        [SwaggerResponse(404, "Not Found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> GetMetadataType(string id) 
        {
            // Check id
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID"));
        
            try 
            {
                // Fetch the metadata type
                string? mType = await resourceManager.GetMetadataType(id);

                // metadata type is null when ID is not found
                if (mType == null)
                    return NotFound(new ApiResponse(false, "ID not found!"));

                // Return metadata type
                return Ok(new ApiResponse(true, "Metadata type found", mType));
            }
            catch (Exception e) 
            {
                logger.Error(e, "Failed to fetch metadata type");
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

        private async Task<IActionResult> GetSimilarResources(string id)
        {
            try
            {
                // Similarity threshold - only return resources with score above this value
                // Score ranges from 0 to 1, where higher means more similar
                const float similarityThreshold = 0.6f;

                // Check if the resource exists
                if (!await resourceManager.ResourceExistsAsync(id))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));

                // Get vector points for the current resource
                IReadOnlyList<ScoredPoint> pointsIds = await ragSystem.QdrantClient.QueryAsync(
                    ragSystem.CollectionName,
                    filter: MatchKeyword("resourceId", id)
                );

                if (pointsIds.Count == 0)
                    return Ok(new ApiResponse(true, "No vector points found for resource", Array.Empty<Resource>()));

                // Find similar resources using vector similarity
                // scoreThreshold ensures only resources with similarity >= 0.6 are returned
                IReadOnlyList<PointGroup> results = await ragSystem.QdrantClient.RecommendGroupsAsync(
                    ragSystem.CollectionName,
                    groupBy: "resourceId",
                    positive: pointsIds.Select(p => p.Id).ToArray(),
                    filter: !MatchKeyword("resourceId", id), // Exclude the current resource
                    limit: 5, // Limit to top 5 similar resources
                    scoreThreshold: similarityThreshold
                );

                // Retrieve the actual resource objects
                if (results.Count == 0)
                    return Ok(new ApiResponse(true, "No similar resources found", Array.Empty<Resource>()));

                // Extract resource IDs
                var resourceIds = results.Select(result => Guid.Parse(result.Id.StringValue)).ToList();

                // Retrieve resources based on the found IDs
                var resources = await resourceManager.GetAllResourcesAsync(
                    predicate: r => resourceIds.Contains(r.Id)
                );

                var resourceArray = resources.ToArray();

                // Check if any resources were found
                if (resourceArray.Length == 0)
                    return Ok(new ApiResponse(true, "No similar resources found", Array.Empty<Resource>()));

                return Ok(new ApiResponse(true, "Similar resources found", resourceArray));
            }

            catch (Exception ex)
            {
                logger.Error(ex, "Error retrieving relation 'resource-similar-resources' for resource with ID '{Id}'", id);
                return StatusCode(500, new ApiResponse(false, "Internal Server Error: Similar Resources", ex.Message));
            }
        }


        #endregion
    }
}