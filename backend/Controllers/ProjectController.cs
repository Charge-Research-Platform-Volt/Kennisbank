using System.Security.Claims;
using KnowledgeBank.Models;
using KnowledgeBank.Services.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("projects")]
[Authorize]
public class ProjectController(ProjectService projectService, TagService tagService, LibraryService libraryService) : AppControllerBase
{
    [HttpPost]
    [SwaggerOperation(Summary = "List projects with filters and pagination")]
    [SwaggerResponse(200, "Project list")]
    public async Task<IActionResult> List([FromBody] ProjectListRequest request)
        => Ok(await projectService.GetListAsync(request));

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Get project info with children, items, and ancestors")]
    [SwaggerResponse(200, "Project info")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> Info(Guid id)
    {
        var info = await projectService.GetInfoAsync(id);
        if (info == null) return NotFound();
        return Ok(info);
    }

    [HttpGet("{rootId}/folders")]
    [SwaggerOperation(Summary = "Get all subfolders of a project")]
    [SwaggerResponse(200, "Folder list")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> GetAllFolders(Guid rootId)
    {
        if (!await projectService.ExistsAsync(rootId)) return NotFound();
        return Ok(await projectService.GetAllFoldersAsync(rootId));
    }

    [HttpPost("create")]
    [SwaggerOperation(Summary = "Create a new root project")]
    [SwaggerResponse(201, "Project created")]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(409, "Title already exists")]
    public async Task<IActionResult> Create([FromBody] ProjectCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return Problem("Title is required.", statusCode: 400);

        if (dto.ProjectType != "root")
            return Problem("ProjectType must be 'root'.", statusCode: 400);

        if (await projectService.ExistsAsync(p => p.Title == dto.Title))
            return Problem("A project with this title already exists.", statusCode: 409);

        foreach (var tagId in dto.Tags)
            if (!await tagService.ExistsAsync(tagId))
                return Problem("One or more tags do not exist.", statusCode: 400);

        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Guid projectId = await projectService.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(Info), new { id = projectId }, new { projectId });
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Delete a project")]
    [SwaggerResponse(204, "Deleted")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Project? project = await projectService.GetByIdAsync(id, includeMembers: true);
        if (project == null) return NotFound();

        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!IsMemberOrAdmin(userId, project)) return Forbid();

        await projectService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}")]
    [SwaggerOperation(Summary = "Update project title or description")]
    [SwaggerResponse(204, "Updated")]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectDto dto)
    {
        if (dto.Title == null && dto.Description == null)
            return Problem("No updates provided.", statusCode: 400);

        Project? project = await projectService.GetByIdAsync(id, includeMembers: true);
        if (project == null) return NotFound();

        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!IsMemberOrAdmin(userId, project)) return Forbid();

        if (dto.Title != null && string.IsNullOrWhiteSpace(dto.Title))
            return Problem("Title cannot be empty.", statusCode: 400);

        if (dto.Description != null && project.ProjectType != "root")
            return Problem("Description can only be set on root projects.", statusCode: 400);

        await projectService.UpdateAsync(id, p =>
        {
            if (dto.Title != null) p.Title = dto.Title.Trim();
            if (dto.Description != null) p.Description = dto.Description;
        });

        return NoContent();
    }

    [HttpPatch("{id}/tags")]
    [SwaggerOperation(Summary = "Set project tags")]
    [SwaggerResponse(204, "Updated")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> SetTags(Guid id, [FromBody] Guid[] tagIds)
    {
        Project? project = await projectService.GetByIdAsync(id, includeMembers: true);
        if (project == null) return NotFound();

        if (project.ProjectType != "root")
            return Problem("Tags can only be set on root projects.", statusCode: 400);

        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!IsMemberOrAdmin(userId, project)) return Forbid();

        await projectService.SetTagsAsync(id, tagIds);
        return NoContent();
    }

    [HttpPatch("{id}/members")]
    [SwaggerOperation(Summary = "Set project members")]
    [SwaggerResponse(204, "Updated")]
    [SwaggerResponse(400, "Members cannot be empty")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> SetMembers(Guid id, [FromBody] Guid[] memberIds)
    {
        if (memberIds.Length == 0)
            return Problem("Members cannot be empty.", statusCode: 400);

        Project? project = await projectService.GetByIdAsync(id, includeMembers: true);
        if (project == null) return NotFound();

        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!IsMemberOrAdmin(userId, project)) return Forbid();

        await projectService.SetMembersAsync(id, memberIds);
        return NoContent();
    }

    [HttpPost("{parentId}/folders")]
    [SwaggerOperation(Summary = "Add a folder to a project or folder")]
    [SwaggerResponse(201, "Folder created")]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(404, "Parent not found")]
    public async Task<IActionResult> AddFolder(Guid parentId, [FromBody] AddFolderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Problem("Folder name is required.", statusCode: 400);

        if (!await projectService.ExistsAsync(parentId)) return NotFound();

        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Guid folderId = await projectService.AddFolderAsync(parentId, dto.Name.Trim(), userId);
        return CreatedAtAction(nameof(Info), new { id = folderId }, new { folderId });
    }

    [HttpPost("{projectId}/items/{itemId}")]
    [SwaggerOperation(Summary = "Add a library item to a project or folder")]
    [SwaggerResponse(204, "Added")]
    [SwaggerResponse(404, "Project or item not found")]
    [SwaggerResponse(409, "Item already linked")]
    public async Task<IActionResult> AddItem(Guid projectId, Guid itemId)
    {
        if (!await projectService.ExistsAsync(projectId)) return NotFound();
        if (!await libraryService.ItemExistsAsync(itemId)) return NotFound();
        if (await projectService.ItemAlreadyLinkedAsync(projectId, itemId))
            return Problem("Item already in project.", statusCode: 409);

        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await projectService.AddItemAsync(projectId, itemId, userId);
        return NoContent();
    }

    [HttpDelete("{projectId}/items/{itemId}")]
    [SwaggerOperation(Summary = "Remove a library item from a project or folder")]
    [SwaggerResponse(204, "Removed")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> RemoveItem(Guid projectId, Guid itemId)
    {
        Project? project = await projectService.GetByIdAsync(projectId, includeMembers: true, includeItems: true);
        if (project == null) return NotFound();

        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        bool userAddedItem = project.ProjectItemRelations?.Any(r => r.ItemId == itemId && r.AddedBy == userId.ToString()) ?? false;

        if (!userAddedItem && !IsMemberOrAdmin(userId, project)) return Forbid();

        await projectService.RemoveItemAsync(projectId, itemId);
        return NoContent();
    }

    private bool IsMemberOrAdmin(Guid userId, Project project)
        => User.IsInRole("admin") ||
           (project.ProjectMemberRelations?.Any(r => r.UserId == userId.ToString()) ?? false);
}