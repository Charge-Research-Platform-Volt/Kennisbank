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

[ApiController]
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

    [HttpPost("{id}/relations/{relation}/{targetId}")]
    [SwaggerOperation(Summary = "Add a relation")]
    [SwaggerResponse(204, "Relation added")]
    [SwaggerResponse(400, "Invalid relation type")]
    public async Task<IActionResult> AddRelation(Guid id, string relation, Guid targetId, [FromQuery] string? role)
    {
        switch (relation)
        {
            case "authored-resources":
                await personService.AddAuthoredResourceAsync(id, targetId);
                break;
            case "related-resources":
                await personService.AddRelatedResourceAsync(id, targetId, role);
                break;
            case "related-persons":
                await personService.AddPersonRelationshipAsync(id, targetId, role);
                break;
            case "organisations":
                await personService.AddOrganisationRelationAsync(id, targetId, role);
                break;
            default:
                return Problem("Invalid relation type.", statusCode: 400);
        }

        return NoContent();
    }

    [HttpDelete("{id}/relations/{relation}/{targetId}")]
    [SwaggerOperation(Summary = "Remove a relation")]
    [SwaggerResponse(204, "Relation removed")]
    [SwaggerResponse(404, "Relation not found")]
    [SwaggerResponse(400, "Invalid relation type")]
    public async Task<IActionResult> RemoveRelation(Guid id, string relation, Guid targetId)
    {
        bool? found = relation switch
        {
            "authored-resources" => await personService.RemoveAuthoredResourceAsync(id, targetId),
            "related-resources" => await personService.RemoveRelatedResourceAsync(id, targetId),
            "related-persons" => await personService.RemovePersonRelationshipAsync(id, targetId),
            "organisations" => await personService.RemoveOrganisationRelationAsync(id, targetId),
            _ => null
        };

        if (found == null) return Problem("Invalid relation type.", statusCode: 400);
        return NoContentOrNotFound(found.Value);
    }

    [HttpPatch("{id}/relations/{relation}/{targetId}/role")]
    [SwaggerOperation(Summary = "Update role in relation")]
    [SwaggerResponse(204, "Role updated")]
    [SwaggerResponse(404, "Relation not found")]
    [SwaggerResponse(400, "Invalid relation type")]
    public async Task<IActionResult> UpdateRelationRole(Guid id, string relation, Guid targetId, [FromQuery] string newRole)
    {
        bool? found = relation switch
        {
            "related-resources" => await personService.UpdateRelatedResourceRoleAsync(id, targetId, newRole),
            "related-persons" => await personService.UpdatePersonRelationshipRoleAsync(id, targetId, newRole),
            "organisations" => await personService.UpdateOrganisationRelationRoleAsync(id, targetId, newRole),
            _ => null
        };

        if (found == null) return Problem("Invalid relation type.", statusCode: 400);
        return NoContentOrNotFound(found.Value);
    }

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