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
[Route("resource-types")]
[Authorize]
public class ResourceTypesController(ResourceTypeService resourceTypeService) : AppControllerBase
{
    private const int MAX_NAME_LENGTH = 100;

    [HttpGet]
    [SwaggerOperation(Summary = "Get resource types with optional search and pagination")]
    [SwaggerResponse(200, "List of resource types")]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        Expression<Func<ResourceType, bool>>? predicate = search != null
            ? r => EF.Functions.TrigramsAreSimilar(r.Name, search) ||
                    EF.Functions.ILike(r.Name, $"%{search}%")
            : null;

        var (items, totalCount) = await resourceTypeService.GetPageAsync(page, pageSize, predicate);

        return Ok(new { items, totalCount });
    }

    [HttpPut]
    [SwaggerOperation(Summary = "Create a resource type")]
    [SwaggerResponse(200, "Resource type created", typeof(Guid))]
    [SwaggerResponse(409, "Resource type already exists", typeof(Guid))]
    public async Task<IActionResult> Create([FromBody] ResourceTypeCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Name is required.", statusCode: 400);

        if (dto.Name.Length > MAX_NAME_LENGTH)
            return Problem($"Name too long (max {MAX_NAME_LENGTH} characters).", statusCode: 400);

        Guid? existingId = await resourceTypeService.FindIdByNameAsync(dto.Name);

        if (existingId != null)
            return Conflict(existingId);

        Guid createdBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Guid id = await resourceTypeService.CreateAsync(dto.Name, createdBy);

        return Ok(id);
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Delete a resource type")]
    [SwaggerResponse(204, "Resource type deleted")]
    [SwaggerResponse(403, "Action forbidden")]
    [SwaggerResponse(404, "Resource type not found")]
    public async Task<IActionResult> Delete(Guid id)
    {
        ResourceType? resourceType = await resourceTypeService.GetByIdAsync(id, includeRelations: true);
        if (resourceType == null) return Problem("Resource type not found.", statusCode: 404);

        bool isAdmin = User.IsInRole("admin");
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int usageCount = resourceType.Resources?.Count ?? 0;

        if (!isAdmin && (resourceType.CreatedBy != userId || usageCount != 0))
            return Problem("Cannot delete this resource type.", statusCode: 403);

        await resourceTypeService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/name")]
    [SwaggerOperation(Summary = "Rename a resource type")]
    [SwaggerResponse(204, "Rename successfull")]
    [SwaggerResponse(409, "Name already exists")]
    [SwaggerResponse(403, "Action forbidden")]
    [SwaggerResponse(404, "Resource type not found")]
    [SwaggerResponse(400, "Invalid name")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] ResourceTypeCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Name is required.", statusCode: 400);

        if (dto.Name.Length > MAX_NAME_LENGTH)
            return Problem($"Name too long (max {MAX_NAME_LENGTH} characters).", statusCode: 400);

        ResourceType? resourceType = await resourceTypeService.GetByIdAsync(id, includeRelations: true);
        if (resourceType == null)
            return Problem("Resource type not found.", statusCode: 404);

        bool isAdmin = User.IsInRole("admin");
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int usageCount = resourceType.Resources?.Count ?? 0;

        if (!isAdmin && (resourceType.CreatedBy != userId || usageCount != 0))
            return Problem("Cannot edit this resource type.", statusCode: 403);

        if (await resourceTypeService.ExistsAsync(r => EF.Functions.ILike(r.Name, dto.Name)))
            return Problem("Resource type name already exists.", statusCode: 409);

        await resourceTypeService.UpdateAsync(id, r => r.Name = dto.Name);
        return NoContent();
    }

    [HttpPatch("merge/{keepId}/{removeId}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Merge two resource types")]
    [SwaggerResponse(204, "Resource types merged")]
    [SwaggerResponse(404, "Resource type not found")]
    [SwaggerResponse(400, "Invalid request")]
    public async Task<IActionResult> Merge(Guid keepId, Guid removeId)
    {
        if (keepId == removeId)
            return Problem("Cannot merge resource type with itself.", statusCode: 400);

        if (!await resourceTypeService.ExistsAsync(keepId) || !await resourceTypeService.ExistsAsync(removeId))
            return Problem("One or both resource types not found.", statusCode: 404);

        await resourceTypeService.MergeAsync(keepId, removeId);
        return NoContent();
    }

    [HttpGet("suggestions")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Get merge suggestions based on name similarity")]
    [SwaggerResponse(200, "List of suggestions", typeof(List<MergeSuggestion>))]
    public async Task<IActionResult> Suggestions([FromQuery] float threshold = 0.6f, [FromQuery] int limit = 20)
        => Ok(await resourceTypeService.GetMergeSuggestionsAsync(threshold, limit));
}