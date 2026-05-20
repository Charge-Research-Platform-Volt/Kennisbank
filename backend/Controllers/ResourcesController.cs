using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using KnowledgeBank.Services.Background;
using KnowledgeBank.Services.AI;
using System.Linq.Expressions;
using Microsoft.Extensions.AI;
using System.Security.Claims;
using KnowledgeBank.Services.Vector;
using KnowledgeBank.Services.Storage;



namespace KnowledgeBank.Controllers
{


    /// <summary>
    /// This controller is responsible for handling API calls to manage resources and their metadata.
    ///
    /// </summary>
    /// <param name="resourceManager">The resource manager service for database interactions</param>
    /// <param name="storageService">The storage service for file storage</param>
    /// <param name="taskQueue">The background task queue for processing tasks asynchronously</param>
    /// <param name="ingestionService">The RAG manager for metadata updates and query processing</param>
    /// <param name="serviceScopeFactory">The service scope factory for creating service scopes in background tasks</param>
    /// <param name="vectorStore">The vector store for handling vector database interactions</param>
    /// <param name="environmentConfig">The environment configuration containing necessary settings</param>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ResourcesController(ResourceManager resourceManager, IStorageService storageService, IBackgroundTaskQueue taskQueue, IngestionService ingestionService, IServiceScopeFactory serviceScopeFactory, IVectorStore vectorStore, EnvironmentConfig environmentConfig) : ControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<ResourcesController>();
        private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

        #region New
        [HttpPut("new")]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Create a new resource in the archive")]
        [SwaggerResponse(200, "Resource was created successfully", typeof(ApiResponse))]
        [SwaggerResponse(409, "Resource already exists", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> New([FromBody] ResourceCreateDto dto) 
        {
            string uploadType = dto switch
            {
                WebsiteCreateDto => "website",
                DocumentCreateDto => "document",
                AudioCreateDto => "audio",
                VideoCreateDto => "video",
                _ => "unknown"
            };

            if (uploadType == "unknown")
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
            
            try 
            {
                // Checks for file
                if (dto is FileResourceCreateDto _fDto) 
                {
                    // Check if the given ID is valid
                    if (!ValidityUtil.IsValidId(_fDto.Id))
                        return BadRequest(new ApiResponse(false, "Invalid ID given."));

                    IDictionary<string, string>? metadata = await storageService.GetObjectMetadataAsync(bucketName, _fDto.Id);

                    if (metadata == null)
                        return BadRequest(new ApiResponse(false, $"There is no file for the given ID '{_fDto.Id}'"));

                    if (!metadata.TryGetValue("extension", out string? extension) || string.IsNullOrEmpty(extension))
                        return BadRequest(new ApiResponse(false, "File metadata is missing extension."));
                        
                    _fDto.FileExtension = extension;
                }

                logger.Information("Creating resource '{Title}'...", dto.Title);

                List<Func<CancellationToken, Task>> backgroundTasks = [];
                // Track entities created in this request to avoid duplicates within the same transaction
                Dictionary<string, Guid> createdOrganisations = [];
                Dictionary<string, Guid> createdPersons = [];
                // Start transaction on the database
                await resourceManager.BeginTransaction();
                
                // First check if we need to create any persons / organisations in the author list that don't exist yet
                for (int i = 0; i < dto.Authors.Length; i++)
                {
                    // If valid ID nothing needs to be done.
                    if (ValidityUtil.IsValidId(dto.Authors[i].Value))
                        continue;

                    // Not a valid ID, so create entity based on type
                    string name = dto.Authors[i].Value;
                    string? type = dto.Authors[i].Type?.ToLowerInvariant();
                    Guid entityId;

                    if (type == "organisation")
                    {
                        if (createdOrganisations.TryGetValue(name, out Guid cachedOrgId))
                        {
                            entityId = cachedOrgId;
                        }
                        else
                        {
                            Organisation? existingOrg = await resourceManager.GetOrganisationAsync(o => o.Name == name);
                            if (existingOrg != null)
                            {
                                entityId = existingOrg.Id;
                                logger.Information("Reusing existing organisation '{Name}' with ID {Id}", name, entityId);
                            }
                            else
                            {
                                entityId = await resourceManager.CreateOrganisationAsync(new OrganisationCreateDto { Name = name });
                                logger.Information("Created new organisation '{Name}' with ID {Id}", name, entityId);
                                backgroundTasks.Add(async token =>
                                {
                                    using var scope = serviceScopeFactory.CreateScope();
                                    IngestionService rag = scope.ServiceProvider.GetRequiredService<IngestionService>();
                                    await rag.RunOrganisationEntityPipelineAsync(entityId);
                                });
                            }
                            createdOrganisations[name] = entityId;
                        }
                    }
                    else
                    {
                        // Default to person (includes when type is null, "person", or any other value)
                        if (createdPersons.TryGetValue(name, out Guid cachedPersonId))
                        {
                            entityId = cachedPersonId;
                        }
                        else
                        {
                            Person? existingPerson = await resourceManager.GetPersonAsync(p => p.Name == name);
                            if (existingPerson != null)
                            {
                                entityId = existingPerson.Id;
                                logger.Information("Reusing existing person '{Name}' with ID {Id}", name, entityId);
                            }
                            else
                            {
                                entityId = await resourceManager.CreatePersonAsync(new PersonCreateDto { Name = name });
                                logger.Information("Created new person '{Name}' with ID {Id}", name, entityId);
                                backgroundTasks.Add(async token =>
                                {
                                    using var scope = serviceScopeFactory.CreateScope();
                                    IngestionService rag = scope.ServiceProvider.GetRequiredService<IngestionService>();
                                    await rag.RunPersonEntityPipelineAsync(entityId);
                                });
                            }
                            createdPersons[name] = entityId;
                        }
                    }

                    dto.Authors[i].Value = entityId.ToString();
                }
                
                // Check if we need to create any organisations that don't exist yet
                for (int i = 0; i < dto.Organisations.Length; i++)
                {
                    // If valid ID nothing needs to be done.
                    if (ValidityUtil.IsValidId(dto.Organisations[i].Id))
                        continue;

                    string name = dto.Organisations[i].Id;
                    if (createdOrganisations.TryGetValue(name, out Guid cachedOrgId))
                    {
                        dto.Organisations[i].Id = cachedOrgId.ToString();
                    }
                    else
                    {
                        Organisation? existingOrg = await resourceManager.GetOrganisationAsync(o => o.Name == name);
                        Guid oId;
                        if (existingOrg != null)
                        {
                            oId = existingOrg.Id;
                            logger.Information("Reusing existing organisation '{Name}' with ID {Id}", name, oId);
                        }
                        else
                        {
                            oId = await resourceManager.CreateOrganisationAsync(new OrganisationCreateDto { Name = name });
                            logger.Information("Created new organisation '{Name}' with ID {Id}", name, oId);
                            backgroundTasks.Add(async token =>
                            {
                                using var scope = serviceScopeFactory.CreateScope();
                                IngestionService rag = scope.ServiceProvider.GetRequiredService<IngestionService>();
                                await rag.RunOrganisationEntityPipelineAsync(oId);
                            });
                        }
                        createdOrganisations[name] = oId;
                        dto.Organisations[i].Id = oId.ToString();
                    }
                }

                // Check if we need to create any related persons that don't exist yet
                for (int i = 0; i < dto.RelatedPersons.Length; i++)
                {
                    // If valid ID nothing needs to be done.
                    if (ValidityUtil.IsValidId(dto.RelatedPersons[i].Id))
                        continue;

                    string name = dto.RelatedPersons[i].Id;
                    if (createdPersons.TryGetValue(name, out Guid cachedPersonId))
                    {
                        dto.RelatedPersons[i].Id = cachedPersonId.ToString();
                    }
                    else
                    {
                        Person? existingPerson = await resourceManager.GetPersonAsync(p => p.Name == name);
                        Guid pId;
                        if (existingPerson != null)
                        {
                            pId = existingPerson.Id;
                            logger.Information("Reusing existing person '{Name}' with ID {Id}", name, pId);
                        }
                        else
                        {
                            pId = await resourceManager.CreatePersonAsync(new PersonCreateDto { Name = name });
                            logger.Information("Created new person '{Name}' with ID {Id}", name, pId);
                            backgroundTasks.Add(async token =>
                            {
                                using var scope = serviceScopeFactory.CreateScope();
                                IngestionService rag = scope.ServiceProvider.GetRequiredService<IngestionService>();
                                await rag.RunPersonEntityPipelineAsync(pId);
                            });
                        }
                        createdPersons[name] = pId;
                        dto.RelatedPersons[i].Id = pId.ToString();
                    }
                }

                // Check if we need to create any tags that don't exist yet
                for (int i = 0; i < dto.Tags.Length; i++)
                {
                    // If valid ID nothing needs to be done.
                    if (ValidityUtil.IsValidId(dto.Tags[i]))
                        continue;

                    // Not a valid ID, so it's a tag name - find or create the tag
                    string tagName = dto.Tags[i];

                    // Try to find existing tag by name (query database directly)
                    Tag? existingTag = await resourceManager.GetTagAsync(t => t.Name == tagName);

                    if (existingTag != null)
                    {
                        // Tag exists, use its ID
                        dto.Tags[i] = existingTag.Id.ToString();
                    }
                    else
                    {
                        // Tag doesn't exist, create it
                        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        Guid tagId = await resourceManager.CreateTagAsync(new TagCreateDto
                        {
                            Name = tagName,
                            // CreatedBy = userId ?? string.Empty
                        });
                        dto.Tags[i] = tagId.ToString();
                    }
                }

                // Create resource in the database and retrieve the ID
                Guid id = Guid.Empty;
                switch (dto) 
                {
                    // Website creation
                    case WebsiteCreateDto wDto:
                        // If the URL points directly to a supported file, download and store it as a document
                        string wUrlPath = new Uri(wDto.Url).LocalPath.TrimEnd('/');
                        string wUrlExt = Path.GetExtension(wUrlPath); // e.g. ".pdf"
                        string? directFileExt = (!string.IsNullOrEmpty(wUrlExt) && Filetype.SupportedText(wUrlExt)) ? wUrlExt : null;

                        if (directFileExt != null)
                        {
                            Guid fileId = Guid.NewGuid();
                            using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
                            using HttpResponseMessage httpFileResponse = await httpClient.GetAsync(wDto.Url);
                            httpFileResponse.EnsureSuccessStatusCode();

                            using MemoryStream fileMemStream = new();
                            using Stream downloadStream = await httpFileResponse.Content.ReadAsStreamAsync();
                            await downloadStream.CopyToAsync(fileMemStream);
                            fileMemStream.Position = 0;

                            string fileHash = Convert.ToHexString(
                                System.Security.Cryptography.SHA256.HashData(fileMemStream.ToArray())
                            ).ToLowerInvariant();
                            fileMemStream.Position = 0;

                            await storageService.UploadObjectAsync(
                                bucketName,
                                fileId.ToString(),
                                fileMemStream,
                                new Dictionary<string, string>
                                {
                                    { "extension", directFileExt },
                                    { "originalFileName", Uri.EscapeDataString(Path.GetFileName(wUrlPath)) }
                                }
                            );

                            DocumentCreateDto docDto = new()
                            {
                                Id = fileId.ToString(),
                                FileExtension = directFileExt,
                                Hash = fileHash,
                                Title = wDto.Title,
                                Description = wDto.Description,
                                TypeId = wDto.TypeId,
                                LanguageCode = wDto.LanguageCode,
                                PublicationCode = wDto.PublicationCode,
                                PublicationDate = wDto.PublicationDate,
                                PublicationDatePrecision = wDto.PublicationDatePrecision,
                                License = wDto.License,
                                Note = wDto.Note,
                                SourceUrl = wDto.Url,
                                Tags = wDto.Tags,
                                Authors = wDto.Authors,
                                Organisations = wDto.Organisations,
                                Regions = wDto.Regions,
                                RelatedPersons = wDto.RelatedPersons,
                            };

                            id = await resourceManager.CreateDocumentAsync(docDto);
                            backgroundTasks.Add(async token =>
                            {
                                using var scope = serviceScopeFactory.CreateScope();
                                IngestionService rag = scope.ServiceProvider.GetRequiredService<IngestionService>();
                                IStorageService storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
                                ObjectDownloadResponse dlResponse = await storage.DownloadObjectAsync(bucketName, id.ToString());
                                await using var dlStream = dlResponse.Stream;
                                await rag.RunResourcePipelineAsync(id: id, fileType: docDto.FileExtension, fileStream: dlStream);
                            });
                        }
                        else
                        {
                            id = await resourceManager.CreateWebsiteAsync(wDto);
                            backgroundTasks.Add(async token =>
                            {
                                using var scope = serviceScopeFactory.CreateScope();
                                IngestionService rag = scope.ServiceProvider.GetRequiredService<IngestionService>();
                                await rag.RunResourcePipelineAsync(id: id);
                            });
                        }
                        break;
                    
                    // Document creation
                    case DocumentCreateDto dDto:
                        id = await resourceManager.CreateDocumentAsync(dDto);
                        backgroundTasks.Add(async token =>
                        {
                            using var scope = serviceScopeFactory.CreateScope();
                            IngestionService rag = scope.ServiceProvider.GetRequiredService<IngestionService>();
                            IStorageService storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

                            ObjectDownloadResponse downloadResponse = await storage.DownloadObjectAsync(bucketName, id.ToString());
                            await using var fileStream = downloadResponse.Stream;

                            await rag.RunResourcePipelineAsync(id: id, fileType: dDto.FileExtension, fileStream: fileStream);
                        });
                        break;
                    
                    // Audio creation
                    case AudioCreateDto aDto:
                        id = await resourceManager.CreateAudioAsync(aDto);
                        backgroundTasks.Add(async token =>
                        {
                            using var scope = serviceScopeFactory.CreateScope();
                            IngestionService rag = scope.ServiceProvider.GetRequiredService<IngestionService>();

                            await rag.RunResourcePipelineAsync(id: id);
                        });
                        break;
                    
                    // Video creation
                    case VideoCreateDto vDto:
                        id = await resourceManager.CreateVideoAsync(vDto);
                        backgroundTasks.Add(async token =>
                        {
                            using var scope = serviceScopeFactory.CreateScope();
                            IngestionService rag = scope.ServiceProvider.GetRequiredService<IngestionService>();

                            await rag.RunResourcePipelineAsync(id: id);
                        });
                        break;
                }

                // Commit changes to the database and return success response
                await resourceManager.Commit();

                // Start background tasks
                foreach (var task in backgroundTasks)
                    taskQueue.QueueBackgroundWorkItem(task);
                
                logger.Information("Resource '{Title}' created successfully.", dto.Title);
                return Ok(new ApiResponse(true, "Resource created successfully", id));
            }
            catch (Exception e) 
            {
                logger.Error(e, "Error creating resource '{Title}'", dto.Title);
                await resourceManager.Rollback();
                return StatusCode(500, new ApiResponse(false, e.Message));
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
                if (!await resourceManager.ResourceExistsAsync(Guid.Parse(id)))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));

                logger.Information("Trashing resource with ID: {ID}", id);

                // Trash the resource
                await resourceManager.TrashResourceAsync(Guid.Parse(id));

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
                if (!await resourceManager.ResourceExistsAsync(Guid.Parse(id)))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));

                logger.Information("Untrashing resource with ID: {ID}", id);

                // Untrash the resource
                await resourceManager.UntrashResourceAsync(Guid.Parse(id));

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
                if (!await resourceManager.ResourceExistsAsync(Guid.Parse(id)))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));

                logger.Information("Deleting resource with ID: {ID}", id);

                // Get the filetype of the resource
                string filetype = await resourceManager.GetResourcePropertyAsync(id, "FileType");

                // Delete the file from storage (ignore if not found - might be a website or already deleted)
                try
                {
                    await storageService.DeleteObjectAsync(filetype, id);
                }
                catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    logger.Information("File {Id} not in storage, continuing with database deletion", id);
                }

                // Delete the resource from the database
                await resourceManager.DeleteResourceAsync(Guid.Parse(id));

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
                if (!await resourceManager.ResourceExistsAsync(Guid.Parse(id)))
                    return NotFound(new ApiResponse(false, "The resource does not exist"));

                // Replace null/empty typeId with the Unknown resource type
                if (updates.TryGetValue("typeId", out var typeIdValue) && (typeIdValue == null || string.IsNullOrWhiteSpace(typeIdValue.ToString())))
                    updates["typeId"] = DatabaseContext.UnknownResourceTypeId;

                // Start a database transaction, since we could have multiple updates
                await resourceManager.BeginTransaction();

                List<string> updatedProperties = [];

                // Update the properties (pass ingestionService for rich metadata updates)
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(Resource), id, updates, ingestionService));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(WebsiteMetadata), id, updates, ingestionService));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(DocumentMetadata), id, updates, ingestionService));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(AudioMetadata), id, updates, ingestionService));
                updatedProperties.AddRange(await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(VideoMetadata), id, updates, ingestionService));

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

                // Handle URL — check website metadata first, then resource SourceUrl (for file-URL documents)
                else if (!string.IsNullOrEmpty(url))
                {
                    resourceId = await resourceManager.GetWebsiteMetadataPropertyOrDefaultAsync(predicate: m => m.Url == url, selector: "ResourceId");
                    resourceId ??= await resourceManager.GetResourcePropertyOrDefaultAsync(predicate: r => r.SourceUrl == url, selector: "Id");
                }



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
                 
                Expression<Func<Resource, bool>>? predicate = searchQuery != null ? r =>    (EF.Functions.TrigramsAreSimilar(r.Title, searchQuery) ||
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
        public async Task<IActionResult> TypesFetch([FromQuery] string? search = null)
        {
            try
            {
                // Fetch the resource types, optionally filtered by name (exclude the Unknown fallback type)
                var unknownTypeId = Guid.Parse(DatabaseContext.UnknownResourceTypeId);
                ResourceType[] types = string.IsNullOrWhiteSpace(search)
                    ? await resourceManager.GetAllResourceTypesAsync(predicate: t => t.Id != unknownTypeId)
                    : await resourceManager.GetAllResourceTypesAsync(predicate: t => t.Id != unknownTypeId && EF.Functions.ILike(t.Name, $"%{search}%"));

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

        #region Types Delete
        /// <summary>
        /// Deletes a resource type. All resources of this type will be reassigned to the unknown type.
        /// </summary>
        /// <param name="id">The ID of the resource type to delete</param>
        [HttpDelete("types/delete/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Deletes a resource type")]
        [SwaggerResponse(200, "Resource type deleted successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource type not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> TypesDelete(string id)
        {
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID"));

            if (!Guid.TryParse(id, out Guid typeId))
                return BadRequest(new ApiResponse(false, "Invalid ID format."));

            try
            {
                if (!await resourceManager.ResourceTypeExistsAsync(typeId))
                    return NotFound(new ApiResponse(false, "Resource type not found."));

                logger.Information("Deleting resource type with ID '{ID}'", id);

                bool deleted = await resourceManager.DeleteResourceTypeAsync(typeId);

                if (!deleted)
                    return NotFound(new ApiResponse(false, "Resource type not found."));

                logger.Information("Resource type with ID '{ID}' deleted successfully", id);
                return Ok(new ApiResponse(true, "Resource type deleted successfully."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error deleting resource type with ID '{ID}'", id);
                return StatusCode(500, new ApiResponse(false, "Error deleting resource type.", e.Message));
            }
        }
        #endregion

        #region Types Rename
        /// <summary>
        /// Renames a resource type
        /// </summary>
        /// <param name="id">The ID of the resource type to rename</param>
        /// <param name="dto">DTO containing the new name</param>
        [HttpPatch("types/rename/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Renames a resource type")]
        [SwaggerResponse(200, "Resource type renamed successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource type not found", typeof(ApiResponse))]
        [SwaggerResponse(409, "Name already in use", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> TypesRename(string id, [FromBody] ResourceTypeCreateDto dto)
        {
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID"));

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new ApiResponse(false, "Name is required."));

            if (!Guid.TryParse(id, out Guid typeId))
                return BadRequest(new ApiResponse(false, "Invalid ID format."));

            try
            {
                if (!await resourceManager.ResourceTypeExistsAsync(typeId))
                    return NotFound(new ApiResponse(false, "Resource type not found."));

                if (await resourceManager.ResourceTypeExistsAsync(rt => rt.Name == dto.Name && rt.Id != typeId))
                    return Conflict(new ApiResponse(false, "A resource type with that name already exists."));

                logger.Information("Renaming resource type '{ID}' to '{Name}'", id, dto.Name);

                await resourceManager.UpdateResourceTypeAsync(typeId, rt => rt.Name, dto.Name);

                logger.Information("Resource type '{ID}' renamed to '{Name}' successfully", id, dto.Name);
                return Ok(new ApiResponse(true, "Resource type renamed successfully."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error renaming resource type '{ID}'", id);
                return StatusCode(500, new ApiResponse(false, "Error renaming resource type.", e.Message));
            }
        }
        #endregion

        #region Types Merge
        /// <summary>
        /// Merges the second resource type into the first. All resources of the second type are
        /// reassigned to the first, and the second type is deleted.
        /// </summary>
        /// <param name="id1">The ID of the resource type to merge into</param>
        /// <param name="id2">The ID of the resource type to merge from (will be deleted)</param>
        [HttpPatch("types/merge/{id1}/{id2}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Merges the second resource type into the first")]
        [SwaggerResponse(200, "Resource types merged successfully", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad request", typeof(ApiResponse))]
        [SwaggerResponse(404, "Resource type(s) not found", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> TypesMerge(string id1, string id2)
        {
            if (string.IsNullOrEmpty(id1) || string.IsNullOrEmpty(id2))
                return BadRequest(new ApiResponse(false, "IDs are required."));

            if (id1 == id2)
                return BadRequest(new ApiResponse(false, "Cannot merge a resource type with itself."));

            if (!Guid.TryParse(id1, out Guid typeId1) || !Guid.TryParse(id2, out Guid typeId2))
                return BadRequest(new ApiResponse(false, "Invalid resource type ID format."));

            try
            {
                if (!await resourceManager.ResourceTypeExistsAsync(typeId1) || !await resourceManager.ResourceTypeExistsAsync(typeId2))
                    return NotFound(new ApiResponse(false, "One or both resource types were not found."));

                logger.Information("Merging resource type '{ID2}' into '{ID1}'", id2, id1);

                bool merged = await resourceManager.MergeResourceTypeAsync(typeId1, typeId2);

                if (!merged)
                    return StatusCode(500, new ApiResponse(false, "Failed to merge resource types."));

                logger.Information("Resource type '{ID2}' merged into '{ID1}' successfully", id2, id1);
                return Ok(new ApiResponse(true, "Resource types merged successfully."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error merging resource types '{ID1}' and '{ID2}'", id1, id2);
                return StatusCode(500, new ApiResponse(false, "Internal Server Error"));
            }
        }
        #endregion

        #region Types Suggestions
        /// <summary>
        /// Returns pairs of resource types with similar names that may be candidates for merging.
        /// </summary>
        /// <param name="threshold">Trigram similarity threshold (0–1, default 0.6)</param>
        /// <param name="limit">Maximum number of suggestions to return (default 20)</param>
        [HttpGet("types/suggestions")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Returns resource type pairs that are candidates for merging based on name similarity")]
        [SwaggerResponse(200, "List of merge suggestions", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> TypesSuggestions([FromQuery] float threshold = 0.6f, [FromQuery] int limit = 20)
        {
            try
            {
                var suggestions = await resourceManager.GetResourceTypeMergeSuggestionsAsync(threshold, limit);
                return Ok(new ApiResponse(true, $"Found {suggestions.Count} suggestion(s)", suggestions));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error fetching resource type merge suggestions");
                return StatusCode(500, new ApiResponse(false, "Error fetching resource type merge suggestions", e.Message));
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
        
        // Helper method to update a property
        private async Task UpdateProperty<TSet, TProperty>(string id, string propertyName, TProperty newValue) where TSet : class
        {
            Type setType = typeof(TSet);

            // Update the appropiate property based on the type
            await (setType switch
            {
                // If type is Resource
                Type t when t == typeof(Resource) => resourceManager.UpdateResourceAsync(Guid.Parse(id), PropertyUpdateUtil.CreatePropertySelector<Resource, TProperty>(propertyName), newValue),

                // If type is WebsiteMetadata
                Type t when t == typeof(WebsiteMetadata) => resourceManager.UpdateWebsiteMetadataAsync(Guid.Parse(id), PropertyUpdateUtil.CreatePropertySelector<WebsiteMetadata, TProperty>(propertyName), newValue),

                // If type is DocumentMetadata
                Type t when t == typeof(DocumentMetadata) => resourceManager.UpdateDocumentMetadataAsync(Guid.Parse(id), PropertyUpdateUtil.CreatePropertySelector<DocumentMetadata, TProperty>(propertyName), newValue),

                // If type is VideoMetadata
                Type t when t == typeof(VideoMetadata) => resourceManager.UpdateVideoMetadataAsync(Guid.Parse(id), PropertyUpdateUtil.CreatePropertySelector<VideoMetadata, TProperty>(propertyName), newValue),

                // If type is AudioMetadata
                Type t when t == typeof(AudioMetadata) => resourceManager.UpdateAudioMetadataAsync(Guid.Parse(id), PropertyUpdateUtil.CreatePropertySelector<AudioMetadata, TProperty>(propertyName), newValue),

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
                if (!await resourceManager.ResourceExistsAsync(Guid.Parse(id)))
                    return NotFound(new ApiResponse(false, $"Resource with ID '{id}' does not exist."));

                // Get vector points for the current resource
                var results = await vectorStore.RecommendSimilarAsync(Guid.Parse(id), limit: 5, scoreThreshold: similarityThreshold);

                if (results.Count == 0)
                    return Ok(new ApiResponse(true, "No similar resources found", Array.Empty<Resource>()));

                // Extract unique resource IDs
                var resourceIds = results.Select(r => r.ResourceId).Distinct().ToList();

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