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
public class RegionsController(RegionService regionService) : AppControllerBase
{
    private const int MAX_NAME_LENGTH = 100;

    [HttpGet]
    [SwaggerOperation(Summary = "Get regions with optional search and pagination")]
    [SwaggerResponse(200, "List of regions")]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        Expression<Func<Region, bool>>? predicate = search != null
            ? r => EF.Functions.TrigramsAreSimilar(r.Name, search) ||
                    EF.Functions.ILike(r.Name, $"%{search}%")
            : null;

        var (items, totalCount) = await regionService.GetPageAsync(page, pageSize, predicate);

        return Ok(new { items, totalCount });
    }
    
    [HttpPut]
    [SwaggerOperation(Summary = "Create a region")]
    [SwaggerResponse(200, "Region created", typeof(Guid))]
    [SwaggerResponse(409, "Region already exists", typeof(Guid))]
    public async Task<IActionResult> Create([FromBody] RegionCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Name is required.", statusCode: 400);

        if (dto.Name.Length > MAX_NAME_LENGTH)
            return Problem($"Name too long (max {MAX_NAME_LENGTH} characters).", statusCode: 400);

        Guid? existingId = await regionService.FindIdByNameAsync(dto.Name);

        if (existingId != null)
            return Conflict(existingId);

        Guid createdBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Guid id = await regionService.CreateAsync(dto.Name, createdBy);

        return Ok(id);
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Delete a region")]
    [SwaggerResponse(204, "Region deleted")]
    [SwaggerResponse(403, "Action forbidden")]
    [SwaggerResponse(404, "Region not found")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Region? region = await regionService.GetByIdAsync(id, includeRelations: true);
        if (region == null) return Problem("Region not found.", statusCode: 404);

        bool isAdmin = User.IsInRole("admin");
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int usageCount = region.ResourceRegionRelations?.Count ?? 0;

        if (!isAdmin && (region.CreatedBy != userId || usageCount != 0))
            return Problem("Cannot delete this region.", statusCode: 403);

        await regionService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/name")]
    [SwaggerOperation(Summary = "Rename a region")]
    [SwaggerResponse(204, "Rename successfull")]
    [SwaggerResponse(409, "Name already exists")]
    [SwaggerResponse(403, "Action forbidden")]
    [SwaggerResponse(404, "Region not found")]
    [SwaggerResponse(400, "Invalid name")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RegionCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Name is required.", statusCode: 400);

        if (dto.Name.Length > MAX_NAME_LENGTH)
            return Problem($"Name too long (max {MAX_NAME_LENGTH} characters).", statusCode: 400);

        Region? region = await regionService.GetByIdAsync(id, includeRelations: true);
        if (region == null)
            return Problem("Region not found.", statusCode: 404);

        bool isAdmin = User.IsInRole("admin");
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int usageCount = region.ResourceRegionRelations?.Count ?? 0;

        if (!isAdmin && (region.CreatedBy != userId || usageCount != 0))
            return Problem("Cannot edit this region.", statusCode: 403);

        if (await regionService.ExistsAsync(r => EF.Functions.ILike(r.Name, dto.Name)))
            return Problem("Region name already exists.", statusCode: 409);

        await regionService.UpdateAsync(id, r => r.Name = dto.Name);
        return NoContent();
    }

    [HttpPatch("merge/{keepId}/{removeId}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Merge two regions")]
    [SwaggerResponse(204, "Regions merged")]
    [SwaggerResponse(404, "Region not found")]
    [SwaggerResponse(400, "Invalid request")]
    public async Task<IActionResult> Merge(Guid keepId, Guid removeId)
    {
        if (keepId == removeId)
            return Problem("Cannot merge region with itself.", statusCode: 400);

        if (!await regionService.ExistsAsync(keepId) || !await regionService.ExistsAsync(removeId))
            return Problem("One or both regions not found.", statusCode: 404);

        await regionService.MergeAsync(keepId, removeId);
        return NoContent();
    }

    [HttpGet("suggestions")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Get merge suggestions based on name similarity")]
    [SwaggerResponse(200, "List of suggestions")]
    public async Task<IActionResult> Suggestions([FromQuery] float threshold = 0.6f, [FromQuery] int limit = 20)
        => Ok(await regionService.GetMergeSuggestionsAsync(threshold, limit));
}