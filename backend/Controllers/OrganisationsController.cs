using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using KnowledgeBank.Services.Background;
using KnowledgeBank.Services;
using KnowledgeBank.Services.AI;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Controllers
{
    /// <summary>
    /// This controller is responsible for handing API calls to manage organisations and their metadata.
    /// </summary>
    /// <param name="resourceManager">The resource manager service for database interactions</param>
    /// <param name="taskQueue">The background task queue for handling long-running tasks</param>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class OrganisationsController(ResourceManager resourceManager, IBackgroundTaskQueue taskQueue) : ControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<OrganisationsController>();
        private readonly IBackgroundTaskQueue _taskQueue = taskQueue;

        #region New
        /// <summary>
        /// Creates a new organisation
        /// </summary>
        /// <param name="dto">The Data Transfer Object</param>
        [HttpPut("new")]
        [SwaggerOperation(Summary = "Create a new organisation in the archive")]
        [SwaggerResponse(200, "Organistation was created successfully", typeof(ApiResponse))]
        [SwaggerResponse(409, "Organisation already exists", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> New([FromBody] OrganisationCreateDto dto)
        {
            // DTO checks
            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new ApiResponse(false, "No name was given"));

            logger.Information("Creating organisation '{Name}'...", dto.Name);

            try
            {
                // Create the organisation and return the ID
                Guid id = await resourceManager.CreateOrganisationAsync(dto);

                // Add the organisation to the vector database
                _taskQueue.QueueBackgroundWorkItem(async token =>
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
                    await ingestionService.RunOrganisationEntityPipelineAsync(id);
                });

                logger.Information("Organisation '{Name}' created successfully.", dto.Name);
                return Ok(new ApiResponse(true, "Organisation created successfully", id));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error creating organisation '{Name}'.", dto.Name);
                return StatusCode(500, new ApiResponse(false, "Error creating organisation", e.Message));
            }
        }
        #endregion

        #region Trash
        /// <summary>
        /// Trashes an organisation
        /// </summary>
        /// <param name="id">The ID of the organisation</param>
        [HttpPatch("trash/{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Trashes an organisation.")]
        [SwaggerResponse(200, "Organisation trashed successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Organisation not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Trash(string id) 
        {
            // Check if the ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID."));
                
            try 
            {
                // Check if the organisation exists
                if (!await resourceManager.OrganisationExistsAsync(Guid.Parse(id)))
                    return NotFound(new ApiResponse(false, $"Organisation with ID '{id}' does not exist."));

                logger.Information("Trashing organisation with ID: {ID}", id);

                // Trash the organisation
                await resourceManager.TrashOrganisationAsync(Guid.Parse(id));

                logger.Information("Trashed organisation with ID '{ID}' successfully.", id);
                return Ok(new ApiResponse(true, "Organisation trashed successfully."));
            }
            catch (Exception e) 
            {
                logger.Error(e, "Error trashing organisation with ID {ID}.", id);
                return StatusCode(500, new ApiResponse(false, "Error trashing organisation", e.Message));
            }
        }
        #endregion

        #region Untrash
        /// <summary>
        /// Untrashes an organisation
        /// </summary>
        /// <param name="id">The ID of the organisation</param>
        [HttpPatch("untrash/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Untrashes an organisation.")]
        [SwaggerResponse(200, "Organisation untrashed successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Organisation not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Untrash(string id) 
        {
            // Check if the ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID."));
                
            try 
            {
                // Check if the organisation exists
                if (!await resourceManager.OrganisationExistsAsync(Guid.Parse(id)))
                    return NotFound(new ApiResponse(false, $"Organisation with ID '{id}' does not exist."));

                logger.Information("Untrashing organisation with ID: {ID}", id);

                // Untrash the organisation
                await resourceManager.UntrashOrganisationAsync(Guid.Parse(id));

                logger.Information("Untrashed organisation with ID '{ID}' successfully.", id);
                return Ok(new ApiResponse(true, "Organisation untrashed successfully."));
            }
            catch (Exception e) 
            {
                logger.Error(e, "Error untrashing organisation with ID {ID}.", id);
                return StatusCode(500, new ApiResponse(false, "Error untrashing organisation", e.Message));
            }
        }
        #endregion
        
        #region Delete
        /// <summary>
        /// Deletes an organisation
        /// </summary>
        /// <param name="id">The ID of the organisation</param>
        [HttpDelete("delete/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Deletes a person")]
        [SwaggerResponse(200, "Organisation deleted successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Organistation not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Delete(string id)
        {
            // Check if the ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID"));

            try
            {
                logger.Information("Deleting organisation with ID: {ID}", id);

                // Delete organisation from database
                bool organisationFound = await resourceManager.DeleteOrganisationAsync(Guid.Parse(id));

                if (!organisationFound)
                {
                    logger.Information("Organisation with ID '{ID}' not found.", id);
                    return NotFound(new ApiResponse(false, "Organisation does not exist"));
                }

                logger.Information("Organisation with ID '{ID}' deleted successfully", id);
                return Ok(new ApiResponse(true, "Organisation deleted successfully"));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error deleting organisation with ID {ID}", id);
                return StatusCode(500, new ApiResponse(false, "Error deleting organisation"));
            }
        }
        #endregion

        #region Merge
        /// <summary>
        /// Merges the second organisation into the first. All relations are transferred to the first organisation and the second is deleted.
        /// </summary>
        /// <param name="id1">The ID of the organisation to merge into (survivor)</param>
        /// <param name="id2">The ID of the organisation to merge from (will be deleted)</param>
        [HttpPatch("merge/{id1}/{id2}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Merges the second organisation into the first")]
        [SwaggerResponse(200, "Organisations merged successfully", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad request", typeof(ApiResponse))]
        [SwaggerResponse(404, "Organisation(s) not found", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Merge(string id1, string id2)
        {
            if (string.IsNullOrEmpty(id1) || string.IsNullOrEmpty(id2))
                return BadRequest(new ApiResponse(false, "IDs are required."));

            if (id1 == id2)
                return BadRequest(new ApiResponse(false, "Cannot merge an organisation with itself."));

            if (!Guid.TryParse(id1, out Guid orgId1) || !Guid.TryParse(id2, out Guid orgId2))
                return BadRequest(new ApiResponse(false, "Invalid organisation ID format."));

            try
            {
                if (!await resourceManager.OrganisationExistsAsync(orgId1) || !await resourceManager.OrganisationExistsAsync(orgId2))
                    return NotFound(new ApiResponse(false, "One or both organisations were not found."));

                await resourceManager.MergeOrganisationsAsync(orgId1, orgId2);
                return Ok(new ApiResponse(true, "Organisations merged successfully."));
            }
            catch (Exception e)
            {
                await resourceManager.Rollback();
                logger.Error(e, "Error merging organisations {OrgId1} and {OrgId2}", id1, id2);
                return StatusCode(500, new ApiResponse(false, "Internal Server Error", e.Message));
            }
        }
        #endregion

        #region Update
        /// <summary>
        /// Updates a organistation
        /// </summary>
        /// <param name="id">The ID of the organisation</param>
        /// <param name="updates">The dictionary of propertynames to update and their new values</param>
        [HttpPatch("update/{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Updates an organisation")]
        [SwaggerResponse(200, "Organisation updated", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(404, "Organisation not found", typeof(ApiResponse))]
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

            logger.Information("Updating organisation with ID '{ID}'...", id);

            try
            {
                // Check if organisation exsists
                if (!await resourceManager.OrganisationExistsAsync(Guid.Parse(id)))
                    return NotFound(new ApiResponse(false, "The organisation does not exist"));

                // Start a database transaction, since we could be doing multiple updates
                await resourceManager.BeginTransaction();

                // Update the properties (get IngestionService from DI for metadata updates)
                using var scope = HttpContext.RequestServices.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
                List<string> updatedProperties = await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(Organisation), id, updates, ingestionService);

                // No props were found
                if (updatedProperties.Count == 0)
                {
                    await resourceManager.Rollback();
                    return BadRequest(new ApiResponse(false, "None of the props were found."));
                }

                // Commit changes to database
                await resourceManager.Commit();

                // Update embeddings
                Guid organisationGuid = Guid.Parse(id);
                _taskQueue.QueueBackgroundWorkItem(async token =>
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
                    await ingestionService.RunOrganisationEntityPipelineAsync(organisationGuid);
                });

                // Join all updated properties
                string updatedPropertiesString = string.Join(", ", updatedProperties);

                // If all properties were updated
                if (updatedProperties.Count == updates.Count)
                {
                    logger.Information("Successfully updated organisation with ID '{ID}'. Updated properties: {props}", id, updatedPropertiesString);
                    return Ok(new ApiResponse(true, $"Person updated successfully.", updatedProperties));
                }

                // If not all properties were updated
                else
                {
                    logger.Information("Partially updated person with ID '{ID}'. Updated properties: {props}", updatedPropertiesString);
                    return Ok(new ApiResponse(true, $"Person updated partially.", updatedProperties));
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error updating person with ID '{ID}'", id);
                return StatusCode(500, new ApiResponse(false, "Error updating person", e.Message));
            }
        }
        #endregion

        #region Exists
        /// <summary>
        /// Checks if a organisation already exists in the database
        /// </summary>
        /// <param name="name">The name of the organisation</param>
        [EnableCors("AllowFrontend")]
        [HttpGet("exists")]
        [SwaggerOperation(Summary = "Check if an organisation exists")]
        [SwaggerResponse(200, "Response with boolean indicating if organisation exists.", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Exists([FromQuery] string? name)
        {
            // Check for null
            if (string.IsNullOrEmpty(name))
                return BadRequest(new ApiResponse(false, "No value given"));

            try
            {
                // Retrieve the ID of the person if it already exists
                object? organisationId = null;

                // Handle name
                if (!string.IsNullOrEmpty(name))
                    organisationId = await resourceManager.GetOrganisationPropertyOrDefaultAsync(predicate: p => EF.Functions.ILike(p.Name, name), selector: "Id");

                // ID is empty, so no person was found
                if (organisationId == null)
                    return Ok(new ApiResponse(true, "Person does not exist", new { exists = false, id = "" }));

                // ID was not empty, so organisation already exists, return the ID
                return Ok(new ApiResponse(true, "Person already exists", new { exists = true, id = organisationId.ToString() }));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while checking if organisation exists");
                return StatusCode(500, new ApiResponse(false, "Error while checking if organisation exists", e.Message));
            }
        }
        #endregion

        #region Info
        /// <summary>
        /// Gets the information of the organisation (database row)
        /// </summary>
        /// <param name="id">The ID of the organisation</param>
        /// <param name="properties">The properties you are trying to receive, separated by comma</param>
        [HttpGet("info/{id}")]
        [SwaggerOperation(Summary = "Get the information of the organisation")]
        [SwaggerResponse(200, "Organisation information", typeof(ApiResponse))]
        [SwaggerResponse(404, "Organisation Not Found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Info(string id, [FromQuery] string? properties)
        {
            // Check if the ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "ID is invalid"));

            try
            {
                // Retrieve the organisation
                object? organisation = string.IsNullOrEmpty(properties) ?
                    await resourceManager.GetOrganisationAsync(id) :
                    await resourceManager.GetOrganisationPropertyAsync(id, $"new({properties})");

                // If null, the organisation was not found
                if (organisation == null)
                    return NotFound(new ApiResponse(false, "The organisation was not found"));

                // Return the organisation
                return Ok(new ApiResponse(true, "Organisation was found", organisation));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error retrieving organisation info.");
                return StatusCode(500, new ApiResponse(false, "Error retrieving organisation info", e.Message));
            }
        }
        #endregion

        #region List
        /// <summary>
        /// Retrieves a list or page of all organisations
        /// </summary>
        /// <param name="pageIndex">(Optional) The index of the page</param>
        /// <param name="pageSize">(Optional) The size of the page</param>
        /// <param name="properties">(Optional) The properties to select, separated by comma</param>
        /// <param name="searchQuery">(Optional) Filter on search query </param>
        /// <param name="trash">(Optional) Whether to show trashed organisations or non trashed organisations</param>
        [HttpGet("list")]
        [SwaggerOperation(Summary = "Retrieves a list or page of all organisations")]
        [SwaggerResponse(200, "A list or page of all the organisations in the archive", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> List(int? pageIndex, int? pageSize, string? properties, string? searchQuery, bool trash = false)
        {
            // Verification
            if (trash && !User.IsInRole("admin"))
                return Unauthorized(new ApiResponse(false, "You are not authorized to view trashed organisations."));

            if (pageIndex != null && pageIndex < 1)
                return BadRequest(new ApiResponse(false, "Page index cannot be lower than 1"));

            if (pageSize != null && pageSize < 1)
                return BadRequest(new ApiResponse(false, "Page size cannot be lower than 1"));

            // Set defaults
            if (pageIndex != null && pageSize == null) pageSize = 100;
            if (pageSize != null && pageIndex == null) pageIndex = 1;

            try
            {
                // All organisations to be returned
                object[]? organisations = [];

                string projectionString = $"new({properties})";
                
                Expression<Func<Organisation, bool>>? predicate = searchQuery != null ? o =>    (/*EF.Functions.TrigramsAreSimilar(o.Name, searchQuery) || {NOT ALLOWED IN AZURE POSTGRES} */
                                                                                                EF.Functions.ILike(o.Name, $"{searchQuery}%") ||
                                                                                                EF.Functions.ILike(o.Name, $"%{searchQuery}%"))
                                                                                            && o.Trashed == trash
                                                                                  : o => o.Trashed == trash;

                // No paging requested, list all organisations
                if (pageIndex == null || pageSize == null)
                {
                    organisations = string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllOrganisationsAsync(predicate: predicate) :
                        await resourceManager.GetAllOrganisationsAsync(projection: projectionString, predicate: predicate);

                    return Ok(new ApiResponse(true, $"Found {organisations.Length} organisations", new { Items = organisations, PageCount = 1, TotalCount = organisations.Length }));
                }

                // Paging requested, retrieve organisations on that page
                organisations = string.IsNullOrEmpty(properties) ?
                    await resourceManager.GetOrganisationPageAsync((int)pageIndex, (int)pageSize, predicate: predicate) :
                    await resourceManager.GetOrganisationPageAsync(projectionString, (int)pageIndex, (int)pageSize, predicate: predicate);

                int totalOrganisations = (await resourceManager.GetAllOrganisationsAsync(predicate: predicate)).Length;
                int pageCount = (int)Math.Ceiling((double)totalOrganisations / (int)pageSize);

                return Ok(new ApiResponse(true, $"Found {organisations.Length} organisations", new { Items = organisations, PageCount = pageCount, TotalCount = totalOrganisations }));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error listing organisations");
                return StatusCode(500, new ApiResponse(false, "Error listing organisations", e.Message));
            }
        }
        #endregion

        #region Relation fetches
        /// <summary>
        /// Retrieves all relations of the given type for the given organisation ID
        /// </summary>
        /// <param name="relation">The relation to retrieve</param>
        /// <param name="id">The ID of the organisation</param>
        /// <param name="properties">(Optional) The properties to select from the result</param>
        [HttpGet("{id}/relations/{relation}")]
        [SwaggerOperation(Summary = "Retrieves all relations of the given type for the given organisation ID")]
        [SwaggerResponse(200, "The relations", typeof(ApiResponse))]
        [SwaggerResponse(404, "Organisation not found", typeof(ApiResponse))]
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
                object? result = relation switch
                {
                    // Authored resources
                    "authored-resources" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceAuthorRelationsAsync(r => r.AuthorId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceAuthorRelationsAsync(predicate: r => r.AuthorId == Guid.Parse(id), projection: $"new({properties})"),
                
                    // Related resources
                    "related-resources" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceOrganisationRelationsAsync(r => r.OrganisationId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceOrganisationRelationsAsync(predicate: r => r.OrganisationId == Guid.Parse(id), projection: $"new({properties})"),

                    // Related organisations
                    "related-organisations" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllOrganisationRelationshipsAsync(predicate: p => p.SourceOrganisationId == Guid.Parse(id) || p.TargetOrganisationId == Guid.Parse(id)) :
                        await resourceManager.GetAllOrganisationRelationshipsAsync(predicate: p => p.SourceOrganisationId == Guid.Parse(id) || p.TargetOrganisationId == Guid.Parse(id), projection: $"new({properties})"),

                    // Related persons
                    "related-persons" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllPersonOrganisationRelationsAsync(predicate: p => p.OrganisationId == Guid.Parse(id)) :
                        await resourceManager.GetAllPersonOrganisationRelationsAsync(predicate: p => p.OrganisationId == Guid.Parse(id), projection: $"new({properties})"),

                    // Default
                    _ => null
                };

                if (result == null)
                    return NotFound(new ApiResponse(false, "ID or relation not found"));

                return Ok(new ApiResponse(true, "Successfully retrieved relations", result));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error retrieving relation '{Relation}' for organisation with ID '{Id}'", relation, id);
                return StatusCode(500, new ApiResponse(false, "Internal Server Error", e.Message));
            }
        }
        #endregion

        #region Add Relations
        /// <summary>
        /// Adds a relation for this organisation
        /// </summary>
        /// <param name="id">The ID of the organisation</param>
        /// <param name="relation">The relation to be made</param>
        /// <param name="targetId">The ID of the other item in the relation</param>
        /// <param name="relationInfo">(Optional) Extra information over the relation</param>
        [HttpGet("{id}/relations/add/{relation}/{targetId}")]
        [SwaggerOperation(Summary = "Adds a relation to the organisation")]
        [SwaggerResponse(200, "Successfully added relation", typeof(ApiResponse))]
        [SwaggerResponse(404, "Organisation not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> AddRelation(string id, string relation, string targetId, [FromQuery] string? relationInfo)
        {
            // Check if relation is filled in
            if (string.IsNullOrEmpty(relation))
                return BadRequest(new ApiResponse(false, "Invalid relation"));

            // Check if ids are valid
            if (!ValidityUtil.IsValidId(id)) return BadRequest(new ApiResponse(false, "Invalid ID"));
            if (!ValidityUtil.IsValidId(targetId)) return BadRequest(new ApiResponse(false, "Invalid target ID"));

            try
            {
                switch (relation)
                {
                    // Authored resources
                    case "authored-resources":
                        await resourceManager.AddAuthorToResourceAsync(targetId, id);
                        break;

                    // Resources
                    case "related-resources":
                        await resourceManager.AddOrganisationToResourceAsync(targetId, id, relationInfo ?? "");
                        break;

                    // Organisations
                    case "related-organisations":
                        await resourceManager.AddOrganisationRelationshipAsync(Guid.Parse(id), relationInfo, Guid.Parse(targetId));
                        break;

                    // Persons
                    case "related-persons":
                        await resourceManager.AddPersonToOrganisationAsync(targetId, relationInfo, id);
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

        #region Remove Relations
        /// <summary>
        /// Removes a relation for this organisation
        /// </summary>
        /// <param name="id">The ID of the organisation</param>
        /// <param name="relation">The relation to be removed</param>
        /// <param name="targetId">The ID of the other item in the relation</param>
        [HttpGet("{id}/relations/remove/{relation}/{targetId}")]
        [SwaggerOperation(Summary = "Removes a relation to the organisation")]
        [SwaggerResponse(200, "Successfully remoed relation", typeof(ApiResponse))]
        [SwaggerResponse(404, "Organisation not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> RemoveRelation(string id, string relation, string targetId)
        {
            // Check if relation is filled in
            if (string.IsNullOrEmpty(relation))
                return BadRequest(new ApiResponse(false, "Invalid relation"));

            // Check if ids are valid
            if (!ValidityUtil.IsValidId(id)) return BadRequest(new ApiResponse(false, "Invalid ID"));
            if (!ValidityUtil.IsValidId(targetId)) return BadRequest(new ApiResponse(false, "Invalid target ID"));

            try
            {
                switch (relation)
                {
                    // Authored resources
                    case "authored-resources":
                        await resourceManager.RemoveAuthorFromResourceAsync(targetId, id);
                        break;

                    // Resources
                    case "related-resources":
                        await resourceManager.RemoveOrganisationFromResourceAsync(targetId, id);
                        break;

                    // Organisations
                    case "related-organisations":
                        await resourceManager.RemoveOrganisationRelationshipAsync(id, targetId);
                        break;

                    // Persons
                    case "related-persons":
                        await resourceManager.RemovePersonFromOrganisationAsync(targetId, id);
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
        /// Updates the role/relation in a relation between this organisation and another entity
        /// </summary>
        /// <param name="id">The ID of the organisation</param>
        /// <param name="relation">The relation type (related-organisations, related-persons, related-resources)</param>
        /// <param name="targetId">The ID of the related entity</param>
        /// <param name="newRole">The new role/relation value</param>
        [HttpPatch("{id}/relations/update-role/{relation}/{targetId}")]
        [Authorize]
        [SwaggerOperation(Summary = "Updates the role/relation in a relationship")]
        [SwaggerResponse(200, "Role updated successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Organisation or relation not found", typeof(ApiResponse))]
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
                    "organisations",
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
                logger.Error(e, "Error updating role in relation '{Relation}' for organisation with ID '{Id}'", relation, id);
                return StatusCode(500, new ApiResponse(false, "Internal Server Error", e.Message));
            }
        }
        #endregion

        #region Suggestions
        [HttpGet("suggestions")]
        [SwaggerOperation(Summary = "Returns pairs of organisations with similar names as merge suggestions")]
        [SwaggerResponse(200, "List of merge suggestions", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Suggestions([FromQuery] float threshold = 0.6f, [FromQuery] int limit = 20)
        {
            try
            {
                var suggestions = await resourceManager.GetOrganisationMergeSuggestionsAsync(threshold, limit);
                return Ok(new ApiResponse(true, $"Found {suggestions.Count} suggestion(s)", suggestions));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error fetching organisation merge suggestions");
                return StatusCode(500, new ApiResponse(false, "Error fetching organisation merge suggestions", e.Message));
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
            await resourceManager.UpdateOrganisationAsync(Guid.Parse(id), PropertyUpdateUtil.CreatePropertySelector<Organisation, TProperty>(propertyName), newValue);
        }
        #endregion
    }
}
