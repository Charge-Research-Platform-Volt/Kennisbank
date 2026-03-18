using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq.Expressions;
using KnowledgeBank.Utils;
using KnowledgeBank.Responses;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Authorize]
public class ProjectController(ProjectManager projectManager, ResourceManager resourceManager) : ControllerBase
{
    private readonly ProjectManager projectManager = projectManager;
    private readonly ResourceManager resourceManager = resourceManager;

    #region Create
    /// <summary>
    /// Adds a new project given a dto.
    /// 
    /// Author: Justin Liem
    /// </summary>
    /// <param name="dto">The DTO for project creation.</param>
    /// <returns>
    /// Returns a 200 OK response containing the added project.
    /// </returns>
    [HttpPut("create")]
    [SwaggerOperation(
            Summary = "Creates a new project.",
            Description = "Creates a new project, given a dto"
        )]
    [SwaggerResponse(200, "New project created", typeof(Guid))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> Create([FromBody] ProjectCreateDto dto)
    {
        Log.Information("Creating a new project.");

        // Make sure we have the required fields
        if (string.IsNullOrEmpty(dto.Title))
        {
            Log.Error("Title is required");
            return BadRequest(new ApiResponse(false, "Title is required"));
        }

        // If the project type is empty or is not root, it is not a new project, but a folder or something else
        if (string.IsNullOrEmpty(dto.ProjectType) || dto.ProjectType != "root")
        {
            Log.Error("Type invalid");
            return BadRequest(new ApiResponse(false, "Type invalid"));
        }

        // Mandatory check: look if project already exists with the same name
        bool projectExists = await projectManager.ProjectExistsAsync(project => project.Title == dto.Title);

        if (projectExists)
        {
            Log.Error("Project already exists.");
            return Conflict(new ApiResponse(false, "Project already exists."));
        }

        // If tags are added, make sure that they exist
        if (dto.Tags != null && dto.Tags.Length > 0)
        {
            foreach (string tagId in dto.Tags)
            {
                if (!await resourceManager.TagExistsAsync(Guid.Parse(tagId)))
                {
                    Log.Error("One or more tags do not exist");
                    return BadRequest(new ApiResponse(false, "One or more tags do not exist"));
                }
            }
        }

        Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;

        if (userId == null)
            throw new Exception("No current user found");

        // Always add the actual creator to the list
        dto.Creators = dto.Creators.Append(userId.Value.ToString()).ToArray();

        // Add the project to the database using the ProjectManager class
        try
        {
            // Create the project
            Guid projectId = await projectManager.CreateProject(dto);

            // Adding the project was successful
            Log.Information("New project added to database.");
            return Ok(new ApiResponse(true, "Project added successfully.", projectId));
        }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException postgresEx && postgresEx.SqlState == "23505")
        {
            // The project already exists
            Log.Error(e, "Project already exists.");
            return Conflict(new ApiResponse(false, "Project already exists."));
        }
        catch (Exception e)
        {
            // Something else went wrong
            Log.Error(e, "Failed to add project.");
            return StatusCode(500, new ApiResponse(false, "Internal server error"));
        }
    }
    #endregion

    #region Delete
    /// <summary>
    /// Deletes a project or folder from the database.
    /// 
    /// Author: Justin Liem
    /// </summary>
    /// <param name="projectId">The id of the project or folder to delete.</param>
    /// <returns>
    /// Returns a 200 OK response if all went well.
    /// </returns>
    [HttpDelete("delete/{projectId}")]
    [Authorize]
    [SwaggerOperation(
            Summary = "Deletes a project (or folder).",
            Description = "Only creators of the project / folder OR admins are able to delete it."
        )]
    [SwaggerResponse(200, "Project deleted")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(403, "Forbidden - User cannot delete this project")]
    [SwaggerResponse(404, "Project not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> Delete(string projectId)
    {
        try
        {
            Log.Information("Deleting project.");

            // Make sure we have the required fields
            if (string.IsNullOrEmpty(projectId) || !ValidityUtil.IsValidId(projectId))
            {
                Log.Error("Id is required");
                return BadRequest(new ApiResponse(false, "Id is required"));
            }

            // Get the GUID of the user
            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;

            // Get the project
            Project? project = await projectManager.GetProjectAsync(projectId, includeProperties: "ProjectCreatorRelations");

            if (project == null)
            {
                Log.Error("Project not found.");
                return NotFound(new ApiResponse(false, "Project not found."));
            }

            // Check if the user has permission to delete this project
            if (ProjectAuthorizationLevel(userId, project) == "unauthorized")
            {
                Log.Warning("User {UserId} attempted to delete {id} without permissions.", userId, projectId);
                return StatusCode(403, new ApiResponse(false, "User cannot delete this project or folder."));
            }
            // Delete project / folder and all its subfolders and resources, creators, tags links
            if (await projectManager.DeleteProject(projectId))
                return Ok(new ApiResponse(true, "Project deleted."));

            Log.Error("Failed to delete project.");
            return StatusCode(500, new ApiResponse(false, "Internal server error."));
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting project {id}", projectId);
            return StatusCode(500, new ApiResponse(false, "Internal server error."));
        }
    }

    #endregion

    #region List

    /// <summary>
    /// Retrieves projects, given a dto to filter on.
    /// The query is a string that is used to search for projects by title.
    ///
    /// Author: Justin Liem
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>
    /// Returns a 200 OK response containing the projects.
    /// </returns>
    [HttpPost("list")]
    [SwaggerOperation(
        Summary = "Retrieves projects",
        Description = "Retrieves projects, given a dto to filter on"
    )]
    [SwaggerResponse(200, "Projects fetched")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> List([FromBody] FilterProjectDto dto)
    {
        try
        {
            // Validate paging parameters if using paging
            if (dto.UsePaging)
            {
                if (dto.PageIndex < 1)
                    return BadRequest(new ApiResponse(false, "Page index cannot be lower than 1."));

                if (dto.PageSize < 1)
                    return BadRequest(new ApiResponse(false, "PAge size cannot be lower than 1."));
            }

            // Build the predicate used to filter the projects by giving it to the BuildPredicate function
            Expression<Func<Project, bool>> predicate = BuildPredicate(dto);
            
            Project[] projects = [];
            
            if (dto.UsePaging)
            {
                projects = await projectManager.GetProjectPageAsync(
                    pageIndex: dto.PageIndex,
                    pageSize: dto.PageSize,
                    predicate: predicate,
                    includeProperties: ["ProjectTagRelations", "ProjectCreatorRelations"]
                );
            }
            else
            {
                projects = await projectManager.GetAllProjectsAsync(
                    predicate: predicate,
                    includeProperties: ["ProjectTagRelations", "ProjectCreatorRelations"]
                );
            }
            if (projects.Length == 0)
            {
                if (dto.UsePaging && dto.PageIndex > 1)
                    return BadRequest(new ApiResponse(false, "The page index is invalid"));
                else
                {
                    return Ok(new ApiResponse(true, "No projects found", new ProjectPageResponse([])));
                }
            }

            if (dto.UsePaging)
            {
                // Calculate the total number of projects and return a ProjectPageResponse
                int totalCount = await projectManager.ProjectCount(predicate);
                int pageCount = (int)Math.Ceiling((double)totalCount / dto.PageSize);
                return Ok(new ApiResponse(true, $"{projects.Length} project(s) found.", new ProjectPageResponse(projects, dto.PageIndex, dto.PageSize, pageCount)));
            }

            return Ok(new ApiResponse(true, $"{projects.Length} project(s) found.", new ProjectPageResponse(projects)));
        }

        catch (Exception e)
        {
            Log.Error(e, "Failed to fetch folders");
            return StatusCode(500, new ApiResponse(false, "Internal server error"));
        }
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates a property of the project itself.
    /// 
    /// Author: Justin Liem
    /// </summary>
    /// <param name="projectId">The ID of the project or folder to update.</param>
    /// <param name="dto">DTO containing the properties to update.</param>
    /// <returns>
    /// Returns a 200 OK response if all went well.
    /// </returns>
    [HttpPatch("update/{projectId}")]
    [Authorize]
    [SwaggerOperation(
            Summary = "Updates a project.",
            Description = "Updates properties of the project, only creators or admins can change them."
        )]
    [SwaggerResponse(200, "Project updated")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(403, "Forbidden - User cannot update this project")]
    [SwaggerResponse(404, "Project not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> Update(string projectId, [FromBody] UpdateProjectDto dto)
    {
        try
        {
            Log.Information("Updating project.");

            if (!ValidityUtil.IsValidId(projectId))
                return BadRequest(new ApiResponse(false, "Id is required"));

            if (dto == null || (dto.Title == null && dto.Description == null && dto.Tags == null && dto.Creators == null))
                return BadRequest(new ApiResponse(false, "No updates were provided."));

            Project? project = await projectManager.GetProjectAsync(projectId, includeProperties: ["ProjectCreatorRelations"]);
            if (project == null)
                return NotFound(new ApiResponse(false, "Project not found."));

            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
            bool userIsAdmin = User.IsInRole("admin");

            if (!userIsAdmin && ProjectAuthorizationLevel(userId, project) == "unauthorized")
            {
                Log.Warning("User {UserId} attempted to update project {id} without permissions.", userId, projectId);
                return StatusCode(403, new ApiResponse(false, "User cannot update this project."));
            }

            await projectManager.BeginTransaction();

            if (dto.Title != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Title))
                {
                    await projectManager.Rollback();
                    return BadRequest(new ApiResponse(false, "Title cannot be empty."));
                }
                await projectManager.UpdateProjectAsync(project.Id, p => p.Title, dto.Title);
            }

            if (dto.Description != null)
            {
                if (project.ProjectType != "root")
                {
                    await projectManager.Rollback();
                    return BadRequest(new ApiResponse(false, "Description can only be set on root projects."));
                }
                await projectManager.UpdateProjectAsync(project.Id, p => p.Description, dto.Description);
            }

            if (dto.Tags != null)
            {
                if (project.ProjectType != "root")
                {
                    await projectManager.Rollback();
                    return BadRequest(new ApiResponse(false, "Tags can only be set on root projects."));
                }
                await projectManager.UpdateProjectTagsAsync(project.Id, dto.Tags.ToArray());
            }

            if (dto.Creators != null)
            {
                if (dto.Creators.Count == 0)
                {
                    await projectManager.Rollback();
                    return BadRequest(new ApiResponse(false, "Creators cannot be empty."));
                }
                await projectManager.UpdateProjectCreatorsAsync(project.Id, dto.Creators.ToArray());
            }

            await projectManager.Commit();
            return Ok(new ApiResponse(true, "Project updated successfully."));
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating project {id}", projectId);
            return StatusCode(500, new ApiResponse(false, "Internal server error."));
        }
    }

    #endregion

    #region Info

    /// <summary>
    /// Gets the project and its direct children (resources and folders).
    /// 
    /// Author: Jelle v.h. Schut
    /// </summary>
    /// <param name="id">The ID of the project / folder</param>
    /// <returns>
    /// Returns a 200 OK response containing the project and its children.
    /// </returns>
    [HttpGet("info/{id}")]
    [SwaggerOperation(Summary = "Get the direct children of the project")]
    [SwaggerResponse(200, "Project Information", typeof(ApiResponse))]
    [SwaggerResponse(404, "Project Not Found", typeof(ApiResponse))]
    [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
    public async Task<IActionResult> Info(string id)
    {
        // Check if ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "ID is invalid."));

        try
        {
            Project? project = await projectManager.GetProjectChildrenAsync(id);

            if (project == null)
                return NotFound(new ApiResponse(false, "Project not found."));

            // Resolve folder AddedBy user names
            HashSet<string> folderUserIds = project.ChildFolders?
                .Where(pfr => pfr.AddedBy != null)
                .Select(pfr => pfr.AddedBy!)
                .ToHashSet() ?? [];

            Dictionary<string, string> userNames = await projectManager.GetUserNamesByIds(folderUserIds);

            List<FolderWithAddedBy> folders = project.ChildFolders?
                .Where(r => r?.ChildFolder != null)
                .Select(r => new FolderWithAddedBy(
                    r.ChildFolder!,
                    r.AddedBy != null && userNames.TryGetValue(r.AddedBy, out string? name) ? name ?? "Unknown" : "Unknown"))
                .ToList() ?? [];

            // Items are resolved via ResourceGridView (covers resources, persons, organisations)
            List<ResourceGridItemWithAddedBy> items = await projectManager.GetProjectItemsAsync(Guid.Parse(id));

            List<UserResponse> creators = project.ProjectCreatorRelations?
                .Select(r => new UserResponse(r.Creator!, "No Role")).ToList() ?? [];

            List<Tag?> tags = project.ProjectTagRelations?
                .Select(r => r.Tag).ToList() ?? [];

            ProjectInfoDto projectInfo = new()
            {
                Project = project,
                Items = items,
                Folders = folders,
                Creators = creators,
                Tags = tags
            };

            return Ok(new ApiResponse(true, "Project found.", projectInfo));
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting project {id}", id);
            return StatusCode(500, new ApiResponse(false, "Internal server error.", e.Message));
        }
    }

    #endregion

    #region Add Folder

    /// <summary>
    /// Adds a new folder given a name and parent.
    /// 
    /// Author: Justin Liem
    /// </summary>
    /// <param name="parentId">ID of the parent project or folder.</param>
    /// <param name="dto">DTO containing the folder name.</param>
    /// <returns>
    /// Returns a 200 OK response containing the ID of the added folder.
    /// </returns>
    [HttpPut("add-folder/{parentId}")]
    [SwaggerOperation(
        Summary = "Adds a folder to a parent project or folder.",
        Description = "Creates an empty folder inside a project or folder. Creators are inherited from the parent plus whoever created this."
    )]
    [SwaggerResponse(200, "Folder created")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Project not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> AddFolder(string parentId, [FromBody] AddFolderDto dto)
    {
        Log.Information("Creating a new folder");

        // Make sure we have the required fields
        if (string.IsNullOrEmpty(dto.Name) || !ValidityUtil.IsValidId(parentId))
        {
            Log.Error("Folder name and valid parent id are required");
            return BadRequest(new ApiResponse(false, "Folder name and valid parent id are required"));
        }

        // Get parent project and user ID
        Project? parent = await projectManager.GetProjectAsync(parentId, includeProperties: "ProjectCreatorRelations");

        if (parent == null)
        {
            Log.Error("Parent component does not exist.");
            return NotFound(new ApiResponse(false, "Parent component does not exist"));
        }

        Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;

        // If there is somehow no user found calling this action, abort
        if (userId == null)
        {
            Log.Error("Failed to add folder.");
            return StatusCode(500, new ApiResponse(false, "Internal server error"));
        }

        // Otherwise grab the creators of the parent component, add the current user to that set and then create an empty folder with the name given and same language code
        HashSet<string> creators = parent.ProjectCreatorRelations?.Select(relation => relation.CreatorId).ToHashSet() ?? [];
        creators.Add(userId.ToString() ?? throw new Exception("User ID is null"));

        // Folders don't have descriptions
        ProjectCreateDto newFolder = new()
        {
            Title = dto.Name,
            ProjectType = "folder",
            Creators = creators.ToArray()
        };

        try
        {
            // Create folder and add to parent with a relation
            Guid folderId = await projectManager.CreateProject(newFolder);
            await projectManager.AddFolderToProjectAsync(parentId, folderId, userId.Value);

            Log.Information("New folder {folderId} added to parent {parentId}.", folderId, parentId);
            return Ok(new ApiResponse(true, "Folder added successfully.", folderId));
        }

        catch (Exception e)
        {
            // Something else went wrong
            Log.Error(e, "Failed to add folder.");
            return StatusCode(500, new ApiResponse(false, "Internal server error"));
        }
    }

    #endregion

    #region Add Item

    /// <summary>
    /// Adds a library item (resource, person, or organisation) to a project or folder.
    /// </summary>
    [HttpPut("add-item/{projectId}/{itemId}")]
    [SwaggerOperation(
        Summary = "Adds a library item to a project or folder.",
        Description = "Links a resource, person, or organisation to a project/folder by ID."
    )]
    [SwaggerResponse(200, "Item added")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Project or item not found")]
    [SwaggerResponse(409, "Item already linked")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> AddItem(string projectId, string itemId)
    {
        if (!ValidityUtil.IsValidId(projectId) || !ValidityUtil.IsValidId(itemId))
            return BadRequest(new ApiResponse(false, "Invalid id."));

        try
        {
            if (!await projectManager.ProjectExistsAsync(projectId))
                return NotFound(new ApiResponse(false, "Project not found."));

            // Validate item exists in the grid view (covers resources, persons, organisations)
            bool itemExists = await projectManager.ItemExistsInGridAsync(Guid.Parse(itemId));
            if (!itemExists)
                return NotFound(new ApiResponse(false, "Item not found."));

            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
            if (userId == null)
                return StatusCode(500, new ApiResponse(false, "Internal server error"));

            if ((await projectManager.GetAllItems(predicate: r => r.ProjectId == Guid.Parse(projectId) && r.ItemId == Guid.Parse(itemId))).Length != 0)
                return Conflict(new ApiResponse(false, "Item already in project / folder."));

            await projectManager.AddItemToProjectAsync(projectId, itemId, userId.Value);
            return Ok(new ApiResponse(true, "Item added to project."));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to add item {itemId} to project {projectId}.", itemId, projectId);
            return StatusCode(500, new ApiResponse(false, "Internal server error"));
        }
    }

    #endregion

    #region Remove Item

    /// <summary>
    /// Removes a library item (resource, person, or organisation) from a project or folder.
    /// </summary>
    [HttpDelete("remove-item/{projectId}/{itemId}")]
    [SwaggerOperation(
        Summary = "Removes a library item from a project or folder.",
        Description = "Only the user who added the item or a project creator or admin can remove it."
    )]
    [SwaggerResponse(200, "Item removed")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Project or item not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> RemoveItem(string projectId, string itemId)
    {
        if (!ValidityUtil.IsValidId(projectId) || !ValidityUtil.IsValidId(itemId))
            return BadRequest(new ApiResponse(false, "Invalid id."));

        try
        {
            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
            bool userIsAdmin = User.IsInRole("admin");

            Project? project = await projectManager.GetProjectAsync(projectId, includeProperties: ["ProjectItemRelations", "ProjectCreatorRelations"]);
            if (project == null)
                return NotFound(new ApiResponse(false, "Project not found."));

            bool userAddedItem = project.ProjectItemRelations?.Any(r => r.ItemId == Guid.Parse(itemId) && r.AddedBy == userId.ToString()) ?? false;
            bool userIsAuthorized = ProjectAuthorizationLevel(userId, project) != "unauthorized";

            if (!userIsAdmin && !userAddedItem && !userIsAuthorized)
            {
                Log.Warning("User {UserId} attempted to remove item {itemId} from {projectId} without permissions.", userId, itemId, projectId);
                return StatusCode(403, new ApiResponse(false, "User cannot remove this item."));
            }

            if (await projectManager.RemoveItemFromProject(projectId, itemId))
                return Ok(new ApiResponse(true, "Item removed from project."));

            return StatusCode(500, new ApiResponse(false, "Internal server error."));
        }
        catch (Exception e)
        {
            Log.Error(e, "Error removing item {itemId} from project {projectId}.", itemId, projectId);
            return StatusCode(500, new ApiResponse(false, "Internal server error."));
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the user authorization level for a project, given the project and the user.
    /// </summary>
    private string ProjectAuthorizationLevel(Guid? user, Project project)
    {
        if (user == null)
            return "unauthorized";
        if (User.IsInRole("admin"))
            return "admin";
        string level = "unauthorized";
        if (project.ProjectCreatorRelations?.Any(r => r.CreatorId == user.ToString()) ?? false)
            level = "creator";
        return level;
    }

    /// <summary>
    /// Builds a predicate using a FilterProjectDto, similar to the function of the same name in TagsController.
    /// 
    /// Author: Justin Liem
    /// </summary>
    /// <param name="dto">Dto used for constructing the predicate</param>
    /// <returns>Predicate built from the dto</returns>
    private Expression<Func<Project, bool>> BuildPredicate(FilterProjectDto dto)
    {
        Expression<Func<Project, bool>>? predicate = null;
        predicate = PredicateBuilder.AddAnd(predicate, project => project.ProjectType == "root");

        if (dto.StartDate.HasValue && dto.EndDate.HasValue)
        {
            predicate = PredicateBuilder.AddAnd(predicate, project => dto.StartDate < project.CreationDate && project.CreationDate < dto.EndDate);
        }

        if (!string.IsNullOrEmpty(dto.CreatedBy))
        {
            predicate = PredicateBuilder.AddAnd(predicate, project => project.ProjectCreatorRelations != null && project.ProjectCreatorRelations.Any(rel => rel.CreatorId == dto.CreatedBy));
        }

        if (!string.IsNullOrEmpty(dto.SearchQuery))
        {
            predicate = PredicateBuilder.AddAnd(predicate, project => project.Title.ToUpper().Contains(dto.SearchQuery.ToUpper()));
        }

        if (dto.Tags != null)
        {
            foreach (Guid tag in dto.Tags)
            {
                predicate = PredicateBuilder.AddAnd(predicate, project => project.ProjectTagRelations != null && project.ProjectTagRelations.Any(rel => dto.Tags.Contains(rel.TagId)));
            }
        }
        return predicate;
    }
    #endregion

}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


