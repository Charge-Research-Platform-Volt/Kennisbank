using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Authorize]
public class TagController(ResourceManager resourceManager) : ControllerBase
{
    // Database context
    private readonly ResourceManager resourceManager = resourceManager;

    // ----------- Endpoints:

    /// <summary>
    /// Retrieves all tags from the drive.
    /// </summary>
    /// <returns>
    /// Returns a 200 OK response containing a list of all tags.
    /// </returns>
    [HttpGet("all-tags")]
    [SwaggerOperation(
            Summary = "List all tags.",
            Description = "List all tags."
        )]
    [SwaggerResponse(200, "List of tags", typeof(Tag[]))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            return Ok(await resourceManager.GetAllTagsAsync());
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve tags");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("tag-page")]
    [SwaggerOperation(Summary = "List all tags paged.", Description = "List all tags paged.")]
    [SwaggerResponse(200, "List of tags", typeof(Tag[]))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetAllPaged(int pageIndex = 1, int pageSize = 100)
    {
        try
        {
            return Ok(await resourceManager.GetTagPageAsync(pageIndex, pageSize));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve tag page");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Retrieves all standardized tags from the drive.
    /// </summary>
    /// <returns>
    /// Returns a 200 OK response containing a list of all tags.
    /// </returns>
    [HttpGet("all-standard-tags")]
    [SwaggerOperation(
            Summary = "List all standardized tags.",
            Description = "List all standardizedtags."
        )]
    [SwaggerResponse(200, "List of tags", typeof(Tag[]))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetAllStandardized()
    {
        try
        {
            return Ok(await resourceManager.GetAllTagsAsync(t => t.IsStandardized));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve tags");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("standard-tag-page")]
    [SwaggerOperation(Summary = "List all standardized tags paged.", Description = "List all standardized tags paged.")]
    [SwaggerResponse(200, "List of tags", typeof(Tag[]))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetAllStandardizedPaged(int pageIndex = 1, int pageSize = 100)
    {
        try
        {
            return Ok(await resourceManager.GetTagPageAsync(
                pageIndex: pageIndex,
                pageSize: pageSize,
                predicate: t => t.IsStandardized
            ));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve standardized tag page");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Retrieves all standardized tags from the drive.
    /// </summary>
    /// <returns>
    /// Returns a 200 OK response containing a list of all tags.
    /// </returns>
    [HttpGet("all-user-tags")]
    [SwaggerOperation(
            Summary = "List all user tags.",
            Description = "List all user tags."
        )]
    [SwaggerResponse(200, "List of tags", typeof(Tag[]))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetAllUser()
    {
        try
        {
            return Ok(await resourceManager.GetAllTagsAsync(t => !t.IsStandardized));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve user tags");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("user-tag-page")]
    [SwaggerOperation(Summary = "List all user tags paged.", Description = "List all user tags paged.")]
    [SwaggerResponse(200, "List of tags", typeof(Tag[]))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetAllUserPaged(int pageIndex = 1, int pageSize = 100)
    {
        try
        {
            return Ok(await resourceManager.GetTagPageAsync(
                pageIndex: pageIndex,
                pageSize: pageSize,
                predicate: t => !t.IsStandardized
            ));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve standardized tag page");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Adds a new standard tag to the tag list.
    /// </summary>
    /// <param name="tagName">The name of the tag to add.</param>
    /// <returns>
    /// Returns a 200 OK response containing the added tag.
    /// </returns>
    [HttpPost("add-standard-tag/{tagName}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(
            Summary = "Adds new standard tag.",
            Description = "Lets an admin add a new tag to the list of standardized tags."
        )]
    [SwaggerResponse(200, "New tag added", typeof(Guid))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(409, "Tag already exists")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> AddStandardTag([FromBody] TagCreateDto dto)
    {
        Log.Information("Adding new tag to tag list.");

        // Make sure we have the required fields
        if (string.IsNullOrEmpty(dto.Name))
        {
            Log.Error("Name is required");
            return BadRequest(new { message = "Name is required" });
        }

        // Check if the tag already exists in the UserTags table
        bool userTagExists = await resourceManager.TagExistsAsync(t => t.Name == dto.Name);

        if (userTagExists)
        {
            Log.Error("Tag already exists in UserTags table.");
            return Conflict(new { message = "Tag already exists in the user tags list, try converting it instead." });
        }

        // Add the tag
        try
        {
            // Create the tag
            Guid tagId = await resourceManager.CreateTagAsync(dto, true);

            // Adding the tag was successful
            Log.Information("New tag added to tag list.");
            return Ok(tagId);
        }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException postgresEx && postgresEx.SqlState == "23505")
        {
            // The tag already exists
            Log.Error(e, "Tag already exists.");
            return Conflict(new { message = "Tag already exists." });
        }
        catch (Exception e)
        {
            // Something else went wrong
            Log.Error(e, "Failed to add tag.");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Adds a new tag to the tag list.
    /// </summary>
    /// <param name="tagName">The name of the tag to add.</param>
    /// <returns>
    /// Returns a 200 OK response containing the added tag.
    /// </returns>
    [HttpPost("add-tag/{tagName}")]
    [SwaggerOperation(
            Summary = "Adds new tag.",
            Description = "Adds a tag."
        )]
    [SwaggerResponse(200, "New tag added", typeof(Guid))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(409, "Tag already exists")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> AddTag([FromBody] TagCreateDto dto)
    {
        Log.Information("Adding new tag to tag list.");

        // Make sure we have the required fields
        if (string.IsNullOrEmpty(dto.Name))
        {
            Log.Error("Name is required");
            return BadRequest(new { message = "Name is required" });
        }

        // Check if the tag already exists in the UserTags table
        bool userTagExists = await resourceManager.TagExistsAsync(t => t.Name == dto.Name);

        if (userTagExists)
        {
            Log.Error("Tag already exists in UserTags table.");
            return Conflict(new { message = "Tag already exists in the user tags list, try converting it instead." });
        }

        // Add the tag
        try
        {
            // Create the tag
            Guid tagId = await resourceManager.CreateTagAsync(dto);

            // Adding the tag was successful
            Log.Information("New tag added to tag list.");
            return Ok(tagId);
        }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException postgresEx && postgresEx.SqlState == "23505")
        {
            // The tag already exists
            Log.Error(e, "Tag already exists.");
            return Conflict(new { message = "Tag already exists." });
        }
        catch (Exception e)
        {
            // Something else went wrong
            Log.Error(e, "Failed to add tag.");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a tag from the tag list.
    /// </summary>
    /// <param name="id">The id of the tag to delete.</param>
    /// <returns>
    /// Returns a 200 OK response containing the deleted tag.
    // </returns>
    [HttpDelete("delete-tag/{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(
            Summary = "Delete standard tag.",
            Description = "Lets and admin delete a tag from the list of standardized tags."
        )]
    [SwaggerResponse(200, "Tag deleted")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Tag not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> DeleteTag(string id)
    {
        Log.Information("Removing tag from tag list.");

        // Make sure we have the required fields
        if (id == null)
        {
            Log.Error("Id is required");
            return BadRequest(new { message = "Id is required" });
        }

        if (await resourceManager.DeleteTagAsync(id))
            return Ok(new { message = "Tag deleted." });

        Log.Error("Tag not found.");
        return NotFound(new { message = "Tag not found." });
    }

    /// <summary>
    /// Changes the name of a tag.
    /// </summary>
    /// <param name="id">The id of the tag.</param>
    /// <param name="newName">The new name of the tag.</param>
    /// <returns>
    /// Returns a 200 OK response.
    // </returns>
    [HttpPatch("change-tag-name/{id}/{newName}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(
            Summary = "Change tag name.",
            Description = "Lets an admin change the name of a standardized tag."
        )]
    [SwaggerResponse(200, "Tag name changed", typeof(Tag))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Tag not found")]
    [SwaggerResponse(409, "Tag already exists")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> ChangeTagName(string id, string newName)
    {
        Log.Information("Changing tag name.");

        // Make sure we have the required fields
        if (string.IsNullOrEmpty(id))
        {
            Log.Error("Id is required");
            return BadRequest(new { message = "Id is required" });
        }

        if (string.IsNullOrEmpty(newName))
        {
            Log.Error("New name is required");
            return BadRequest(new { message = "New name is required" });
        }

        //Change tag name
        try
        {
            if (await resourceManager.TagExistsAsync(t => t.Name == newName))
            {
                Log.Error("Tag already exists in UserTags table.");
                return Conflict(new { message = "Tag already exists in the user tags list, try converting the user tag instead." });
            }

            if (!await resourceManager.UpdateTagAsync(id, t => t.Name, newName))
            {
                Log.Error("Tag not found.");
                return NotFound(new { message = "Tag not found." });
            }

            return Ok(new { message = "Tag name changed." });
        }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException postgresEx && postgresEx.SqlState == "23505")
        {
            // The tag already exists
            Log.Error(e, "Tag already exists.");
            return Conflict(new { message = "New tag name already exists." });
        }
    }

    // TODO: Approving tags
}
