using System.Linq.Expressions;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class JournalsController(JournalService journalService) : AppControllerBase
{
    private const int MAX_NAME_LENGTH = 100;

    [HttpGet]
    [SwaggerOperation(Summary = "Get journals with optional search and pagination")]
    [SwaggerResponse(200, "List of journals")]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        Expression<Func<Journal, bool>>? predicate = search != null
            ? j => EF.Functions.TrigramsAreSimilar(j.Name, search) ||
                   EF.Functions.ILike(j.Name, $"%{search}%")
            : null;

        var (items, totalCount) = await journalService.GetPageAsync(page, pageSize, predicate);

        return Ok(new { items, totalCount });
    }

    [HttpPut]
    [SwaggerOperation(Summary = "Create a journal")]
    [SwaggerResponse(200, "Journal created", typeof(Guid))]
    [SwaggerResponse(409, "Journal already exists", typeof(Guid))]
    public async Task<IActionResult> Create([FromBody] JournalCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Name is required.", statusCode: 400);

        if (dto.Name.Length > MAX_NAME_LENGTH)
            return Problem($"Name too long (max {MAX_NAME_LENGTH} characters).", statusCode: 400);

        Guid? existingId = await journalService.FindIdByNameAsync(dto.Name);

        if (existingId != null)
            return Conflict(existingId);

        Guid createdBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Guid id = await journalService.CreateAsync(dto.Name, createdBy);

        return Ok(id);
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Delete a journal")]
    [SwaggerResponse(204, "Journal deleted")]
    [SwaggerResponse(403, "Action forbidden")]
    [SwaggerResponse(404, "Journal not found")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Journal? journal = await journalService.GetByIdAsync(id, includeRelations: true);
        if (journal == null) return Problem("Journal not found.", statusCode: 404);

        bool isAdmin = User.IsInRole("admin");
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int usageCount = journal.Resources?.Count ?? 0;

        if (!isAdmin && (journal.CreatedBy != userId || usageCount != 0))
            return Problem("Cannot delete this journal.", statusCode: 403);

        await journalService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/name")]
    [SwaggerOperation(Summary = "Rename a journal")]
    [SwaggerResponse(204, "Rename successfull")]
    [SwaggerResponse(409, "Name already exists")]
    [SwaggerResponse(403, "Action forbidden")]
    [SwaggerResponse(404, "Journal not found")]
    [SwaggerResponse(400, "Invalid name")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] JournalCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Name is required.", statusCode: 400);

        if (dto.Name.Length > MAX_NAME_LENGTH)
            return Problem($"Name too long (max {MAX_NAME_LENGTH} characters).", statusCode: 400);

        Journal? journal = await journalService.GetByIdAsync(id, includeRelations: true);
        if (journal == null)
            return Problem("Journal not found.", statusCode: 404);

        bool isAdmin = User.IsInRole("admin");
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int usageCount = journal.Resources?.Count ?? 0;

        if (!isAdmin && (journal.CreatedBy != userId || usageCount != 0))
            return Problem("Cannot edit this journal.", statusCode: 403);

        if (await journalService.ExistsAsync(j => EF.Functions.ILike(j.Name, dto.Name)))
            return Problem("Journal name already exists.", statusCode: 409);

        await journalService.UpdateAsync(id, j => j.Name = dto.Name);
        return NoContent();
    }

    [HttpPatch("merge/{keepId}/{removeId}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Merge two journals")]
    [SwaggerResponse(204, "Journals merged")]
    [SwaggerResponse(404, "Journal not found")]
    [SwaggerResponse(400, "Invalid request")]
    public async Task<IActionResult> Merge(Guid keepId, Guid removeId)
    {
        if (keepId == removeId)
            return Problem("Cannot merge journal with itself.", statusCode: 400);

        if (!await journalService.ExistsAsync(keepId) || !await journalService.ExistsAsync(removeId))
            return Problem("One or both journals not found.", statusCode: 404);

        await journalService.MergeAsync(keepId, removeId);
        return NoContent();
    }

    [HttpGet("suggestions")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Get merge suggestions based on name similarity")]
    [SwaggerResponse(200, "List of suggestions", typeof(List<MergeSuggestion>))]
    public async Task<IActionResult> Suggestions([FromQuery] float threshold = 0.6f, [FromQuery] int limit = 20)
        => Ok(await journalService.GetMergeSuggestionsAsync(threshold, limit));
}