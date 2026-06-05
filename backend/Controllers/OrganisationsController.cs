using System.Linq.Expressions;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using KnowledgeBank.Services.Background;
using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Controllers;

[Route("[controller]")]
[Authorize]
public class OrganisationsController(OrganisationService organisationService, IBackgroundTaskQueue taskQueue) : AppControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Get organisations with optional search and pagination")]
    [SwaggerResponse(200, "List of organisations")]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool trash = false)
    {
        Expression<Func<Organisation, bool>> predicate = search != null
            ? o => (EF.Functions.TrigramsAreSimilar(o.Name, search) ||
                    EF.Functions.ILike(o.Name, $"%{search}%")) && o.Trashed == trash
            : o => o.Trashed == trash;

        var (items, totalCount) = await organisationService.GetPageAsync(page, pageSize, predicate);

        return Ok(new { items, totalCount });
    }

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Get organisation details")]
    [SwaggerResponse(200, "Details of the organisation", typeof(OrganisationDetailDto))]
    [SwaggerResponse(404, "Organisation not found")]
    public async Task<IActionResult> GetDetails(Guid id)
        => OkOrNotFound(await organisationService.GetDetailAsync(id));

    [HttpPut]
    [SwaggerOperation(Summary = "Create an organisation")]
    [SwaggerResponse(200, "Organisation created", typeof(Guid))]
    [SwaggerResponse(409, "Organisation already exists", typeof(Guid))]
    public async Task<IActionResult> Create([FromBody] OrganisationCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Name is required.", statusCode: 400);

        Guid? existingId = await organisationService.FindIdByNameAsync(dto.Name);
        if (existingId != null)
            return Conflict(existingId);

        Guid createdBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Guid id = await organisationService.CreateAsync(dto, createdBy);

        taskQueue.QueueBackgroundWorkItem(async token =>
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
            await ingestionService.RunOrganisationEntityPipelineAsync(id);
        });

        return Ok(id);
    }

    [HttpPatch("{id}")]
    [SwaggerOperation(Summary = "Update an organisation")]
    [SwaggerResponse(204, "Organisation updated")]
    [SwaggerResponse(404, "Organisation not found")]
    public async Task<IActionResult> Update(Guid id, [FromBody] OrganisationUpdateDto dto)
    {
        bool found = await organisationService.UpdateAsync(id, o =>
        {
            if (dto.Name != null) o.Name = dto.Name;
            if (dto.Description != null) o.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description;
            if (dto.EmailAddress != null) o.EmailAddress = string.IsNullOrWhiteSpace(dto.EmailAddress) ? null : dto.EmailAddress;
            if (dto.Website != null) o.Website = string.IsNullOrWhiteSpace(dto.Website) ? null : dto.Website;
        });

        if (!found) return Problem("Organisation not found.", statusCode: 404);

        taskQueue.QueueBackgroundWorkItem(async token =>
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
            await ingestionService.RunOrganisationEntityPipelineAsync(id);
        });

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Delete an organisation permanently")]
    [SwaggerResponse(204, "Organisation deleted")]
    [SwaggerResponse(404, "Organisation not found")]
    public async Task<IActionResult> Delete(Guid id)
        => NoContentOrNotFound(await organisationService.DeleteAsync(id));

    [HttpPatch("{id}/trash")]
    [SwaggerOperation(Summary = "Move organisation to trash")]
    [SwaggerResponse(204, "Organisation moved to trash")]
    [SwaggerResponse(404, "Organisation not found")]
    public async Task<IActionResult> Trash(Guid id)
        => NoContentOrNotFound(await organisationService.TrashAsync(id));

    [HttpPatch("{id}/untrash")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Restore organisation from trash")]
    [SwaggerResponse(204, "Organisation restored from trash")]
    [SwaggerResponse(404, "Organisation not found")]
    public async Task<IActionResult> Untrash(Guid id)
        => NoContentOrNotFound(await organisationService.UntrashAsync(id));

    [HttpPost("{id}/authored-resources/{targetId}")]
    [SwaggerOperation(Summary = "Add authored resource")]
    [SwaggerResponse(204, "Relation added")]
    public async Task<IActionResult> AddAuthoredResource(Guid id, Guid targetId)
    {
        await organisationService.AddAuthoredResourceAsync(id, targetId);
        return NoContent();
    }

    [HttpDelete("{id}/authored-resources/{targetId}")]
    [SwaggerOperation(Summary = "Remove authored resource")]
    [SwaggerResponse(204, "Relation removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveAuthoredResource(Guid id, Guid targetId)
        => NoContentOrNotFound(await organisationService.RemoveAuthoredResourceAsync(id, targetId));

    [HttpPost("{id}/related-resources/{targetId}")]
    [SwaggerOperation(Summary = "Add related resource")]
    [SwaggerResponse(204, "Relation added")]
    public async Task<IActionResult> AddRelatedResource(Guid id, Guid targetId, [FromQuery] string? role)
    {
        await organisationService.AddRelatedResourceAsync(id, targetId, role);
        return NoContent();
    }

    [HttpDelete("{id}/related-resources/{targetId}")]
    [SwaggerOperation(Summary = "Remove related resource")]
    [SwaggerResponse(204, "Relation removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveRelatedResource(Guid id, Guid targetId)
        => NoContentOrNotFound(await organisationService.RemoveRelatedResourceAsync(id, targetId));

    [HttpPatch("{id}/related-resources/{targetId}/role")]
    [SwaggerOperation(Summary = "Update related resource role")]
    [SwaggerResponse(204, "Role updated")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> UpdateRelatedResourceRole(Guid id, Guid targetId, [FromQuery] string newRole)
        => NoContentOrNotFound(await organisationService.UpdateRelatedResourceRoleAsync(id, targetId, newRole));

    [HttpPost("{id}/related-organisations/{targetId}")]
    [SwaggerOperation(Summary = "Add related organisation")]
    [SwaggerResponse(204, "Relation added")]
    public async Task<IActionResult> AddRelatedOrganisation(Guid id, Guid targetId, [FromQuery] string? role)
    {
        await organisationService.AddOrganisationRelationshipAsync(id, targetId, role);
        return NoContent();
    }

    [HttpDelete("{id}/related-organisations/{targetId}")]
    [SwaggerOperation(Summary = "Remove related organisation")]
    [SwaggerResponse(204, "Relation removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveRelatedOrganisation(Guid id, Guid targetId)
        => NoContentOrNotFound(await organisationService.RemoveOrganisationRelationshipAsync(id, targetId));

    [HttpPatch("{id}/related-organisations/{targetId}/role")]
    [SwaggerOperation(Summary = "Update related organisation role")]
    [SwaggerResponse(204, "Role updated")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> UpdateRelatedOrganisationRole(Guid id, Guid targetId, [FromQuery] string newRole)
        => NoContentOrNotFound(await organisationService.UpdateOrganisationRelationshipRoleAsync(id, targetId, newRole));

    [HttpPost("{id}/related-persons/{targetId}")]
    [SwaggerOperation(Summary = "Add person relation")]
    [SwaggerResponse(204, "Relation added")]
    public async Task<IActionResult> AddPerson(Guid id, Guid targetId, [FromQuery] string? role)
    {
        await organisationService.AddPersonRelationAsync(id, targetId, role);
        return NoContent();
    }

    [HttpDelete("{id}/related-persons/{targetId}")]
    [SwaggerOperation(Summary = "Remove person relation")]
    [SwaggerResponse(204, "Relation removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemovePerson(Guid id, Guid targetId)
        => NoContentOrNotFound(await organisationService.RemovePersonRelationAsync(id, targetId));

    [HttpPatch("{id}/related-persons/{targetId}/role")]
    [SwaggerOperation(Summary = "Update person relation role")]
    [SwaggerResponse(204, "Role updated")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> UpdatePersonRole(Guid id, Guid targetId, [FromQuery] string newRole)
        => NoContentOrNotFound(await organisationService.UpdatePersonRelationRoleAsync(id, targetId, newRole));

    [HttpPatch("merge/{keepId}/{removeId}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Merge two organisations")]
    [SwaggerResponse(204, "Organisations merged")]
    [SwaggerResponse(404, "Organisation not found")]
    [SwaggerResponse(400, "Invalid request")]
    public async Task<IActionResult> Merge(Guid keepId, Guid removeId)
    {
        if (keepId == removeId)
            return Problem("Cannot merge organisation with itself.", statusCode: 400);

        if (!await organisationService.ExistsAsync(keepId) || !await organisationService.ExistsAsync(removeId))
            return Problem("One or both organisations not found.", statusCode: 404);

        await organisationService.MergeAsync(keepId, removeId);
        return NoContent();
    }

    [HttpGet("suggestions")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Get merge suggestions based on name similarity")]
    [SwaggerResponse(200, "List of suggestions")]
    public async Task<IActionResult> Suggestions([FromQuery] float threshold = 0.6f, [FromQuery] int limit = 20)
        => Ok(await organisationService.GetMergeSuggestionsAsync(threshold, limit));
}
