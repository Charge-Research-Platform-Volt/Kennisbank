// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

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
using KnowledgeBank.BackgroundServices;
using KnowledgeBank.Services;

namespace KnowledgeBank.Controllers
{
    /// <summary>
    /// This controller is responsible for handing API calls to manage persons and their metadata.
    /// 
    /// Author: Abel Dieterich
    /// </summary>
    /// <param name="resourceManager">The resource manager service for database interactions</param>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class PersonsController(ResourceManager resourceManager, IBackgroundTaskQueue taskQueue, RAGSystem ragSystem) : ControllerBase
    {
        private readonly Serilog.ILogger _logger = Log.ForContext<PersonsController>();
        private readonly IBackgroundTaskQueue _taskQueue = taskQueue;
        private readonly RAGSystem _ragSystem = ragSystem;


        #region New
        /// <summary>
        /// Creates a new person
        /// </summary>
        /// <param name="dto">The Data Transfer Object</param>
        [HttpPut("new")]
        [SwaggerOperation(Summary = "Create a new person in the archive")]
        [SwaggerResponse(200, "Person was created successfully", typeof(ApiResponse))]
        [SwaggerResponse(409, "Person already exists", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> New([FromBody] PersonCreateDto dto)
        {
            // DTO checks
            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new ApiResponse(false, "No name was given"));

            if (string.IsNullOrEmpty(dto.Occupation))
                return BadRequest(new ApiResponse(false, "No occupation was given"));

            _logger.Information("Creating person '{Name}'...", dto.Name);

            try
            {
                // Create the person and return the ID
                Guid id = await resourceManager.CreatePersonAsync(dto);

                // Add the person to the vector database 
                _taskQueue.QueueBackgroundWorkItem(async token =>
                {
                    await _ragSystem.CreatePoints(id: id, chunk: $"{dto.Name}\n{dto.Description}", fileType: null);
                });

                _logger.Information("Person '{Name}' created successfully.", dto.Name);
                return Ok(new ApiResponse(true, "Person created successfully", id));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error creating person '{Name}'.", dto.Name);
                return StatusCode(500, new ApiResponse(false, "Error creating person", e.Message));
            }
        }
        #endregion

        #region Delete
        /// <summary>
        /// Deletes a person
        /// </summary>
        /// <param name="id">The ID of the person</param>
        [HttpDelete("delete/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Deletes a person")]
        [SwaggerResponse(200, "Person deleted successfully", typeof(ApiResponse))]
        [SwaggerResponse(404, "Person not found", typeof(ApiResponse))]
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
                _logger.Information("Deleting person with ID: {ID}", id);
                bool found = await resourceManager.DeletePersonAsync(id);

                if (found)
                {
                    _logger.Information("Person with ID '{ID}' deleted successfully", id);
                    return Ok(new ApiResponse(true, "Person deleted successfully"));
                }

                _logger.Information("Person with ID '{ID}' not found.", id);
                return NotFound(new ApiResponse(false, "Person does not exist"));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error deleting person with ID {ID}", id);
                return StatusCode(500, new ApiResponse(false, "Error deleting person"));
            }
        }
        #endregion

        #region Update
        /// <summary>
        /// Updates a person
        /// </summary>
        /// <param name="id">The ID of the person</param>
        /// <param name="updates">The dictionary of propertynames to update and their new values</param>
        [HttpPatch("update/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Updates a person")]
        [SwaggerResponse(200, "Person updated", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(404, "Person not found", typeof(ApiResponse))]
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

            _logger.Information("Updating person with ID '{ID}'...", id);

            try
            {
                // Check if person exists
                if (!await resourceManager.PersonExistsAsync(id))
                    return NotFound(new ApiResponse(false, "The person does not exist"));

                // Start a database transaction, since we could be doing multiple updates
                await resourceManager.BeginTransaction();

                // Update the properties
                List<string> updatedProperties = await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(Person), id, updates);

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
                    _logger.Information("Successfully updated person with ID '{ID}'. Updated properties: {props}", id, updatedPropertiesString);
                    return Ok(new ApiResponse(true, $"Person updated successfully.", updatedProperties));
                }

                // If not all properties were updated
                else
                {
                    _logger.Information("Partially updated person with ID '{ID}'. Updated properties: {props}", id, updatedPropertiesString);
                    return Ok(new ApiResponse(true, $"Person updated partially.", updatedProperties));
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error updating person with ID '{ID}'", id);
                return StatusCode(500, new ApiResponse(false, "Error updating person", e.Message));
            }
        }
        #endregion

        #region Exists
        /// <summary>
        /// Checks if a person already exists in the database
        /// </summary>
        /// <param name="name">The name of the person</param>
        [EnableCors("AllowFrontend")]
        [HttpGet("exists")]
        [SwaggerOperation(Summary = "Check if a person exists")]
        [SwaggerResponse(200, "Response with boolean indicating if person exists.", typeof(ApiResponse))]
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
                Guid personId = Guid.Empty;

                // Handle name
                if (!string.IsNullOrEmpty(name))
                    personId = await resourceManager.GetPersonPropertyOrDefaultAsync(predicate: p => p.Name == name, selector: p => p.Id);



                // ID is empty, so no person was found
                if (personId == Guid.Empty)
                    return Ok(new ApiResponse(true, "Person does not exist", new { exists = false, id = "" }));

                // ID was not empty, so person already exists, return the ID
                return Ok(new ApiResponse(true, "Person already exists.", new { exists = true, id = personId.ToString() }));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error while checking if person exists");
                return StatusCode(500, new ApiResponse(false, "Error while checking if person exists", e.Message));
            }
        }
        #endregion

        #region Info
        /// <summary>
        /// Gets the information of the person (database row)
        /// </summary>
        /// <param name="id">The ID of the person</param>
        [HttpGet("info/{id}")]
        [SwaggerOperation(Summary = "Get the information of the person")]
        [SwaggerResponse(200, "Person information", typeof(ApiResponse))]
        [SwaggerResponse(404, "Person Not Found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Info(string id)
        {
            // Check if ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "ID is invalid"));

            try
            {
                // Retrieve the person
                Person? person = await resourceManager.GetPersonAsync(id);

                // If null, the person was not found
                if (person == null)
                    return NotFound(new ApiResponse(false, "The person does not exist"));

                // Return the person
                return Ok(new ApiResponse(true, "Person was found", person));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error retrieving person info.");
                return StatusCode(500, new ApiResponse(false, "Error retrieving person info.", e.Message));
            }
        }
        #endregion

        #region List
        /// <summary>
        /// Retrieves a list or page of all persons
        /// </summary>
        /// <param name="pageIndex">(Optional) The index of the page</param>
        /// <param name="pageSize">(Optional) The size of the page</param>
        [HttpGet("list")]
        [SwaggerOperation(Summary = "Retrieves a list or page of all persons")]
        [SwaggerResponse(200, "A list or page of all the persons in the archive", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> List(int? pageIndex, int? pageSize)
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
                // All persons to be returned
                Person[] persons = [];

                // No paging requested, list all persons
                if (pageIndex == null || pageSize == null)
                    persons = await resourceManager.GetAllPersonsAsync();

                // Paging requested, retrieve persons on that page
                else
                    persons = await resourceManager.GetPersonPageAsync((int)pageIndex, (int)pageSize);

                // Return found persons
                return Ok(new ApiResponse(true, $"Found {persons.Length} persons", persons));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error listing persons.");
                return StatusCode(500, new ApiResponse(false, "Error listing persons.", e.Message));
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
            await resourceManager.UpdatePersonAsync(id, PropertyUpdateUtil.CreatePropertySelector<Person, TProperty>(propertyName), newValue);
        }
        #endregion
    }
}