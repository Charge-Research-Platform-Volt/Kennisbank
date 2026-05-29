using System.Linq.Expressions;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace KnowledgeBank.Controllers;

[Route("[controller]")]
[Authorize]
public class TagsController(TagService tagService) : AppControllerBase
{
    private const int MAX_NAME_LENGTH = 100;

    [HttpGet]
    [SwaggerOperation(Summary = "Get tags with optional search and pagination")]
    [SwaggerResponse(200, "List of tags")]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        Expression<Func<Tag, bool>>? predicate = search != null
            ? t =>  EF.Functions.TrigramsAreSimilar(t.Name, search) ||
                    EF.Functions.ILike(t.Name, $"%{search}%")
            : null;

        var (items, totalCount) = await tagService.GetPageAsync(page, pageSize, predicate);

        return Ok(new { items, totalCount });
    }

    [HttpPut]
    [SwaggerOperation(Summary = "Create a tag")]
    [SwaggerResponse(200, "Tag created", typeof(Guid))]
    [SwaggerResponse(409, "Tag already exists", typeof(Guid))]
    public async Task<IActionResult> Create([FromBody] TagCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Name is required.", statusCode: 400);

        if (dto.Name.Length > MAX_NAME_LENGTH)
            return Problem($"Name too long (max {MAX_NAME_LENGTH} characters).", statusCode: 400);

        Guid? existingId = await tagService.FindIdByNameAsync(dto.Name);

        if (existingId != null)
            return Conflict(existingId);

        Guid createdBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Guid id = await tagService.CreateAsync(dto.Name, createdBy);

        return Ok(id);
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Delete a tag")]
    [SwaggerResponse(204, "Tag deleted")]
    [SwaggerResponse(403, "Action forbidden")]
    [SwaggerResponse(404, "Tag not found")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Tag? tag = await tagService.GetByIdAsync(id, includeRelations: true);
        if (tag == null) return Problem("Tag not found.", statusCode: 404);

        bool isAdmin = User.IsInRole("admin");
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int usageCount = (tag.ResourceTagRelations?.Count ?? 0) + (tag.ProjectTagRelations?.Count ?? 0);

        if (!isAdmin && (tag.CreatedBy != userId || usageCount != 0))
            return Problem("Cannot delete this tag.", statusCode: 403);

        await tagService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/name")]
    [SwaggerOperation(Summary = "Rename a tag")]
    [SwaggerResponse(204, "Rename successfull")]
    [SwaggerResponse(409, "Name already exists")]
    [SwaggerResponse(403, "Action forbidden")]
    [SwaggerResponse(404, "Tag not found")]
    [SwaggerResponse(400, "Invalid name")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] TagCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Name is required.", statusCode: 400);

        if (dto.Name.Length > MAX_NAME_LENGTH)
            return Problem($"Name too long (max {MAX_NAME_LENGTH} characters).", statusCode: 400);

        Tag? tag = await tagService.GetByIdAsync(id, includeRelations: true);
        if (tag == null)
            return Problem("Tag not found.", statusCode: 404);

        bool isAdmin = User.IsInRole("admin");
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int usageCount = (tag.ResourceTagRelations?.Count ?? 0) + (tag.ProjectTagRelations?.Count ?? 0);

        if (!isAdmin && (tag.CreatedBy != userId || usageCount != 0))
            return Problem("Cannot edit this tag.", statusCode: 403);

        if (await tagService.ExistsAsync(t => EF.Functions.ILike(t.Name, dto.Name)))
            return Problem("Tag name already exists.", statusCode: 409);

        await tagService.UpdateAsync(id, t => t.Name = dto.Name);
        return NoContent();
    }

    [HttpPatch("merge/{keepId}/{removeId}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Merge two tags")]
    [SwaggerResponse(204, "Tags merged")]
    [SwaggerResponse(404, "Tag not found")]
    [SwaggerResponse(400, "Invalid request")]
    public async Task<IActionResult> Merge(Guid keepId, Guid removeId)
    {
        if (keepId == removeId)
            return Problem("Cannot merge tag with itself.", statusCode: 400);

        if (!await tagService.ExistsAsync(keepId) || !await tagService.ExistsAsync(removeId))
            return Problem("One or both tags not found.", statusCode: 404);

        await tagService.MergeAsync(keepId, removeId);
        return NoContent();
    }

    [HttpGet("suggestions")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Get merge suggestions based on name similarity")]
    [SwaggerResponse(200, "List of suggestions")]
    public async Task<IActionResult> Suggestions([FromQuery] float threshold = 0.6f, [FromQuery] int limit = 20)
        => Ok(await tagService.GetMergeSuggestionsAsync(threshold, limit));
}