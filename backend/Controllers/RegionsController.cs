// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//

using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Controllers
{
    /// <summary>
    /// This controller is responsible for handing API calls to manage regions and their metadata.
    ///
    /// Author: Abel Dieterich
    /// </summary>
    /// <param name="resourceManager">The resource manager service for database interactions</param>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class RegionsController(ResourceManager resourceManager) : ControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<RegionsController>();

        #region New
        /// <summary>
        /// Creates a new region
        /// </summary>
        /// <param name="dto">The Data Transfer Object</param>
        [HttpPut("new")]
        [SwaggerOperation(Summary = "Create a new region in the archive")]
        [SwaggerResponse(200, "Region was created successfully", typeof(ApiResponse))]
        [SwaggerResponse(409, "Region already exists", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> New([FromBody] RegionCreateDto dto)
        {
            // DTO checks
            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new ApiResponse(false, "No name was given"));

            logger.Information("Creating region '{Name}'...", dto.Name);

            try
            {
                // Create the region and return the ID
                Guid id = await resourceManager.CreateRegionAsync(dto);

                logger.Information("Region '{Name}' created successfully.", dto.Name);
                return Ok(new ApiResponse(true, "Region created successfully", id));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error creating region '{Name}'.", dto.Name);
                return StatusCode(500, new ApiResponse(false, "Error creating region", e.Message));
            }

        }
        #endregion

        #region Delete
        /// <summary>
        /// Deletes a region
        /// </summary>
        /// <param name="id">The ID of the region</param>
        [HttpDelete("delete/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Deletes a region")]
        [SwaggerResponse(200, "Region deleted successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Region not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Delete(string id)
        {
            // Check if the ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID"));

            try
            {
                // Delete person
                logger.Information("Deleting region with ID: {ID}", id);
                bool found = await resourceManager.DeleteRegionAsync(Guid.Parse(id));

                if (found)
                {
                    logger.Information("Region with ID '{ID}' deleted successfully", id);
                    return Ok(new ApiResponse(true, "Region deleted successfully"));
                }

                logger.Information("Region with ID '{ID}' not found", id);
                return NotFound(new ApiResponse(false, "Region does not exist"));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error deleting region with ID '{ID}'", id);
                return StatusCode(500, new ApiResponse(false, "Error deleting region"));
            }
        }
        #endregion

        #region Update
        /// <summary>
        /// Updates a region
        /// </summary>
        /// <param name="id">The ID of the region</param>
        /// <param name="updates">The dictionary of propertynames to update and their new values</param>
        [HttpPatch("update/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Updates a person")]
        [SwaggerResponse(200, "Region updated", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(404, "Region not found", typeof(ApiResponse))]
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

            logger.Information("Updating person with ID '{ID}'...", id);

            try
            {
                // Check if region exists
                if (!await resourceManager.RegionExistsAsync(Guid.Parse(id)))
                    return NotFound(new ApiResponse(false, "The region does not exist"));

                // Start a database transaction, since we could be doing multiple updates
                await resourceManager.BeginTransaction();

                // Update the properties (regions don't have vector embeddings, so no ragManager needed)
                List<string> updatedProperties = await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(Region), id, updates);

                // No props were found
                if (updatedProperties.Count == 0)
                {
                    await resourceManager.Rollback();
                    return BadRequest(new ApiResponse(false, "None of the props were found"));
                }

                // Commit changes to database
                await resourceManager.Commit();

                // Join all updated properties
                string updatedPropertiesString = string.Join(", ", updatedProperties);

                // If all properties were updated
                if (updatedProperties.Count == updates.Count)
                {
                    logger.Information("Successfully updated region with ID '{ID}'. Updated properties: {props}", id, updatedPropertiesString);
                    return Ok(new ApiResponse(true, "Region updated successfully", updatedProperties));
                }

                // If not all properties were updated
                else
                {
                    logger.Information("Partially updated region with ID '{ID}'. Updated properties: {props}", id, updatedPropertiesString);
                    return Ok(new ApiResponse(true, "Region updated partially", updatedProperties));
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error updating region with ID '{ID}'", id);
                return StatusCode(500, new ApiResponse(false, "Error updating region", e.Message));
            }
        }
        #endregion

        #region Exists
        /// <summary>
        /// Checks if a region already exists in the database
        /// </summary>
        /// <param name="name">The name of the region</param>
        [EnableCors("AllowFrontend")]
        [HttpGet("exists")]
        [SwaggerOperation(Summary = "Check if a region exists")]
        [SwaggerResponse(200, "Response with boolean indicating if region exists.", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Exists([FromQuery] string? name)
        {
            // Check for null
            if (string.IsNullOrEmpty(name))
                return BadRequest(new ApiResponse(false, "No value given"));

            try
            {
                // Retrieve the ID of the region if it already exists
                object? regionId = null;

                // Handle name
                if (!string.IsNullOrEmpty(name))
                    regionId = await resourceManager.GetRegionPropertyOrDefaultAsync(predicate: r => r.Name == name, selector: "Id");



                // ID is empty, so no region was found
                if (regionId == null)
                    return Ok(new ApiResponse(true, "Region does not exist", new { exists = false, id = "" }));

                // ID was not empty, so region already exists, return the ID
                return Ok(new ApiResponse(true, "Region already exists.", new { exists = true, id = regionId.ToString() }));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while checking if region exists");
                return StatusCode(500, new ApiResponse(false, "Error while checking if region exists", e.Message));
            }
        }
        #endregion

        #region Info
        /// <summary>
        /// Gets the information of the region (database row)
        /// </summary>
        /// <param name="id">The ID of the region</param>
        /// <param name="properties">The properties you are trying to receive, separated by comma</param>
        [HttpGet("info/{id}")]
        [SwaggerOperation(Summary = "Get the information of the region")]
        [SwaggerResponse(200, "Region information", typeof(ApiResponse))]
        [SwaggerResponse(404, "Region Not Found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Info(string id, [FromQuery] string? properties)
        {
            // Check if ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "ID is invalid"));

            try
            {
                // Retrieve the region
                object? region = string.IsNullOrEmpty(properties) ?
                    await resourceManager.GetRegionAsync(id) :
                    await resourceManager.GetRegionPropertyAsync(id, $"new({properties})");

                // If null, the region was not found
                if (region == null)
                    return NotFound(new ApiResponse(false, "The region does not exist"));

                // Return the region
                return Ok(new ApiResponse(true, "Region was found", region));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error retrieving region info.");
                return StatusCode(500, new ApiResponse(false, "Error retrieving region info", e.Message));
            }
        }
        #endregion

        #region List
        /// <summary>
        /// Retrieves a list or page of all regions
        /// </summary>
        /// <param name="pageIndex">(Optional) The index of the page</param>
        /// <param name="pageSize">(Optional) The size of the page</param>
        /// <param name="properties">(Optional) The properties to select, separated by comma</param>
        /// <param name="searchQuery">(Optional) Filter on search query </param>
        [HttpGet("list")]
        [SwaggerOperation(Summary = "Retrieves a list or page of all regions")]
        [SwaggerResponse(200, "A list or page of all the regions in the archive", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> List(int? pageIndex, int? pageSize, string? properties, string? searchQuery)
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
                // All regions to be returned
                object[] regions = [];

                string projectionString = $"new({properties})";

                Expression<Func<Region, bool>>? predicate = searchQuery != null ? r =>  /*EF.Functions.TrigramsAreSimilar(r.Name, searchQuery) || {NOT ALLOWED IN AZURE POSTGRES} */
                                                                                        EF.Functions.ILike(r.Name, $"{searchQuery}%") ||
                                                                                        EF.Functions.ILike(r.Name, $"%{searchQuery}%") : null;

                // No paging requested, list all regions
                if (pageIndex == null || pageSize == null)
                    regions = string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllRegionsAsync(predicate: predicate) :
                        await resourceManager.GetAllRegionsAsync(projection: projectionString, predicate: predicate);

                // Paging requested, retrieve regions on that page
                else
                    regions = string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetRegionPageAsync((int)pageIndex, (int)pageSize, predicate: predicate) :
                        await resourceManager.GetRegionPageAsync(projectionString, (int)pageIndex, (int)pageSize, predicate: predicate);

                // Return found regions
                return Ok(new ApiResponse(true, $"Found {regions.Length} regions", regions));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error listing regions");
                return StatusCode(500, new ApiResponse(false, "Error listing regions", e.Message));
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
            await resourceManager.UpdateRegionAsync(Guid.Parse(id), PropertyUpdateUtil.CreatePropertySelector<Region, TProperty>(propertyName), newValue);
        }
        #endregion
    }
}
