using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
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
    [HttpGet]
    [Route("all-tags")]
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
    /// <param name="tagDto">The Data Transfer Object for adding a tag.</param>
    /// <returns>
    /// Returns a 200 OK response containing the added tag.
    // </returns>
    [HttpPost]
    [Route("add-tag")]
    public async Task<IActionResult> AddTag([FromBody] TagCreateDto tagDto)
    {
        Log.Information($"Adding new tag to tag list: {tagDto.Name}.");

        // Make sure we have the required fields from body
        if (string.IsNullOrEmpty(tagDto.Name))
        {
            Log.Error("Name is required");
            return BadRequest("Name is required");
        }

        Tag tag = new()
        {
            Name = tagDto.Name,
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
        Log.Information($"New tag added {tagDto.Name} to tag list.");
        return Ok(tag);
    }

    /// <summary>
    /// Removes a tag from the tag list.
    /// </summary>
    /// <param name="tagDto">The Data Transfer Object for deleting a tag.</param>
    /// <returns>
    /// Returns a 200 OK response containing the removed tag.
    // </returns>
    [HttpDelete]
    [Route("delete-tag")]
    public async Task<IActionResult> DeleteTag([FromBody] TagDeleteDto tagDto)
    {
        Log.Information($"Removing tag from tag list: {tagDto.Name}.");

        // Make sure we have the required fields from body
        if (string.IsNullOrEmpty(tagDto.Name))
        {
            Log.Error("Name is required");
            return BadRequest("Name is required");
        }

        //find tag in database
        Tag tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagDto.Name) 
            ?? new Tag() {Name = ""}; //if not found: set tag to empty tag

        //check if tag is empty, if so: it was not found
        if(string.IsNullOrEmpty(tag.Name))
        {
            Log.Error($"Tag {tagDto.Name} not found.");
            return NotFound($"Tag {tagDto.Name} not found.");
        }
        
        //remove tag from database
        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();

        return Ok(tag);
    }
}
