using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Authorize]
public class UserTagController : ControllerBase 
{
    // Database context
    private readonly DatabaseContext _context;
    public UserTagController(DatabaseContext context)
    {
        _context = context;
    }

    // ----------- Endpoints:

    /// <summary>
    /// Retrieves all user tags from the storage.
    /// </summary>
    /// <returns>
    /// Returns a 200 OK response containing a list of all Storage entities.
    /// </returns>
    [HttpGet("all-tags")]
    [SwaggerOperation(
            Summary = "List all user tags.",
            Description = "List all tags created by users."
        )]
    [SwaggerResponse(200, "List of tags", typeof(List<Tag>))]
    [SwaggerResponse(500, "Internal server error")]
    public IActionResult Get()
    {
        try
        {
            return Ok(_context.Tags.ToList().OrderBy(t => t.Name));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve documents");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Adds a new tag to the user tag list.
    /// </summary>
    /// <param name="tagName">The name of the user tag to add.</param>
    /// <returns>
    /// Returns a 200 OK response containing the added user tag.
    // </returns>
    [HttpPost("add-usertag/{tagName}")]
    [SwaggerOperation(
            Summary = "Adds new user tag.",
            Description = "Lets a user add a new tag to the list of user tags."
        )]
    [SwaggerResponse(200, "New tag added", typeof(Tag))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(409, "Tag already exists")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> AddTag(string tagName)
    {
        Log.Information("Adding new user tag to user tag list.");

        // Make sure we have the required fields
        if (string.IsNullOrEmpty(tagName))
        {
            Log.Error("Name is required");
            return BadRequest(new { message = "Name is required" });
        }

        // Check if the tag already exists in the Tags table
        bool tagExists = await _context.Tags.AnyAsync(t => t.Name.ToLower() == tagName.ToLower());

        if (tagExists)
        {
            Log.Error("Tag already exists in Tags table.");
            return Conflict(new { message = "Tag already exists in the standardized tags list." });
        }

        // Check if the tag already exists in the UserTags table
        bool userTagExists = await _context.Tags.AnyAsync(ut => ut.Name.ToLower() == tagName);

        if (userTagExists)
        {
            Log.Error("Tag already exists in UserTags table.");
            return Conflict(new { message = "Tag name already exists in the user tags list." });
        }

        Tag userTag = new()
        {
            Id = Guid.NewGuid(),
            Name = tagName,
            IsStandardized = false,
            CreatedBy = "TestUser1", // TODO: User should be the actual User
            IsApproved = false, // By default the tag is not approved
            CreatedOn = DateTime.UtcNow,
        };

        // Add the user tag
        try
        {
            await _context.Tags.AddAsync(userTag);
            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // Something else went wrong
            Log.Error(e, "Failed to add tag.");
            return StatusCode(500, new { message = "Internal server error" });
        }

        // Adding the tag was successful
        Log.Information("New tag added to tag list.");
        return Ok(new { message = "User tag added." });
    }

    


    /// <summary>
    /// Deletes a user tag from the user tag list.
    /// </summary>
    /// <param name="id">The id of the user tag to delete.</param>
    /// <returns>
    /// Returns a 200 OK response.
    // </returns>
    [HttpDelete("delete-usertag/{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(
            Summary = "Delete user tag.",
            Description = "Lets an admin delete a tag from the list of user tags."
        )]
    [SwaggerResponse(200, "Tag deleted", typeof(Tag))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Tag not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> DeleteTag(string id)
    {
        try
        {
            Log.Information("Removing user tag from user tag list.");

            // Make sure we have the required fields
            if (id == null)
            {
                Log.Error("Id is required");
                return BadRequest(new { message = "Id is required" });
            }

            Guid guid = Guid.Parse(id);

        //find tag in database
        Tag? userTag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == guid); //if not found: set tag to null

        //check if tag is found
        if (userTag == null)
        {
            Log.Error("Tag not found.");
            return NotFound(new { message = "Tag not found." });
        }

        // Make sure the user has created this tag or it's the admin deleting it
        if (userTag.CreatedBy != "TestUser1") // TODO: Shouldn't compare strings but actual users
        {
            Log.Error("Another user has created this tag.");
            return StatusCode(403, new { message = "Another user has created this tag." });
        }
        catch (Exception e)
        {
            Log.Error("Tag has been approved and can no longer be deleted.");
            return Conflict(new { message = "Tag has been approved and can no longer be deleted." });
        }

        // remove tag from database
        _context.Tags.Remove(userTag);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Tag deleted." });
    }

    // TODO: Not sure users should be able to change tag names?
    // Maybe only tags they specifically made and have not
    // yet been approved by an admin?

    /// </summary>
    /// <param name="id">The id of the user tag.</param>
    /// <param name="newName">The new name of the user tag.</param>
    /// <returns>
    /// Returns a 200 OK response.
    // </returns>
    [HttpPatch("change-usertag-name/{id}/{newName}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(
            Summary = "Change user tag name.",
            Description = "Lets an admin change the name of a user tag."
        )]
    [SwaggerResponse(200, "Tag name changed", typeof(Tag))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Tag not found")]
    [SwaggerResponse(409, "New tag name already exists")]
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

        // parse id
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
        Tag? userTag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == guid); //if not found: set tag to null

        // check if tag is found
        if (userTag == null)
        {
            Log.Error("Tag not found.");
            return NotFound(new { message = "Tag not found." });
        }

        // Make sure the user has created this tag 
        if (userTag.CreatedBy != "TestUser1") // TODO: Shouldn't compare strings but actual users
        {
            Log.Error("Another user has created this tag.");
            return StatusCode(403, new { message = "Another user has created this tag." });
        }

        // Make sure the tag has not been approved yet
        if (userTag.IsApproved)
        {
            Log.Error("Tag already exists in Tags table.");
            return Conflict(new { message = "Tag already exists in the standardized tags list." });
        }

        // Change tag name
        try
        {
            userTag.Name = newName;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tag name changed." });
        }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException postgresEx && postgresEx.SqlState == "23505")
        {
            // The tag already exists
            Log.Error(e, "Tag already exists.");
            return Conflict(new { message = "New tag name already exists." });
        }

    }

    // TODO: Once we implement authorization, this should be moved to AdminController.cs

    /*
    /// <summary>
    /// Approve a user tag.
    /// </summary>
    /// <param name="id">The id of the user tag</param>
    /// <returns>
    /// Returns a 200 OK response containing the name of the tag.
    // </returns>
    [HttpPatch("approve-usertag/{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(
        Summary = "Approve a user tag.",
        Description = "Marks a user-created tag as approved."
        )]
    [SwaggerResponse(200, "Tag approved", typeof(Tag))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Tag not found")]
    [SwaggerResponse(409, "Tag already approved")]
    public async Task<IActionResult> ApproveUserTag(string id)
    {
        Log.Information($"Approving user tag with ID: {id}");

        if (string.IsNullOrEmpty(id))
        {
            Log.Error("Id is required");
            return BadRequest(new { message = "Id is required" });
        }

        if (!Guid.TryParse(id, out Guid guid))
        {
            Log.Error("Invalid id format.");
            return BadRequest(new { message = "Invalid id format." });
        }

        var userTag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == guid);

        if (userTag == null)
        {
            Log.Error("Tag not found.");
            return NotFound(new { message = "Tag not found." });
        }

        if (userTag.IsApproved)
        {
            Log.Error("Tag is already approved.");
            return Conflict(new { message = "Tag is already approved." });
        }

        // Check if tag name already exists in the Tags table
        bool tagExists = await _context.AdminTags.AnyAsync(t => t.Name == userTag.Name);
        if (tagExists)
        {
            Log.Error("A tag with this name already exists in Tags.");
            return Conflict(new { message = "A tag with this name already exists." });
        }

        // Approve the user tag
        userTag.IsApproved = true;
        _context.Tags.Update(userTag);

        // Create new Tag
        Tag newTag = new()
        {
            Id = userTag.Id,
            Name = userTag.Name,
        };

        // Add the tag and remove the user tag
        try
        {
            await _context.AdminTags.AddAsync(newTag);
            await _context.SaveChangesAsync();

            Log.Information($"User tag '{userTag.Name}' approved and added to Tags.");
            return Ok(new { message = "Tag approved and added to Tags.", tag = newTag });
        }
        catch (Exception e)
        {
            // Something else went wrong
            Log.Error(e, "Failed to convert the user tag to tag.");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    */
}
