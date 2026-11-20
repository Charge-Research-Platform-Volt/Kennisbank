using System.Security.Claims;
using KnowledgeBank.Models.ProjectDocuments;
using KnowledgeBank.Services.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("project-document-types")]
[Authorize]
public class ProjectDocumentTypesController(ProjectDocumentTypeService projectDocumentTypeService) : AppControllerBase
{
    private const int MAX_NAME_LENGTH = 100;

    [HttpGet]
    [SwaggerOperation(Summary = "Get all project document types")]
    [SwaggerResponse(200, "List of document types")]
    public async Task<IActionResult> Get()
        => Ok(await projectDocumentTypeService.GetAllAsync());

    [HttpPut]
    [SwaggerOperation(Summary = "Create a project document type")]
    [SwaggerResponse(200, "Document type created", typeof(Guid))]
    [SwaggerResponse(409, "Document type already exists", typeof(Guid))]
    public async Task<IActionResult> Create([FromBody] ProjectDocumentTypeCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Name is required.", statusCode: 400);

        if (dto.Name.Trim().Length > MAX_NAME_LENGTH)
            return Problem($"Name too long (max {MAX_NAME_LENGTH} characters).", statusCode: 400);

        Guid? existingId = await projectDocumentTypeService.FindIdByNameAsync(dto.Name);
        if (existingId != null) 
            return Conflict(existingId);

        Guid createdBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await projectDocumentTypeService.CreateAsync(dto.Name, createdBy));
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Delete an unused project document type")]
    [SwaggerResponse(204, "Document type deleted")]
    [SwaggerResponse(403, "Action forbidden")]
    [SwaggerResponse(404, "Document type not found")]
    [SwaggerResponse(409, "Document type is in use")]
    public async Task<IActionResult> Delete(Guid id)
    {
        ProjectDocumentType? type = await projectDocumentTypeService.GetByIdAsync(id);
        if (type == null) return Problem("Document type not found.", statusCode: 404);

        // The built-in types have no creator, so only an admin can remove those
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!User.IsInRole("admin") && type.CreatedBy != userId)
            return Problem("Cannot delete this document type.", statusCode: 403);

        // Unlike resource types there is no fallback, so a type in use stays until its documents move to another type
        int usageCount = await projectDocumentTypeService.CountUsageAsync(id);
        if (usageCount > 0)
            return Problem($"This type is used by {usageCount} document(s).", statusCode: 409);

        await projectDocumentTypeService.DeleteAsync(id);
        return NoContent();
    }
}