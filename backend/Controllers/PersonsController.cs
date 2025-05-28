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
using Org.BouncyCastle.Asn1.X509;

namespace KnowledgeBank.Controllers 
{
    /// <summary>
    /// This controller is responsible for handing API calls to manage persons and their metadata.
    /// 
    /// Author: Abel Dieterich
    /// </summary>
    /// <param name="resourceManager">The resource manager service for database interactions</param>
    [ApiController] [Route("[controller]")] [Produces("application/json")] [Authorize]
    public class PersonsController(ResourceManager resourceManager) : ControllerBase
    {   
        private readonly Serilog.ILogger logger = Log.ForContext<PersonsController>();

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
            
            logger.Information("Creating person '{Name}'...", dto.Name);

            try
            {
                // Create the person and return the ID
                Guid id = await resourceManager.CreatePersonAsync(dto);

                logger.Information("Person '{Name}' created successfully.", dto.Name);
                return Ok(new ApiResponse(true, "Person created successfully", id));
            }
            catch (Exception e) 
            {
                logger.Error(e, "Error creating person '{Name}'.", dto.Name);
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
                logger.Information("Deleting person with ID: {ID}", id);
                bool found = await resourceManager.DeletePersonAsync(id);

                if (found)
                {
                    logger.Information("Person with ID '{ID}' deleted successfully", id);
                    return Ok(new ApiResponse(true, "Person deleted successfully"));
                }

                logger.Information("Person with ID '{ID}' not found.", id);
                return NotFound(new ApiResponse(false, "Person does not exist"));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error deleting person with ID {ID}", id);
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

            logger.Information("Updating person with ID '{ID}'...", id);

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
                    logger.Information("Successfully updated person with ID '{ID}'. Updated properties: {props}", id, updatedPropertiesString);
                    return Ok(new ApiResponse(true, $"Person updated successfully.", updatedProperties));
                }

                // If not all properties were updated
                else
                {
                    logger.Information("Partially updated person with ID '{ID}'. Updated properties: {props}", id, updatedPropertiesString);
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
                object? personId = null;

                // Handle name
                if (!string.IsNullOrEmpty(name))
                    personId = await resourceManager.GetPersonPropertyOrDefaultAsync(predicate: p => p.Name == name, selector: "Id");



                // ID is empty, so no person was found
                if (personId == null)
                    return Ok(new ApiResponse(true, "Person does not exist", new { exists = false, id = "" }));

                // ID was not empty, so person already exists, return the ID
                return Ok(new ApiResponse(true, "Person already exists.", new { exists = true, id = personId.ToString() }));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while checking if person exists");
                return StatusCode(500, new ApiResponse(false, "Error while checking if person exists", e.Message));
            }
        }
        #endregion

        #region Info
        /// <summary>
        /// Gets the information of the person (database row)
        /// </summary>
        /// <param name="id">The ID of the person</param>
        /// <param name="properties">The properties you are trying to receive, separated by comma</param>
        [HttpGet("info/{id}")]
        [SwaggerOperation(Summary = "Get the information of the person")]
        [SwaggerResponse(200, "Person information", typeof(ApiResponse))]
        [SwaggerResponse(404, "Person Not Found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Info(string id, [FromQuery] string? properties)
        {
            // Check if ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "ID is invalid"));

            try
            {
                // Retrieve the person
                object? person = string.IsNullOrEmpty(properties) ?
                    await resourceManager.GetPersonAsync(id) :
                    await resourceManager.GetPersonAsync(id, $"new({properties})");

                // If null, the person was not found
                if (person == null)
                    return NotFound(new ApiResponse(false, "The person does not exist"));

                // Return the person
                return Ok(new ApiResponse(true, "Person was found", person));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error retrieving person info.");
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
        /// <param name="properties">(Optional) The properties to select, separated by comma</param>
        /// <param name="searchQuery">(Optional) Filter on search query </param>
        [HttpGet("list")]
        [SwaggerOperation(Summary = "Retrieves a list or page of all persons")]
        [SwaggerResponse(200, "A list or page of all the persons in the archive", typeof(ApiResponse))]
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
                // All persons to be returned
                object[] persons = [];

                string projectionString = $"new({properties})";


                // No paging requested, list all persons
                if (pageIndex == null || pageSize == null)
                    persons = string.IsNullOrEmpty(properties) ?
                        (string.IsNullOrEmpty(searchQuery) ? await resourceManager.GetAllPersonsAsync() :
                        await resourceManager.GetAllPersonsAsync(predicate: r => r.Name.ToLower().Contains(searchQuery.ToLower()))) :
                        (string.IsNullOrEmpty(searchQuery) ? await resourceManager.GetAllPersonsAsync(projection: projectionString) :
                        await resourceManager.GetAllPersonsAsync(projection: projectionString, predicate: r => r.Name.ToLower().Contains(searchQuery.ToLower())));

                // Paging requested, retrieve persons on that page
                else
                    persons = string.IsNullOrEmpty(properties) ?
                        (string.IsNullOrEmpty(searchQuery) ? await resourceManager.GetPersonPageAsync((int)pageIndex, (int)pageSize) :
                        await resourceManager.GetPersonPageAsync((int)pageIndex, (int)pageSize, predicate: r => r.Name.ToLower().Contains(searchQuery.ToLower()))) :
                        (string.IsNullOrEmpty(searchQuery) ? await resourceManager.GetPersonPageAsync(projectionString, (int)pageIndex, (int)pageSize) :
                        await resourceManager.GetPersonPageAsync(projectionString, (int)pageIndex, (int)pageSize, predicate: r => r.Name.ToLower().Contains(searchQuery.ToLower())));


                // Return found persons
                return Ok(new ApiResponse(true, $"Found {persons.Length} persons", persons));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error listing persons.");
                return StatusCode(500, new ApiResponse(false, "Error listing persons.", e.Message));
            }
        }
        #endregion
        
        
        #region Relation fetches
        /// <summary>
        /// Retrieves all relations of the given type for the given person ID
        /// </summary>
        /// <param name="relation">The relation to retrieve</param>
        /// <param name="id">The ID of the person</param>
        /// <param name="properties">(Optional) The properties to select from the result</param>
        [HttpGet("{id}/relations/{relation}")]
        [SwaggerOperation(Summary = "Retrieves all relations of the given type for the given person ID")]
        [SwaggerResponse(200, "The relations", typeof(ApiResponse))]
        [SwaggerResponse(404, "Person not found", typeof(ApiResponse))]
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
                        await resourceManager.GetAllResourceAuthorRelationsAsync(r => r.PersonId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceAuthorRelationsAsync(predicate: r => r.PersonId == Guid.Parse(id), projection: $"new({properties})"),

                    // Related resources
                    "related-resources" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllResourceRelatedPersonRelationsAsync(r => r.PersonId == Guid.Parse(id)) :
                        await resourceManager.GetAllResourceRelatedPersonRelationsAsync(predicate: r => r.PersonId == Guid.Parse(id), projection: $"new({properties})"),

                    // Related persons
                    "person-related-persons" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllPersonRelationshipsAsync(predicate: p => p.SourcePersonId == Guid.Parse(id) || p.TargetPersonId == Guid.Parse(id)) :
                        await resourceManager.GetAllPersonRelationshipsAsync(predicate: p => p.SourcePersonId == Guid.Parse(id) || p.TargetPersonId == Guid.Parse(id), projection: $"new({properties})"),
                        
                    // Related organisations
                    "organisations" => string.IsNullOrEmpty(properties) ?
                        await resourceManager.GetAllPersonOrganisationRelationsAsync(predicate: p => p.PersonId == Guid.Parse(id)) :
                        await resourceManager.GetAllPersonOrganisationRelationsAsync(predicate: p => p.PersonId == Guid.Parse(id), projection: $"new({properties})"),
                    
                    // Default
                    _ => null
                };

                if (result == null)
                    return NotFound(new ApiResponse(false, "ID or relation not found"));

                return Ok(new ApiResponse(true, "Successfully retrieved relations", result));
            }
            catch (Exception e) 
            {
                logger.Error(e, "Error retrieving relation '{Relation}' for person with ID '{Id}'", relation, id);
                return StatusCode(500, new ApiResponse(false, "Internal Server Error", e.Message));
            }
        }
        
        #endregion
        
        #region Add Relations
        /// <summary>
        /// Adds a relation for this person
        /// </summary>
        /// <param name="id">The ID of the person</param>
        /// <param name="relation">The relation to be made</param>
        /// <param name="targetId">The ID of the other item in the relation</param>
        /// /// <param name="relationInfo">(Optional) Extra information over the relation</param>
        [HttpGet("{id}/relations/add/{relation}/{targetId}")]
        [SwaggerOperation(Summary = "Adds a relation to the person")]
        [SwaggerResponse(200, "Successfully added relation", typeof(ApiResponse))]
        [SwaggerResponse(404, "Person not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> AddRelation(string id, string relation, string targetId, [FromQuery]string? relationInfo) 
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
                    
                    // Related resources
                    case "related-resources":
                        await resourceManager.AddRelatedPersonToResourceAsync(targetId, id, relationInfo);
                        break;
                    
                    // Persons
                    case "person-related-persons":
                        await resourceManager.AddPersonRelationshipAsync(id, relationInfo, targetId);
                        break;
                    
                    // Organisations
                    case "organisations":
                        await resourceManager.AddPersonToOrganisationAsync(id, relationInfo, targetId);
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
        /// Removes a relation for this person
        /// </summary>
        /// <param name="id">The ID of the person</param>
        /// <param name="relation">The relation to be removed</param>
        /// <param name="targetId">The ID of the other item in the relation</param>
        [HttpGet("{id}/relations/remove/{relation}/{targetId}")]
        [SwaggerOperation(Summary = "Removes a relation to the person")]
        [SwaggerResponse(200, "Successfully removed relation", typeof(ApiResponse))]
        [SwaggerResponse(404, "Person not found", typeof(ApiResponse))]
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

                    // Related resources
                    case "related-resources":
                        await resourceManager.RemoveRelatedPersonFromResourceAsync(targetId, id);
                        break;

                    // Persons
                    case "person-related-persons":
                        await resourceManager.RemovePersonRelationshipAsync(id, targetId);
                        break;

                    // Organisations
                    case "organisations":
                        await resourceManager.RemovePersonFromOrganisationAsync(id, targetId);
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