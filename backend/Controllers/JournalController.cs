using Serilog;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.AspNetCore.Cors;
using System.Linq.Expressions;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Authorize]
public class JournalController(ResourceManager resourceManager) : ControllerBase
{
    private readonly Serilog.ILogger logger = Log.ForContext<JournalController>();

    #region New
    [HttpPut("new")]
    [SwaggerOperation(Summary = "Create a new journal")]
    [SwaggerResponse(200, "Journal was created successfully", typeof(ApiResponse))]
    [SwaggerResponse(409, "Journal already exists", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse))]
    public async Task<IActionResult> New([FromBody] JournalCreateDto dto)
    {
        // DTO Checks
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new ApiResponse(false, "No name was given"));

        logger.Information("Creating journal '{Name}'...", dto.Name);

        try
        {
            Guid id = await resourceManager.CreateJournalAsync(dto);

            logger.Information("Journal '{Name}' created successfully.", dto.Name);
            return Ok(new ApiResponse(true, "Journal created successfully", id));
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return Conflict(new ApiResponse(false, "Journal already exists"));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error creating journal '{Name}'.", dto.Name);
            return StatusCode(500, new ApiResponse(false, "Error creating journal"));
        }
    }
    #endregion

    #region Delete
    [HttpDelete("delete/{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Deletes a journal")]
    [SwaggerResponse(200, "Journal deleted successfully", typeof(ApiResponse))]
    [SwaggerResponse(404, "Journal not found", typeof(ApiResponse))]
    [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse))]
    public async Task<IActionResult> Delete(string id)
    {
        // Check if the ID is valid
        if (!ValidityUtil.IsValidId(id))
            return BadRequest(new ApiResponse(false, "Invalid ID"));

        try
        {
            logger.Information("Deleting journal with ID: {ID}", id);
            bool found = await resourceManager.DeleteJournalAsync(Guid.Parse(id));

            if (found)
            {
                logger.Information("Journal with ID '{ID}' deleted successfully", id);
                return Ok(new ApiResponse(true, "Journal deleted successfully", id));
            }

            logger.Information("Journal with ID '{ID}' not found", id);
            return NotFound(new ApiResponse(false, "Journal with this ID does not exist.", id));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error deleting journal with ID '{ID}'", id);
            return StatusCode(500, new ApiResponse(false, e.Message, id));
        }
    }
    #endregion

    #region Update
    [HttpPatch("update/{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Updates the specified journal")]
    [SwaggerResponse(200, "Journal updated", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad request", typeof(ApiResponse))]
    [SwaggerResponse(404, "Journal not found", typeof(ApiResponse))]
    [SwaggerResponse(409, "Already exists", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse))]
    public async Task<IActionResult> Update(string id, [FromBody] Dictionary<string, object> updates)
    {
        if (!ValidityUtil.IsValidId(id))
            return BadRequest(new ApiResponse(false, "Invalid ID."));

        // Check if updates are provided
        if (updates == null || updates.Count == 0)
            return BadRequest(new ApiResponse(false, "No updates were provided."));

        logger.Information("Updating journal with ID '{ID}'...", id);

        try
        {
            // Check if journal exists
            if (!await resourceManager.JournalExistsAsync(Guid.Parse(id)))
                return NotFound(new ApiResponse(false, "This journal does not exist", id));

            await resourceManager.BeginTransaction();

            List<string> updatedProperties = await PropertyUpdateUtil.UpdateProperties(this, nameof(UpdateProperty), typeof(Journal), id, updates);

            if (updatedProperties.Count == 0)
            {
                await resourceManager.Rollback();
                return BadRequest(new ApiResponse(false, "None of the props were found"));
            }

            // Commit changes to database
            await resourceManager.Commit();

            // Join all updated properties
            string updatedPropertiesString = string.Join(", ", updatedProperties);

            logger.Information("Successfully updated journal with ID '{ID}'. Updated properties: {props}", id, updatedPropertiesString);
            return Ok(new ApiResponse(true, $"Journal updated {(updatedProperties.Count == updates.Count ? "successfully" : "partially")}."));
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return Conflict(new ApiResponse(false, "A journal with that name already exists"));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error updating journal with ID '{ID}'", id);
            return StatusCode(500, new ApiResponse(false, e.Message, id));
        }
    }
    #endregion

    #region Exists
    [EnableCors("AllowFrontend")]
    [HttpGet("exists")]
    [SwaggerOperation(Summary = "Check if a journal exists")]
    [SwaggerResponse(200, "Boolean indicating if region exists", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad request", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse))]
    public async Task<IActionResult> Exists([FromQuery] string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new ApiResponse(false, "No value given"));

        try
        {
            object? journalId = null;

            journalId = await resourceManager.GetJournalPropertyOrDefaultAsync(predicate: j => EF.Functions.ILike(j.Name, name), selector: "Id");

            return Ok(new ApiResponse(true, $"Journal {(journalId != null ? "already exists" : "does not exist")}", new { exists = journalId != null, id = journalId == null ? "" : journalId.ToString() }));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error while checking if journal exists");
            return StatusCode(500, new ApiResponse(false, e.Message, name));
        }
    }
    #endregion

    #region Info
    [HttpGet("info/{id}")]
    [SwaggerOperation(Summary = "Get journal information")]
    [SwaggerResponse(200, "Journal information", typeof(ApiResponse))]
    [SwaggerResponse(404, "Journal not found", typeof(ApiResponse))]
    [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse))]
    public async Task<IActionResult> Info(string id, [FromQuery] string? properties)
    {
        if (!ValidityUtil.IsValidId(id))
            return BadRequest(new ApiResponse(false, "ID is invalid", id));

        try
        {
            object? journal = string.IsNullOrWhiteSpace(properties) ?
                await resourceManager.GetJournalAsync(id) :
                await resourceManager.GetJournalPropertyAsync(id, $"new({properties})");

            if (journal == null)
                return NotFound(new ApiResponse(false, "The journal does not exist.", id));

            return Ok(new ApiResponse(true, "Journal was found", journal));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error retrieving journal info.");
            return StatusCode(500, new ApiResponse(false, e.Message, id));
        }
    }
    #endregion

    #region List
    [HttpGet("list")]
    [SwaggerOperation(Summary = "Retrieves a list or page of all journals")]
    [SwaggerResponse(200, "A list or page of all the journals", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad request", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse))]
    public async Task<IActionResult> List(int? pageIndex, int? pageSize, string? properties, string? searchQuery)
    {
        if (pageIndex != null && pageIndex < 1)
            return BadRequest(new ApiResponse(false, "Page index cannot be lower than 1."));

        if (pageSize != null && pageSize < 1)
            return BadRequest(new ApiResponse(false, "Page size cannot be lower than 1."));

        // set defaults
        if (pageIndex != null && pageSize == null) pageSize = 100;
        if (pageSize != null && pageIndex == null) pageIndex = 1;

        try
        {
            object[] journals = [];

            string projectionString = $"new({properties})";

            Expression<Func<Journal, bool>>? predicate = searchQuery != null ? j => EF.Functions.TrigramsAreSimilar(j.Name, searchQuery) ||
                                                                                    EF.Functions.ILike(j.Name, $"{searchQuery}%") ||
                                                                                    EF.Functions.ILike(j.Name, $"%{searchQuery}%") : null;

            // No paging
            if (pageIndex == null || pageSize == null)
                journals = string.IsNullOrWhiteSpace(properties) ?
                    await resourceManager.GetAllJournalsAsync(predicate: predicate) :
                    await resourceManager.GetAllJournalsAsync(projection: projectionString, predicate: predicate);

            // Paging
            else
                journals = string.IsNullOrWhiteSpace(properties) ?
                    await resourceManager.GetJournalPageAsync((int)pageIndex, (int)pageSize, predicate: predicate) :
                    await resourceManager.GetJournalPageAsync(projectionString, (int)pageIndex, (int)pageSize, predicate: predicate);

            return Ok(new ApiResponse(true, $"Found {journals.Length} journals", journals));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing journals");
            return StatusCode(500, new ApiResponse(false, e.Message));
        }
    }
    #endregion

    #region Merge
    [HttpPatch("merge/{id1}/{id2}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Merges the second journal into the first")]
    [SwaggerResponse(200, "Journals merged successfully", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad request", typeof(ApiResponse))]
    [SwaggerResponse(404, "Journal(s) not found", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse))]
    public async Task<IActionResult> Merge(string id1, string id2)
    {
        if (string.IsNullOrWhiteSpace(id1) || string.IsNullOrWhiteSpace(id2))
            return BadRequest(new ApiResponse(false, "IDs are required."));

        if (id1 == id2)
            return BadRequest(new ApiResponse(false, "Cannot merge with itself."));

        if (!Guid.TryParse(id1, out Guid journalId1) || !Guid.TryParse(id2, out Guid journalId2))
            return BadRequest(new ApiResponse(false, "Invalid ID format."));

        try
        {
            if (!await resourceManager.JournalExistsAsync(journalId1) || !await resourceManager.JournalExistsAsync(journalId2))
                return NotFound(new ApiResponse(false, "One or both journals not found"));

            await resourceManager.BeginTransaction();

            // Update all resources with ID2 to link to ID1
            await resourceManager.UpdateResourceAsync(predicate: r => r.JournalId == journalId2, propertySelector: r => r.JournalId, journalId1);

            // Delete ID2
            if (!await resourceManager.DeleteJournalAsync(journalId2))
            {
                await resourceManager.Rollback();
                logger.Error("Failed to delete journal {JournalId2} during merge", id2);
                return StatusCode(500, new ApiResponse(false, "Failed to delete source journal during merge."));
            }

            await resourceManager.Commit();
            return Ok(new ApiResponse(true, "Journals merged successfully."));
        }
        catch (Exception e)
        {
            await resourceManager.Rollback();
            logger.Error(e, "Error merging regions {JournalId1} and {JournalId2}", id1, id2);
            return StatusCode(500, new ApiResponse(false, e.Message));
        }
    }
    #endregion

    #region Suggestions
    [HttpGet("suggestions")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Returns journal pairs that are candidates for merging based on name similarity.")]
    [SwaggerResponse(200, "List of merge suggestions", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse))]
    public async Task<IActionResult> Suggestions([FromQuery] float threshold = 0.6f, [FromQuery] int limit = 20)
    {
        try
        {
            var suggestions = await resourceManager.GetJournalMergeSuggestionsAsync(threshold, limit);
            return Ok(new ApiResponse(true, $"Found {suggestions.Count} suggestion(s)", suggestions));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error fetching journal merge suggestions");
            return StatusCode(500, new ApiResponse(false, e.Message));
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
        await resourceManager.UpdateJournalAsync(Guid.Parse(id), PropertyUpdateUtil.CreatePropertySelector<Journal, TProperty>(propertyName), newValue);
    }
    #endregion
}