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
using System.Threading.Tasks;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Authorize]
public class ProjectController(ProjectManager projectManager, ResourceManager resourceManager) : ControllerBase
{
    private readonly ProjectManager projectManager = projectManager;
    private readonly ResourceManager resourceManager = resourceManager;

    #region Endpoints
    /// <summary>
    /// Adds a new project given a dto.
    /// </summary>
    /// <param name="dto">The DTO for project creation.</param>
    /// <returns>
    /// Returns a 200 OK response containing the added project.
    /// </returns>
    [HttpPut("create-project")]
    [SwaggerOperation(
            Summary = "Creates a new project.",
            Description = "Creates a new project given a dto"
        )]
    [SwaggerResponse(200, "New project created", typeof(Guid))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> CreateProject([FromBody] ProjectCreateDto dto)
    {
        Log.Information("Creating a new project.");

        // Make sure we have the required fields
        if (string.IsNullOrEmpty(dto.Title))
        {
            Log.Error("Title is required");
            return BadRequest(new ApiResponse(false, "Title is required"));
        }

        if (string.IsNullOrEmpty(dto.LanguageCode) || dto.LanguageCode.Length != 2)
        {
            Log.Error("Language code invalid");
            return BadRequest(new ApiResponse(false, "Language code invalid"));
        }

        // Mandatory check: look if project already exists with the same name
        bool projectExists = await projectManager.ProjectExistsAsync(project => project.Title == dto.Title);

        if (projectExists)
        {
            Log.Error("Project already exists.");
            return Conflict(new ApiResponse(false, "Project already exists."));
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

    /// <summary>
    /// Adds a new folder given a name and parent.
    /// </summary>
    /// <param name="folderName">Name of the new folder.</param>
    /// <param name="parentId">Id of parent component.</param>
    /// <returns>
    /// Returns a 200 OK response containing an Id of the added folder.
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
    public async Task<IActionResult> CreateFolder(string folderName, string parentId)
    {
        Log.Information("Creating a new folder");

        // Make sure we have the required fields
        if (string.IsNullOrEmpty(folderName))
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

        Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;

        // If there is somehow no user found calling this action, abort
        if (userId == null)
        {
            Log.Error("Failed to add folder.");
            return StatusCode(500, new ApiResponse(false, "Internal server error"));
        }

        // Otherwise grab the creators of the parent component, add the current user to that set and then create an empty folder with the name given and same language code
        HashSet<string> creators = parent.ProjectCreatorRelations?.Select(relation => relation.CreatorId).ToHashSet() ?? [];
        creators.Add(userId.ToString());

        // Folders don't have descriptions or extra notes (for now), nor do they have 
        ProjectCreateDto newFolder = new()
        {
            Title = folderName,
            LanguageCode = parent.LanguageCode,
            CreationDate = DateTime.UtcNow,
            DeletionDate = DateTime.UtcNow,
            ProjectType = "folder",
            Creators = creators.ToArray()
        };

        try
        {
            // create folder and add to parent with a relation
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
            Summary = "Deletes a project.",
            Description = "Only creators of the folder / project OR admins are able to delete it."
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
            if (string.IsNullOrEmpty(projectId))
            {
                Log.Error("Id is required");
                return BadRequest(new ApiResponse(false, "Id is required"));
            }

            // Get the GUID of the user
            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
            bool userIsAdmin = User.IsInRole("admin");

            // Get the project
            Project? project = await projectManager.GetProjectAsync(projectId, includeProperties: "ProjectCreatorRelations");

            if (project == null)
            {
                Log.Error("Project not found.");
                return NotFound(new ApiResponse(false, "Project not found."));
            }

            // Check if user is creator of project / folder
            bool userValidation = project.ProjectCreatorRelations?.Any(relation => relation.CreatorId == userId.ToString()) ?? false;

            // Check if the user has permission to delete this project
            if (!userIsAdmin && !userValidation)
            {
                Log.Warning("User {UserId} attempted to delete {id} without permissions.", userId, projectId);
                return StatusCode(403, new ApiResponse(false, "User cannot delete this project or folder."));
            }
            // Delete project / folder and all its subfolders and resources links
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

    /// <summary>
    /// Updates a property of the project itself.
    /// </summary>
    /// <param name="projectId">The id of the project or folder to delete.</param>
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
    public async Task<IActionResult> UpdateProperty(string projectId, [FromBody] Dictionary<string, string> updates)
    {
        try
        {
            Log.Information("Updating project name.");

            // Make sure we have the required fields
            if (string.IsNullOrEmpty(projectId))
            {
                Log.Error("Id is required");
                return BadRequest(new ApiResponse(false, "Id is required"));
            }

            if (updates == null || updates.Count == 0)
                return BadRequest(new ApiResponse(false, "No updates were provided."));

            Project? project = await projectManager.GetProjectAsync(projectId, includeProperties: ["ProjectCreatorRelations"]);
            if (project == null)
            {
                Log.Error("Project not found.");
                return NotFound(new ApiResponse(false, "Project not found."));
            }

            await projectManager.BeginTransaction();

            foreach (KeyValuePair<string, string> update in updates)
            {
                string property = update.Key;
                string newValue = update.Value;

                if (string.IsNullOrEmpty(newValue))
                {
                    await projectManager.Rollback();
                    Log.Error("Title is required");
                    return BadRequest(new ApiResponse(false, "Title is required"));
                }

                // Check if user is authorized to edit property
                Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
                bool userIsAdmin = User.IsInRole("admin");
                if (!userIsAdmin && !project.ProjectCreatorRelations.Any(relation => relation.CreatorId == userId.ToString()))
                {
                    await projectManager.Rollback();
                    Log.Warning("User {UserId} attempted to update property {property} of {id} without permissions.", userId, property, projectId);
                    return StatusCode(403, new ApiResponse(false, "User cannot update this property."));
                }

                switch (property)
                {
                    case "title":
                        await projectManager.UpdateProjectAsync(projectId, project => project.Title, newValue);
                        break;
                    case "description":
                        await projectManager.UpdateProjectAsync(projectId, project => project.Description, newValue);
                        break;
                    case "note":
                        await projectManager.UpdateProjectAsync(projectId, project => project.Note, newValue);
                        break;
                    case "languagecode": // TODO: stop if wrong statuscode
                        await UpdateLanguageCode(project, newValue);
                        break;
                    default:
                        Log.Error("Property not found.");
                        return NotFound(new ApiResponse(false, "Property not found."));
                }
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


    // update title => project and folders
    // update description, note, tags, languagecode => only change root project, no cascading update, fails if applied to folder
    // update creators => cascading update


    [HttpPut("add-resource/{projectId}/{resourceId}")]
    [SwaggerOperation(
        Summary = "Adds a resource to a parent project or folder.",
        Description = "Takes a resource id and project id and links both"
    )]
    [SwaggerResponse(200, "Resource added")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Project / Resource not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> AddResource(string projectId, string resourceId)
    {
        Log.Information("Adding resource to project");
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(resourceId))
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

            // Also abort if the link already exists
            //TODO: this could be moved to projectmanager.info probably, but for now it's fine
            if ((await projectManager.GetAllResources(predicate: relation => relation.ProjectId == Guid.Parse(projectId) && relation.ResourceId == Guid.Parse(resourceId))).Length != 0)
            {
                Log.Error("Resource-project link already exists.");
                return BadRequest(new ApiResponse(false, "Resource already in project / folder."));
            }

            await projectManager.AddResourceToProjectAsync(projectId, resourceId); //TODO: add added-by property to function with deletes
            return Ok(new ApiResponse(true, "Successfully added resource to project"));
        }

        catch (Exception e)
        {
            // Something else went wrong
            Log.Error(e, "Failed to add resource.");
            return StatusCode(500, new ApiResponse(false, "Internal server error"));
        }
    }

    [HttpDelete("delete-resource/{projectId}/{resourceId}")]
    [Authorize]
    [SwaggerOperation(
            Summary = "Deletes a resource from a project / folder.",
            Description = "Only people who added the resource to the folder / project OR admins are able to delete it."
        )]
    [SwaggerResponse(200, "Resource deleted")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(403, "Forbidden - User cannot delete this resource")]
    [SwaggerResponse(404, "Project / resource not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> DeleteResource(string projectId, string resourceId)
    {
        try
        {
            Log.Information("Deleting resource from project.");

            // Make sure we have the required fields
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(resourceId))
            {
                Log.Error("Id is required");
                return BadRequest(new ApiResponse(false, "Id is required"));
            }

            // Get the GUID of the user
            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
            bool userIsAdmin = User.IsInRole("admin");

            // Check if project and resource exist
            Project? project = await projectManager.GetProjectAsync(projectId, includeProperties: "ProjectResourcesRelations");

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

            // Check if user is creator of relation
            // TODO: Add parent creators permissions
            bool userValidation = project.ProjectResourcesRelations?.Any(relation => relation.AddedBy == userId) ?? false;

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

    // change tags / language code
    // get projects, with paging, filtering, search, etc.
    // get contents of project, with paging, filtering, search, etc.
    // implement delete permissions
    // prohibit adding same resource twice in same project if they are in subfolders
    #region helper functions
    private async Task<IActionResult> UpdateLanguageCode(Project project, string newCode)
    {
        // first, we check if the projectid is the one from a root project, since it should only be possible
        // to edit those from the root project
        if (project.ParentFolders.Count != 0)
        {
            Log.Error("Language code can only be changed from root folder.");
            return BadRequest(new ApiResponse(false, "Language code can only be changed from root folder."));
        }
        Queue<Guid> foldersToUpdate = new();
        foldersToUpdate.Enqueue(project.Id);

        while (foldersToUpdate.Count != 0)
        {
            // Get folder to change language code of
            Project folderToUpdate = await projectManager.GetProjectAsync(folder => folder.Id == foldersToUpdate.Dequeue());

            // Change languagecode
            await projectManager.UpdateProjectAsync(folderToUpdate.Id, project => project.LanguageCode, newCode);

            // Enqueue subfolders
            await projectManager.GetAllFolders(relation => relation.ParentId == folderToUpdate.Id);
        }
        return Ok(new ApiResponse(true, "Language code changed successfully"));
    }




    #endregion

}