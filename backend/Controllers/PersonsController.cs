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
public class PersonsController(PersonService personService, IBackgroundTaskQueue taskQueue) : AppControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Get persons with optional search and pagination")]
    [SwaggerResponse(200, "List of persons")]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool trash = false)
    {
        Expression<Func<Person, bool>> predicate = search != null
            ? p => (EF.Functions.TrigramsAreSimilar(p.Name, search) ||
                    EF.Functions.ILike(p.Name, $"%{search}%")) &&
                    p.Trashed == trash
            : p => p.Trashed == trash;

        var (items, totalCount) = await personService.GetPageAsync(page, pageSize, predicate);

        return Ok(new { items, totalCount });
    }

    [HttpPut]
    [SwaggerOperation(Summary = "Create a person")]
    [SwaggerResponse(200, "Person created", typeof(Guid))]
    [SwaggerResponse(409, "Person already exists", typeof(Guid))]
    public async Task<IActionResult> Create([FromBody] PersonCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Name is required.", statusCode: 400);

        Guid? existingId = await personService.FindIdByNameAsync(dto.Name);

        if (existingId != null)
            return Conflict(existingId);

        Guid createdBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Guid id = await personService.CreateAsync(dto, createdBy);

        taskQueue.QueueBackgroundWorkItem(async token =>
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            IngestionService ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
            await ingestionService.RunPersonEntityPipelineAsync(id);
        });

        return Ok(id);
    }

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Get person details")]
    [SwaggerResponse(200, "Details of the person", typeof(PersonDetailDto))]
    [SwaggerResponse(404, "Person not found")]
    public async Task<IActionResult> GetDetails(Guid id)
        => OkOrNotFound(await personService.GetDetailAsync(id));

    [HttpPatch("{id}")]
    [SwaggerOperation(Summary = "Update a person")]
    [SwaggerResponse(204, "Person updated")]
    [SwaggerResponse(404, "Person not found")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PersonUpdateDto dto)
    {
        bool found = await personService.UpdateAsync(id, p =>
        {
            if (dto.Name != null) p.Name = dto.Name;
            if (dto.Description != null) p.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description;
            if (dto.EmailAddress != null) p.EmailAddress = string.IsNullOrWhiteSpace(dto.EmailAddress) ? null : dto.EmailAddress;
            if (dto.Occupation != null) p.Occupation = string.IsNullOrWhiteSpace(dto.Occupation) ? null : dto.Occupation;
            if (dto.Linkedin != null) p.Linkedin = string.IsNullOrWhiteSpace(dto.Linkedin) ? null : dto.Linkedin;
        });

        if (!found) return Problem("Person not found.", statusCode: 404);

        taskQueue.QueueBackgroundWorkItem(async token =>
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            IngestionService ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
            await ingestionService.RunPersonEntityPipelineAsync(id);
        });

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Delete a person permanently")]
    [SwaggerResponse(204, "Person deleted permanently.")]
    [SwaggerResponse(404, "Person not found")]
    public async Task<IActionResult> Delete(Guid id)
        => NoContentOrNotFound(await personService.DeleteAsync(id));

    [HttpPatch("{id}/trash")]
    [SwaggerOperation(Summary = "Move person to trash")]
    [SwaggerResponse(204, "Person moved to trash.")]
    [SwaggerResponse(404, "Person not found")]
    public async Task<IActionResult> Trash(Guid id)
        => NoContentOrNotFound(await personService.TrashAsync(id));

    [HttpPatch("{id}/untrash")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Restore person from trash")]
    [SwaggerResponse(204, "Person restored from trash.")]
    [SwaggerResponse(404, "Person not found")]
    public async Task<IActionResult> Untrash(Guid id)
        => NoContentOrNotFound(await personService.UntrashAsync(id));

    [HttpPost("{id}/authored-resources/{targetId}")]
    [SwaggerOperation(Summary = "Add authored resource")]
    [SwaggerResponse(204, "Relation added")]
    public async Task<IActionResult> AddAuthoredResource(Guid id, Guid targetId)
    {
        await personService.AddAuthoredResourceAsync(id, targetId);
        return NoContent();
    }

    [HttpDelete("{id}/authored-resources/{targetId}")]
    [SwaggerOperation(Summary = "Remove authored resource")]
    [SwaggerResponse(204, "Relation removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveAuthoredResource(Guid id, Guid targetId)
        => NoContentOrNotFound(await personService.RemoveAuthoredResourceAsync(id, targetId));

    [HttpPost("{id}/related-resources/{targetId}")]
    [SwaggerOperation(Summary = "Add related resource")]
    [SwaggerResponse(204, "Relation added")]
    public async Task<IActionResult> AddRelatedResource(Guid id, Guid targetId, [FromQuery] string? role)
    {
        await personService.AddRelatedResourceAsync(id, targetId, role);
        return NoContent();
    }

    [HttpDelete("{id}/related-resources/{targetId}")]
    [SwaggerOperation(Summary = "Remove related resource")]
    [SwaggerResponse(204, "Relation removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveRelatedResource(Guid id, Guid targetId)
        => NoContentOrNotFound(await personService.RemoveRelatedResourceAsync(id, targetId));

    [HttpPatch("{id}/related-resources/{targetId}/role")]
    [SwaggerOperation(Summary = "Update related resource role")]
    [SwaggerResponse(204, "Role updated")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> UpdateRelatedResourceRole(Guid id, Guid targetId, [FromQuery] string newRole)
        => NoContentOrNotFound(await personService.UpdateRelatedResourceRoleAsync(id, targetId, newRole));

    [HttpPost("{id}/related-persons/{targetId}")]
    [SwaggerOperation(Summary = "Add related person")]
    [SwaggerResponse(204, "Relation added")]
    public async Task<IActionResult> AddRelatedPerson(Guid id, Guid targetId, [FromQuery] string? role)
    {
        await personService.AddPersonRelationshipAsync(id, targetId, role);
        return NoContent();
    }

    [HttpDelete("{id}/related-persons/{targetId}")]
    [SwaggerOperation(Summary = "Remove related person")]
    [SwaggerResponse(204, "Relation removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveRelatedPerson(Guid id, Guid targetId)
        => NoContentOrNotFound(await personService.RemovePersonRelationshipAsync(id, targetId));

    [HttpPatch("{id}/related-persons/{targetId}/role")]
    [SwaggerOperation(Summary = "Update related person role")]
    [SwaggerResponse(204, "Role updated")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> UpdateRelatedPersonRole(Guid id, Guid targetId, [FromQuery] string newRole)
        => NoContentOrNotFound(await personService.UpdatePersonRelationshipRoleAsync(id, targetId, newRole));

    [HttpPost("{id}/organisations/{targetId}")]
    [SwaggerOperation(Summary = "Add organisation relation")]
    [SwaggerResponse(204, "Relation added")]
    public async Task<IActionResult> AddOrganisation(Guid id, Guid targetId, [FromQuery] string? role)
    {
        await personService.AddOrganisationRelationAsync(id, targetId, role);
        return NoContent();
    }

    [HttpDelete("{id}/organisations/{targetId}")]
    [SwaggerOperation(Summary = "Remove organisation relation")]
    [SwaggerResponse(204, "Relation removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveOrganisation(Guid id, Guid targetId)
        => NoContentOrNotFound(await personService.RemoveOrganisationRelationAsync(id, targetId));

    [HttpPatch("{id}/organisations/{targetId}/role")]
    [SwaggerOperation(Summary = "Update organisation relation role")]
    [SwaggerResponse(204, "Role updated")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> UpdateOrganisationRole(Guid id, Guid targetId, [FromQuery] string newRole)
        => NoContentOrNotFound(await personService.UpdateOrganisationRelationRoleAsync(id, targetId, newRole));

    [HttpPatch("merge/{keepId}/{removeId}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Merge two persons")]
    [SwaggerResponse(204, "Persons merged.")]
    [SwaggerResponse(404, "Person not found")]
    [SwaggerResponse(400, "Invalid request")]
    public async Task<IActionResult> Merge(Guid keepId, Guid removeId)
    {
        if (keepId == removeId)
            return Problem("Cannot merge person with itself.", statusCode: 400);

        if (!await personService.ExistsAsync(keepId) || !await personService.ExistsAsync(removeId))
            return Problem("One or both persons not found.", statusCode: 404);

        await personService.MergeAsync(keepId, removeId);
        return NoContent();
    }

    [HttpGet("suggestions")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Get merge suggestions based on name similarity")]
    [SwaggerResponse(200, "List of suggestions", typeof(List<MergeSuggestion>))]
    public async Task<IActionResult> Suggestions([FromQuery] float threshold = 0.6f, [FromQuery] int limit = 20)
        => Ok(await personService.GetMergeSuggestionsAsync(threshold, limit));
}