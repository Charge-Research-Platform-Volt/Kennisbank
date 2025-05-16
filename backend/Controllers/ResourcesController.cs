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

namespace KnowledgeBank.Controllers 
{


    /// <summary>
    /// This controller is responsible for handling API calls to manage resources and their metadata.
    /// 
    /// Author: Abel Dieterich
    /// </summary>
    /// <param name="resourceManager">The resource manager service for database interactions</param>
    /// <param name="blobService">The Azure Blob Service for file storage</param>
    [ApiController][Route("[controller]")][Produces("application/json")][Authorize]
    public class ResourcesController(ResourceManager resourceManager, IAzureBlobService blobService) : ControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<ResourcesController>();

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
                Guid id = uploadDto.UploadType switch
                {
                    "website" => await resourceManager.CreateWebsiteAsync((WebsiteCreateDto)dto),
                    "document" => await resourceManager.CreateDocumentAsync((DocumentCreateDto)dto),
                    "audio" => await resourceManager.CreateAudioAsync((AudioCreateDto)dto),
                    "video" => await resourceManager.CreateVideoAsync((VideoCreateDto)dto),
                    _ => await resourceManager.CreateResourceAsync(dto)
                };

                // If the resource is a file, upload it to storage
                if (dto is FileResourceCreateDto fDto)
                {
                    // Check if file was empty
                    if (fDto.File == null)
                        return BadRequest(new ApiResponse(false, "No file was uploaded."));

                    // Get the extension and filetype
                    string extension = Path.GetExtension(fDto.File.FileName);
                    string fileType = Filetype.ConvertExtensionToFiletype(extension);

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

            try
            {
                // Check if the resource exists
                if (!await resourceManager.ResourceExistsAsync(id))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));

                logger.Information("Downloding resource with ID: {ID}", id);

                // Get the filetype from the database
                string filetype = await resourceManager.GetResourcePropertyAsync(id, "new(FileType as FileType)");

                // If website, we cannot download, return BadRequest
                if (filetype.Equals("website", StringComparison.CurrentCultureIgnoreCase))
                    return BadRequest(new ApiResponse(false, "Cannot download website."));

                // Try to retrieve the file
                BlobDownloadResponse? maybeResponse = await blobService.DownloadBlobAsync(filetype, id);

                // If response is empty, the file does not exist in storage
                if (maybeResponse == null)
                    return NotFound(new ApiResponse(false, "Resource exists in database, but file could not be found."));

                // Convert to a non-empty response
                BlobDownloadResponse response = (BlobDownloadResponse)maybeResponse;

                // Set the contentType and generate a filename from the title
                string contentType = "application/octet-stream";
                string title = await resourceManager.GetResourcePropertyAsync(id, "new(Title as Title)");
                string extension = response.Metadata["extension"];
                string fileName = SanitizeFileName(title) + extension;

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
                return StatusCode(500, new ApiResponse(false, "Error downloading resource", e.Message));
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
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(Resource), id, updates));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(WebsiteMetadata), id, updates));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(DocumentMetadata), id, updates));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(AudioMetadata), id, updates));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(VideoMetadata), id, updates));

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

        #region Get Relation Information
        /// <summary>
        /// Retrieves a list containing a specific relation (i.g. tags, persons, organisations), containing the id, name/title, and type
        /// </summary>
        /// <param name="id">The guid of the resource</param>
        /// <param name="relation">The relation that is retrieved</param>
        [HttpGet("relation/{id}/{relation}")]
        [SwaggerOperation(Summary = "Retrieves a list containing a specific relation")]
        [SwaggerResponse(200, "A list containing a specific relation", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource not found", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Relation(string id, string relation)
        {

            // Check if the ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID."));

            logger.Information(id);
            logger.Information(relation);

            try
            {
                // Check if the resource exists
                if (!await resourceManager.ResourceExistsAsync(id))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));


                logger.Information("Check succesful");

                object[] relations;
                object[] items;

                //Get all the items in the relation
                switch (relation)
                {
                    case "author":
                        relations = await resourceManager.GetAllResourceAuthorRelationsAsync(predicate: r => r.ResourceId.ToString() == id);
                        items = await GetItems<ResourceAuthorRelation, Person>(relations as ResourceAuthorRelation[], async relation  => await resourceManager.GetPersonAsync(relation.PersonId));

                        break; case "organisation":
                        relations = await resourceManager.GetAllResourceOrganisationRelationsAsync(predicate: r => r.ResourceId.ToString() == id);
                        items = await GetItems<ResourceOrganisationRelation, Organisation>(relations as ResourceOrganisationRelation[], async relation => await resourceManager.GetOrganisationAsync(relation.OrganisationId));

                        break; case "region":
                        relations = await resourceManager.GetAllResourceRegionRelationsAsync(predicate: r => r.ResourceId.ToString() == id);
                        items = await GetItems<ResourceRegionRelation, Region>(relations as ResourceRegionRelation[], async relation => await resourceManager.GetRegionAsync(relation.ResourceId));

                        break; case "relatedOrganisation":
                        relations = await resourceManager.GetAllResourceRelatedOrganisationRelationsAsync(predicate: r => r.ResourceId.ToString() == id);
                        items = await GetItems<ResourceRelatedOrganisationRelation, Organisation>(relations as ResourceRelatedOrganisationRelation[], async relation => await resourceManager.GetOrganisationAsync(relation.OrganisationId));

                        break; case "relatedPerson":
                        relations = await resourceManager.GetAllResourceRelatedPersonRelationsAsync(predicate: r => r.ResourceId.ToString() == id);
                        items = await GetItems<ResourceRelatedPersonRelation, Person>(relations as ResourceRelatedPersonRelation[], async relation => await resourceManager.GetPersonAsync(relation.PersonId));

                        break; case "relatedSource":
                        items = await resourceManager.GetAllResourceRelatedSourceRelationsAsync(predicate: r => r.ResourceId.ToString() == id);

                        break; case "source":
                        items = await resourceManager.GetAllResourceSourceRelationsAsync(predicate: r => r.ResourceId.ToString() == id);

                        break; case "tag":
                        relations = await resourceManager.GetAllResourceTagRelationsAsync(predicate: r => r.ResourceId.ToString() == id);
                        items = await GetItems<ResourceTagRelation, Tag>(relations as ResourceTagRelation[], async relation => await resourceManager.GetTagAsync(relation.TagId));


                        break; default:
                        return BadRequest(new ApiResponse(false, $"Invalid relation: {relation}"));
                }

                object[] results = new object[items.Length];

                logger.Information("Getting relation succesful");

                // Source Dto
                if (relation == "source" || relation == "relatedSourec")
                {
                    results = new object[items.Length];
                    for (int i = 0; i < items.Length; i++)
                    {
                        object resourceDto = DtoGenerator.ToDto(items[i], true, "Url");
                        results[i] = resourceDto;
                    }
                }

                // Dto for every other relation
                else
                {
                    results = new object[items.Length];
                    for (int i = 0; i < items.Length; i++)
                    {
                        object resourceDto = DtoGenerator.ToDto(items[i], true, "Id", "Name");
                        results[i] = resourceDto;
                    }
                }

                // Return the relation
                logger.Information("Resources relation with ID '{ID}' successfully retrieved.", id);
                return Ok(new ApiResponse(true, "Succesfully retrieved relation", results));
            }

            catch (Exception e)
            {
                logger.Error(e, $"Error retrieving relation with ID: {id} and relation: {relation}.", id);
                return StatusCode(500, new ApiResponse(false, "Error retrieving relation", e.Message));
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
        [HttpGet("list")]
        [SwaggerOperation(Summary = "Retrieves a list or page of all resources")]
        [SwaggerResponse(200, "A list or page of all the resources in the archive", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> List(int? pageIndex, int? pageSize, string? properties)
        {
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
                
                // No paging requested, list all resources
                if (pageIndex == null || pageSize == null)
                    resources = string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourcesAsync() :
                        await resourceManager.GetAllResourcesAsync(projection: projectionString);

                // Paging requested, retrieve resources on that page
                else
                    resources = string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetResourcePageAsync((int)pageIndex, (int)pageSize) :
                        await resourceManager.GetResourcePageAsync(projectionString, (int)pageIndex, (int)pageSize);

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

        private async Task<TResult[]>  GetItems<TSource, TResult>(TSource[] relation, Func<TSource, Task<TResult>> getItemFunc)
        {
            TResult[] result = new TResult[relation.Length];
            for (int i = 0; i < relation.Length; i++)
            {
                result[i] = await getItemFunc(relation[i]);
            }
            return result;
        }
        #endregion
    }
}