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
using System.Text.Json;

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
                if (!await resourceManager.TagExistsAsync(tagId))
                {
                    Log.Error("One or more tags do not exist");
                    return BadRequest(new ApiResponse(false, "One or more tags do not exist"));
                }
            }
        }

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
                    return Ok(new ApiResponse(true, "No projects found", projects));
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
    /// </summary>
    /// <param name="projectId">The ID of the project or folder to delete.</param>
    /// <param name="updates">The properties to update with the new values. </param>
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
    public async Task<IActionResult> Update(string projectId, [FromBody] Dictionary<string, object> updates)
    {
        try
        {
            Log.Information("Updating project.");

            // Make sure we have the required fields
            if (!ValidityUtil.IsValidId(projectId))
            {
                Log.Error("Id is required");
                return BadRequest(new ApiResponse(false, "Id is required"));
            }

            if (updates == null || updates.Count == 0)
                return BadRequest(new ApiResponse(false, "No updates were provided."));

            // Fetch project, if there is no project, throw an error
            Project? project = await projectManager.GetProjectAsync(projectId, includeProperties: ["ProjectCreatorRelations"]);

            if (project == null)
            {
                Log.Error("Project not found.");
                return NotFound(new ApiResponse(false, "Project not found."));
            }

            // Then we begin a transaction as we might need to do a rollback
            await projectManager.BeginTransaction();

            foreach (KeyValuePair<string, object> update in updates)
            {
                string property = update.Key;
                object newValue = update.Value;

                List<string> Ids = new List<string>();

                if (property == "creators" || property == "tags")
                {
                    JsonElement JsonArray = (JsonElement)newValue;
                    Ids = JsonArray.Deserialize<List<string>>() ?? throw new Exception("Could not parse JSONArray to strings");
                    newValue = Ids;
                }

                // If the new value is empty, throw an error
                if (string.IsNullOrEmpty(property) || newValue == null || !ValidUpdate(property, newValue))
                {
                    await projectManager.Rollback();
                    Log.Error("Property empty");
                    return BadRequest(new ApiResponse(false, "Property empty"));
                }

                // Check if user is authorized to edit property
                Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
                bool userIsAdmin = User.IsInRole("admin");

                // If user is NOT an admin AND if he is NOT the creator of the folder where the property is updated, then throw an error
                if (!userIsAdmin && ProjectAuthorizationLevel(userId, project) == "unauthorized")
                {
                    await projectManager.Rollback();
                    Log.Warning("User {UserId} attempted to update property {property} of {id} without permissions.", userId, property, projectId);
                    return StatusCode(403, new ApiResponse(false, "User cannot update this property."));
                }

                await UpdateProperty(project, property, newValue);
            }
            await projectManager.Commit();
            return Ok(new ApiResponse(true, "property successfully updated"));
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

            // Check if the project was found
            if (project == null)
                return NotFound(new ApiResponse(false, "Project not found."));

            // Extract the resources and folders from the project
            List<Resource?>? resources = project.ProjectResourcesRelations?.Select(r => r.Resource).ToList() ?? [];
            List<Project?>? folders = project.ChildFolders?.Select(f => f.ChildFolder).ToList() ?? [];

            // Create the DTO
            ProjectInfoDto projectInfo = new()
            {
                Project = project,
                Resources = resources,
                Folders = folders
            };

            // If null, the project was not found
            if (project == null)
                return NotFound(new ApiResponse(false, "Project not found."));

            // Return the project and its children
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
    /// </summary>
    /// <param name="folderName">Name of the new folder.</param>
    /// <param name="parentId">ID of parent component.</param>
    /// <returns>
    /// Returns a 200 OK response containing an ID of the added folder.
    /// </returns>
    [HttpPut("add-folder/{folderName}/{parentId}")]
    [SwaggerOperation(
        Summary = "Adds a folder to a parent project or folder.",
        Description = "Creates an empty folder to a root folder or project. Does not contain tags. Creators are the creators of the parent component + whoever created this"
    )]
    [SwaggerResponse(200, "Folder created")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Project not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> AddFolder(string folderName, string parentId)
    {
        Log.Information("Creating a new folder");

        // Make sure we have the required fields
        if (string.IsNullOrEmpty(folderName) || !ValidityUtil.IsValidId(parentId))
        {
            Log.Error("Title is required");
            return BadRequest(new ApiResponse(false, "Title is required"));
        }

        // If the parent does not exist, we cannot create a folder
        bool parentExists = await projectManager.ProjectExistsAsync(parentId);

        if (!parentExists)
        {
            Log.Error("Parent component does not exist.");
            return NotFound(new ApiResponse(false, "Parent component does not exist"));
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

        // Folders don't have descriptions (for now)
        ProjectCreateDto newFolder = new()
        {
            Title = folderName,
            ProjectType = "folder",
            Creators = creators.ToArray()
        };

        try
        {
            // Create folder and add to parent with a relation
            Guid folderId = await projectManager.CreateProject(newFolder);
            await projectManager.AddFolderToProjectAsync(parentId, folderId);

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

    #region Add Resource

    /// <summary>
    /// Adds a resource to a project / folder.
    /// </summary>
    /// <param name="projectId">The ID of the project</param>
    /// <param name="resourceId">The ID of the resource</param>
    /// <returns>
    /// Returns a 200 OK response.
    /// </returns>
    [HttpPut("add-resource/{projectId}/{resourceId}")]
    [SwaggerOperation(
        Summary = "Adds a resource to a parent project or folder.",
        Description = "Takes a resource id and project id and links both"
    )]
    [SwaggerResponse(200, "Resource added")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Project / Resource not found")]
    [SwaggerResponse(409, "Resource already linked")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> AddResource(string projectId, string resourceId)
    {
        Log.Information("Adding resource to project");

        // If the project or resource id is invalid, abort
        if (!ValidityUtil.IsValidId(projectId) || !ValidityUtil.IsValidId(resourceId))
        {
            Log.Error("Id of either project or resource invalid");
            return BadRequest(new ApiResponse(false, "Id missing."));
        }

        try
        {
            // Abort if no project or resource is found with those id's
            if (!await projectManager.ProjectExistsAsync(projectId))
            {
                Log.Error("Project not found.");
                return NotFound(new ApiResponse(false, "Project not found."));
            }

            if (!await resourceManager.ResourceExistsAsync(resourceId))
            {
                Log.Error("Resource not found.");
                return NotFound(new ApiResponse(false, "Resource not found."));
            }

            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;

            // If there is somehow no user found calling this action, abort
            if (userId == null)
            {
                Log.Error("Failed to add folder.");
                return StatusCode(500, new ApiResponse(false, "Internal server error"));
            }

            // Also abort if the link already exists
            if ((await projectManager.GetAllResources(predicate: relation => relation.ProjectId == Guid.Parse(projectId) && relation.ResourceId == Guid.Parse(resourceId))).Length != 0)
            {
                Log.Error("Resource-project link already exists.");
                return Conflict(new ApiResponse(false, "Resource already in project / folder."));
            }

            // Now userId always has a value so we can safely take it
            await projectManager.AddResourceToProjectAsync(projectId, resourceId, userId.Value);
            return Ok(new ApiResponse(true, "Successfully added resource to project"));
        }

        catch (Exception e)
        {
            // Something else went wrong
            Log.Error(e, "Failed to add resource.");
            return StatusCode(500, new ApiResponse(false, "Internal server error"));
        }
    }

    #endregion

    #region Remove Resource

    /// <summary>
    /// Removes a resource from a project / folder.
    /// </summary>
    /// <param name="projectId">The ID of the project</param>
    /// <param name="resourceId">The ID of the resource</param>
    /// <returns>
    /// Returns a 200 OK response.
    /// </returns>
    [HttpDelete("remove-resource/{projectId}/{resourceId}")]
    [Authorize]
    [SwaggerOperation(
            Summary = "Removes a resource from a project / folder.",
            Description = "Only people who added the resource to the folder / project OR admins are able to remove it."
        )]
    [SwaggerResponse(200, "Resource removed")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(403, "Forbidden - User cannot remove this resource")]
    [SwaggerResponse(404, "Project / resource not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> RemoveResource(string projectId, string resourceId)
    {
        try
        {
            Log.Information("Deleting resource from project.");

            // Make sure we have the required fields
            if (!ValidityUtil.IsValidId(projectId) || !ValidityUtil.IsValidId(resourceId))
            {
                Log.Error("Id is required");
                return BadRequest(new ApiResponse(false, "Id is required"));
            }

            // Get the GUID of the user
            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
            bool userIsAdmin = User.IsInRole("admin");

            // Check if project and resource exist
            Project? project = await projectManager.GetProjectAsync(projectId, includeProperties: ["ProjectResourcesRelations", "ProjectCreatorRelations"]);

            if (project == null)
            {
                Log.Error("Project not found.");
                return NotFound(new ApiResponse(false, "Project not found."));
            }

            if (!await resourceManager.ResourceExistsAsync(resourceId))
            {
                Log.Error("Resource not found.");
                return NotFound(new ApiResponse(false, "Resource not found."));
            }

            // Check if user is creator of relation or creator of folder where the resource is in
            bool userValidation = (project.ProjectResourcesRelations?.Any(relation => relation.AddedBy == userId) ?? false) || !(ProjectAuthorizationLevel(userId, project) == "unauthorized");

            // Check if the user has permission to delete this project
            if (!userIsAdmin && !userValidation)
            {
                Log.Warning("User {UserId} attempted to delete resource {id} from folder {id2} without permissions.", userId, resourceId, projectId);
                return StatusCode(403, new ApiResponse(false, "User cannot delete resource from this project or folder."));
            }

            // Remove link from project-folder
            if (await projectManager.RemoveResourceFromProject(projectId, resourceId))
                return Ok(new ApiResponse(true, "Resource deleted from project."));

            Log.Error("Failed to delete resource from project.");
            return StatusCode(500, new ApiResponse(false, "Internal server error."));
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting resource {id} from project {id}", resourceId, projectId);
            return StatusCode(500, new ApiResponse(false, "Internal server error."));
        }
    }

    #endregion

    #region Helper Methods

    // Keep in mind, updates to tags and creators are done by just supplying the new tags + creators, so just delete the old ones and make new links
    private async Task UpdateProperty(Project project, string property, object newValue)
    {
        // Update the appropiate property based on the type
        await (property switch
        {
            // Updating general properties
            // Title can only be updated if the user is authorized, can be changed in both folders and projects
            "title" => projectManager.UpdateProjectAsync(project.Id, p => p.Title, newValue.ToString()),
            // Description and tags can only be changed from the root project, and fail if applied to folder
            "description" => project.ProjectType == "root" ? projectManager.UpdateProjectAsync(project.Id, p => p.Description, newValue.ToString()) : throw new UnauthorizedAccessException("User cannot update property used in folder."),

            // Updating relations
            "tags" => project.ProjectType == "root" ? projectManager.UpdateProjectTagsAsync(project.Id, ((List<string>)newValue).ToArray()) : throw new UnauthorizedAccessException("User cannot update property used in folder."),
            // Creators need to updated with a cascading update to make sure permissions are okay
            "creators" => projectManager.UpdateProjectCreatorsAsync(project.Id, ((List<string>)newValue).ToArray()),

            // Default
            _ => throw new ArgumentException($"Cannot update property: {property}")
        });
    }

    /// <summary>
    /// Gets the user authorization level for a project, given the project and the user
    /// </summary>
    /// <param name="user">The user id to verify the authorization level</param>
    /// <param name="project">The project id to verify the authorization level</param>
    /// <returns>authorization level</returns>
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
    /// Checks if update is valid or not
    /// </summary>
    /// <param name="prop">Property of update</param>
    /// <param name="val">Value of update</param>
    /// <returns>Boolean indicating whether or not the update is valid</returns>
    /// <exception cref="ArgumentException">Thrown if the property itself is invalid</exception>
    private bool ValidUpdate(string prop, object val)
    {
        switch (prop)
        {
            case "title":
            case "description":
                return !string.IsNullOrEmpty(val.ToString());
            case "creators":
            case "tags":
                return ((List<string>)val).Count > 0;
            default:
                throw new ArgumentException($"Cannot update property: {prop}");
        }
        ;
    }

    /// <summary>
    /// Builds a predicate using a FilterProjectDto, similar to the function of the same name in TagsController
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