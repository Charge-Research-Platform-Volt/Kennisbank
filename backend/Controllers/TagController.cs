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
public class TagController : ControllerBase
{
    // Database context
    private readonly DatabaseContext _context;
    public TagController(DatabaseContext context)
    {
        _context = context;
    }

    // ----------- Endpoints:

    /// <summary>
    /// Retrieves all tags from the drive.
    /// </summary>
    /// <returns>
    /// Returns a 200 OK response containing a list of all Drive entities.
    /// </returns>
    [HttpGet("all-tags")]
    [SwaggerOperation(
            Summary = "List all tags.",
            Description = "List all standardized tags created by admins."
        )]
    [SwaggerResponse(200, "List of tags", typeof(List<AdminTag>))]
    [SwaggerResponse(500, "Internal server error")]
    public IActionResult Get()
    {
        try
        {
            return Ok(_context.AdminTags.ToList().OrderBy(t => t.Name));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve documents");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Adds a new tag to the tag list.
    /// </summary>
    /// <param name="tagName">The name of the tag to add.</param>
    /// <returns>
    /// Returns a 200 OK response containing the added tag.
    // </returns>
    [HttpPost("add-tag/{tagName}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(
            Summary = "Adds new standard tag.",
            Description = "Lets an admin add a new tag to the list of standardized tags."
        )]
    [SwaggerResponse(200, "New tag added", typeof(AdminTag))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(409, "Tag already exists")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> AddTag(string tagName)
    {
        Log.Information("Adding new tag to tag list.");

        // Make sure we have the required fields
        if (string.IsNullOrEmpty(tagName))
        {
            Log.Error("Name is required");
            return BadRequest(new { message = "Name is required" });
        }

        AdminTag tag = new()
        {
            Id = Guid.NewGuid(),
            Name = tagName,
        };

        // Add the tag
        try
        {
            await _context.AdminTags.AddAsync(tag);
            await _context.SaveChangesAsync();
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

        // Adding the tag was successful
        Log.Information("New tag added to tag list.");
        return Ok(new { message = "Tag added." });
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
    [SwaggerResponse(200, "Tag deleted", typeof(AdminTag))]
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

        Guid guid = Guid.Parse(id);

        //find tag in database
        AdminTag? tag = await _context.AdminTags.FirstOrDefaultAsync(t => t.Id == guid); //if not found: set tag to null

        //check if tag is found
        if (tag == null)
        {
            Log.Error("Tag not found.");
            return NotFound(new { message = "Tag not found." });
        }

        //remove tag from database
        _context.AdminTags.Remove(tag);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Tag deleted." });
    }

    /// </summary>
    /// <param name="tagName">The name of the tag to delete.</param>
    /// <returns>
    /// Returns a 200 OK response containing the deleted tag.
    // </returns>
    [HttpPatch("change-tag-name/{id}/{newName}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(
            Summary = "Change tag name.",
            Description = "Lets and admin change the name of a standardized tag."
        )]
    [SwaggerResponse(200, "Tag name changed", typeof(AdminTag))]
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

        //parse id
        Guid guid;
        try
        {
            guid = Guid.Parse(id);
        }
        catch (FormatException)
        {
            Log.Error("Invalid id format.");
            return BadRequest(new { message = "Invalid id format." });
        }

        //find tag in database
        AdminTag? tag = await _context.AdminTags.FirstOrDefaultAsync(t => t.Id == guid); //if not found: set tag to null

        //check if tag is found
        if (tag == null)
        {
            Log.Error("Tag not found.");
            return NotFound(new { message = "Tag not found." });
        }

        //Change tag name
        try
        {
            tag.Name = newName;
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException postgresEx && postgresEx.SqlState == "23505")
        {
            // The tag already exists
            Log.Error(e, "Tag already exists.");
            return Conflict(new { message = "New tag name already exists." });
        }

        return Ok(new { message = "Tag name changed." });
    }
}
