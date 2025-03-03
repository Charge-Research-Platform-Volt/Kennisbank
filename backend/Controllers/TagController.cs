using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
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
            Description = "List all standarized tags created by admins."
        )]
    [SwaggerResponse(200, "List of tags", typeof(List<Tag>))]
    [SwaggerResponse(500, "Internal server error")]
    public IActionResult Get()
    {
        try
        {
            return Ok(_context.Tags.ToList());
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve documents");
            return StatusCode(500, "Internal server error");
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
    [SwaggerOperation(
            Summary = "Adds new standard tag.",
            Description = "Lets an admin add a new tag to the list of standarized tags."
        )]
    [SwaggerResponse(200, "New tag added", typeof(Tag))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(409, "Tag already exists")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> AddTag(string tagName)
    {
        Log.Information($"Adding new tag to tag list: {tagName}.");

        // Make sure we have the required fields from body
        if (string.IsNullOrEmpty(tagName))
        {
            Log.Error("Name is required");
            return BadRequest("Name is required");
        }

        Tag tag = new()
        {
            Name = tagName,
        };

        // Add the tag
        try
        {
            await _context.Tags.AddAsync(tag);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException postgresEx && postgresEx.SqlState == "23505")
        {
            // The tag already exists
            Log.Error(e, "Tag already exists.");
            return Conflict("Tag already exists.");
        }
        catch (Exception e)
        {
            // Something else went wrong
            Log.Error(e, "Failed to add tag.");
            return StatusCode(500, "Internal server error");
        }

        // Adding the tag was successful
        Log.Information($"New tag added {tagName} to tag list.");
        return Ok(tag);
    }

    /// <summary>
    /// Deletes a tag from the tag list.
    /// </summary>
    /// <param name="tagName">The name of the tag to delete.</param>
    /// <returns>
    /// Returns a 200 OK response containing the deleted tag.
    // </returns>
    [HttpDelete("delete-tag/{tagName}")]
    [SwaggerOperation(
            Summary = "Delete standard tag.",
            Description = "Lets and admin delete a tag from the list of standarized tags."
        )]
    [SwaggerResponse(200, "Tag deleted", typeof(Tag))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Tag not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> DeleteTag(string tagName)
    {
        Log.Information($"Removing tag from tag list: {tagName}.");

        // Make sure we have the required fields from body
        if (string.IsNullOrEmpty(tagName))
        {
            Log.Error("Name is required");
            return BadRequest("Name is required");
        }

        //find tag in database
        Tag tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName)
            ?? new Tag() { Name = "" }; //if not found: set tag to empty tag

        //check if tag is empty, if so: it was not found
        if (string.IsNullOrEmpty(tag.Name))
        {
            Log.Error($"Tag {tagName} not found.");
            return NotFound($"Tag {tagName} not found.");
        }

        //remove tag from database
        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();

        return Ok(tag);
    }
}
