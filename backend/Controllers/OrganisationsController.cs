// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;

namespace KnowledgeBank.Controllers
{
    /// <summary>
    /// This controller is responsible for handing API calls to manage organisations and their metadata.
    /// 
    /// Author: Abel Dieterich
    /// </summary>
    /// <param name="resourceManager">The resource manager service for database interactions</param>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class OrganisationsController(ResourceManager resourceManager) : ControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<OrganisationsController>();

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
                // Delete organisation
                logger.Information("Deleting organistation with ID: {ID}", id);
                bool found = await resourceManager.DeleteOrganisationAsync(id);

                if (found)
                {
                    logger.Information("Organistaion with ID '{ID}' deleted successfully", id);
                    return Ok(new ApiResponse(true, "Organisation deleted successfully"));
                }

                logger.Information("Organisation with ID '{ID}' not found.", id);
                return NotFound(new ApiResponse(false, "Organisation does not exist"));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error deleting organisation with ID {ID}", id);
                return StatusCode(500, new ApiResponse(false, "Error deleting organisation"));
            }
        }
        #endregion

        #region Update
        /// <summary>
        /// Updates a organistation
        /// </summary>
        /// <param name="id">The ID of the organistation</param>
        /// <param name="updates">The dictionary of propertynames to update and their new values</param>
        [HttpPatch("update/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Updates an organistation")]
        [SwaggerResponse(200, "Organistation updated", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(404, "Organistation not found", typeof(ApiResponse))]
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
                // Check if organistation exsists
                if (!await resourceManager.OrganisationExistsAsync(id))
                    return NotFound(new ApiResponse(false, "The organisation does not exist"));

                // Start a database transaction, since we could be doing multiple updates
                await resourceManager.BeginTransaction();

                // Update the properties
                List<string> updatedProperties = await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(Organisation), id, updates);

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
                    organisationId = await resourceManager.GetOrganisationPropertyOrDefaultAsync(predicate: p => p.Name == name, selector: "Id");


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
                    await resourceManager.GetOrganisationAsync(id, $"new({properties})");

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
        [HttpGet("list")]
        [SwaggerOperation(Summary = "Retrieves a list or page of all organisations")]
        [SwaggerResponse(200, "A list or page of all the organisations in the archive", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> List(int? pageIndex, int? pageSize, string? properties, string? searchQuery)
        {
            // Verification
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

                // No paging requested, list all organisations
                if (pageIndex == null || pageSize == null)
                    organisations = string.IsNullOrEmpty(properties) ?
                        (string.IsNullOrEmpty(searchQuery) ? await resourceManager.GetAllOrganisationsAsync() :
                        await resourceManager.GetAllOrganisationsAsync(predicate: r => r.Name.Contains(searchQuery))) :
                        (string.IsNullOrEmpty(searchQuery) ? await resourceManager.GetAllOrganisationsAsync(projection: projectionString) :
                        await resourceManager.GetAllOrganisationsAsync(projection: projectionString, predicate: r => r.Name.Contains(searchQuery)));

                // Paging requested, retrieve organisations on that page
                else
                    organisations = string.IsNullOrEmpty(properties) ?
                        (string.IsNullOrEmpty(searchQuery) ? await resourceManager.GetOrganisationPageAsync((int)pageIndex, (int)pageSize) :
                        await resourceManager.GetOrganisationPageAsync((int)pageIndex, (int)pageSize, predicate: r => r.Name.Contains(searchQuery))) :
                        (string.IsNullOrEmpty(searchQuery) ? await resourceManager.GetOrganisationPageAsync(projectionString, (int)pageIndex, (int)pageSize) :
                        await resourceManager.GetOrganisationPageAsync(projectionString, (int)pageIndex, (int)pageSize, predicate: r => r.Name.Contains(searchQuery)));

                // Return found organisations
                return Ok(new ApiResponse(true, $"Found {organisations.Length} organisations", organisations));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error listing organisations");
                return StatusCode(500, new ApiResponse(false, "Error listing organisations", e.Message));
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
            await resourceManager.UpdateOrganisationAsync(id, PropertyUpdateUtil.CreatePropertySelector<Organisation, TProperty>(propertyName), newValue);
        }
        #endregion
    }
}
