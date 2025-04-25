using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq.Expressions;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Authorize] 
public class TagsController(ResourceManager resourceManager) : ControllerBase
{
    // Database context
    private readonly ResourceManager resourceManager = resourceManager;

    // ----------- Endpoints:
    
    /// <summary>
    /// Retrieves tags with advanced filtering and sorting capabilities
    /// </summary>
    /// <returns>
    /// Returns a 200 OK response containing a list of tags.
    /// </returns>
    [HttpGet("tags")]
    [SwaggerOperation(
        Summary = "Get tags with advanced filtering and sorting",
        Description = "Retrieve tags with options for pagination, filtering by multiple properties, and sorting"
    )]
    [SwaggerResponse(200, "List of tags", typeof(Tag[]))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetTags([FromQuery] TagFilterOptions filterOptions)
    {
        try
        {
            // Validate paging parameters if using paging
            if (filterOptions.UsePaging)
            {
                if (filterOptions.PageIndex < 1)
                    return BadRequest(new { message = "Page index cannot be lower than 1." });

                if (filterOptions.PageSize < 1)
                    return BadRequest(new { message = "Page size cannot be lower than 1." });
            }

            // Build the predicate based on filter parameters
            Expression<Func<Tag, bool>>? predicate = BuildPredicate(filterOptions);

            // Get filtered tags based on whether we're using paging
            Tag[]? tags;
            if (filterOptions.UsePaging)
            {
                tags = await resourceManager.GetTagPageAsync(
                    pageIndex: filterOptions.PageIndex,
                    pageSize: filterOptions.PageSize,
                    predicate: predicate
                );
            }
            else
            {
                tags = await resourceManager.GetAllTagsAsync(predicate: predicate);
            }

            // Handle null result
            if (tags == null)
            {
                return Ok(Array.Empty<Tag>());
            }

            // Check if tags can be edited/deleted if requested
            if (filterOptions.IncludeCanEditAndDelete)
            {
                foreach (Tag tag in tags)
                {
                    int relationCount = await resourceManager.ResourceTagRelationCountAsync(r => r.TagId == tag.Id);
                    tag.CanEditAndDelete = relationCount == 0;
                }
            }

            // Apply sorting if specified
            if (!string.IsNullOrEmpty(filterOptions.SortBy))
            {
                tags = await ApplySortingAsync(tags, filterOptions.SortBy, filterOptions.SortDescending);
            }

            return Ok(tags);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve tags");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

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
            return Ok(await resourceManager.GetAllTagsAsync(predicate: t => t.IsStandardized));
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
            return Ok(await resourceManager.GetAllTagsAsync(predicate: t => !t.IsStandardized));
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
    /// <param name="dto">The DTO for tag creation.</param>
    /// <returns>
    /// Returns a 200 OK response containing the added tag.
    /// </returns>
    [HttpPut("add-standard-tag")]
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
            // Get the GUID of the user
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null) dto.CreatedBy = userId;

            // Create the tag
            Guid tagId = await resourceManager.CreateTagAsync(dto, true);

            // Adding the tag was successful
            Log.Information("New tag added to tag list.");
            return Ok(new { message = "Tag added successfully.", tagId });
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
    /// <returns>
    /// Returns a 200 OK response containing the added tag.
    /// </returns>
    [HttpPut("add-user-tag")]
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
            // Get the GUID of the user
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null) dto.CreatedBy = userId;

            // Create the tag
            Guid tagId = await resourceManager.CreateTagAsync(dto);

            // Adding the tag was successful
            Log.Information("New tag added to tag list.");
            return Ok(new { message = "Tag added successfully.", tagId });
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
    /// </returns>
    [HttpDelete("delete-tag/{id}")]
    [Authorize]
    [SwaggerOperation(
            Summary = "Deletes a tag.",
            Description = "Lets admins delete any tag, and users delete their own tags if not assigned to resources."
        )]
    [SwaggerResponse(200, "Tag deleted")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(403, "Forbidden - User cannot delete this tag")]
    [SwaggerResponse(404, "Tag not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> DeleteTag(string id)
    {
        try
        {
            Log.Information("Removing tag from tag list.");

            // Make sure we have the required fields
            if (id == null)
            {
                Log.Error("Id is required");
                return BadRequest(new { message = "Id is required" });
            }
            
            // Get the GUID of the user
            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
            bool userIsAdmin = User.IsInRole("admin");
            
            // Get the tag
            Tag? tag = await resourceManager.GetTagAsync(id);
            
            if (tag == null)
            {
                Log.Error("Tag not found.");
                return NotFound(new { message = "Tag not found."});
            }
            
            // Get the resource-tag relations count
            int relationCount = await resourceManager.ResourceTagRelationCountAsync(predicate: r => r.TagId == Guid.Parse(id));
                        
            // Check if the user has permission to delete this tag
            if (!userIsAdmin && (tag.CreatedBy != userId || relationCount != 0)) 
            {
                Log.Warning("User {UserId} attempted to delete {TagId} without permissions.", userId, id);
                return StatusCode(403, new { message = "User cannot delete this tag." });
            }
            
            if (await resourceManager.DeleteTagAsync(id))
                return Ok(new { message = "Tag deleted." });
                
            Log.Error("Failed to delete tag.");
            return StatusCode(500, "Internal server error.");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error deleting tag {TagId}", id);
            return StatusCode(500, "Internal server error.");
        }
    }

    /// <summary>
    /// Changes the name of a tag.
    /// </summary>
    /// <param name="id">The id of the tag.</param>
    /// <param name="newName">The new name of the tag.</param>
    /// <returns>
    /// Returns a 200 OK response.
    /// </returns>
    [HttpPatch("rename-tag/{id}/{newName}")]
    [Authorize]
    [SwaggerOperation(
            Summary = "Change tag name.",
            Description = "Lets admins change the name of any tag, and users their own tags if not assigned to resources."
        )]
    [SwaggerResponse(200, "Tag name changed")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(403, "Forbidden - User cannot edit this tag")]
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
            // Get the GUID of the user
            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
            bool userIsAdmin = User.IsInRole("admin");
                        
            // Get the tag
            Tag? tag = await resourceManager.GetTagAsync(id);
            
            if (tag == null)
            {
                Log.Error("Tag not found.");
                return NotFound(new { message = "Tag not found."});
            }
            
            // Get the resource-tag relations count
            int relationCount = await resourceManager.ResourceTagRelationCountAsync(predicate: r => r.TagId == Guid.Parse(id));
            
            // Check if the user has permission to delete this tag
            if (!userIsAdmin && (tag.CreatedBy != userId || relationCount != 0)) 
            {
                Log.Warning("User {UserId} attempted to edit {TagId} without permissions.", userId, id);
                return StatusCode(403, new { message = "User cannot edit this tag." });
            }
        
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

    [HttpPatch("approve-tag/{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerResponse(200, "Tag was approved")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Tag not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> ApproveTag(string id)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest(new { message = "ID is required." });

        try
        {
            // Check if tag exists
            if (!await resourceManager.TagExistsAsync(id)) return NotFound(new { message = "Tag was not found." });

            await resourceManager.UpdateTagAsync(id, t => t.IsApproved, true);
            await resourceManager.UpdateTagAsync(id, t => t.ApprovedOn, DateTime.UtcNow);

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
                await resourceManager.UpdateTagAsync(id, t => t.ApprovedBy, Guid.Parse(userId));

            return Ok(new { message = "Tag was approved" });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error approving tag {TagId}", id);
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPatch("make-standardized/{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerResponse(200, "Tag was standardized")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Tag not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> MakeStandardized(string id)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest(new { message = "ID is required." });

        try
        {
            // Check if tag exists
            if (!await resourceManager.TagExistsAsync(id)) return NotFound(new { message = "Tag was not found." });

            await resourceManager.UpdateTagAsync(id, t => t.IsStandardized, true);

            return Ok(new { message = "Tag was standardized" });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error approving tag {TagId}", id);
            return StatusCode(500, "Internal Server Error");
        }
    }
    
    [HttpPatch("merge/{id1}/{id2}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerResponse(200, "Tags merged")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "Tag(s) not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> Merge(string id1, string id2) 
    {
        // Validate input parameters
        if (string.IsNullOrEmpty(id1) || string.IsNullOrEmpty(id2)) 
            return BadRequest(new { message = "IDs are required." });
            
        if (id1 == id2)
            return BadRequest(new { message = "Cannot merge a tag with itself." });
            
        if (!Guid.TryParse(id1, out Guid tagId1) || !Guid.TryParse(id2, out Guid tagId2))
            return BadRequest(new { message = "Invalid tag ID format." });
        
        try 
        {
            // Check if both tags exist
            if (!await resourceManager.TagExistsAsync(id1) || !await resourceManager.TagExistsAsync(id2)) 
                return NotFound(new { message = "One or both tags were not found." });

            await resourceManager.BeginTransaction();
            
            // Get all resources related to the second tag
            ResourceTagRelation[]? tagRelations = await resourceManager
                .GetAllResourceTagRelationsAsync(
                    predicate: r => r.TagId == tagId2,
                    includeProperties: "Resource");
            
            // Add the first tag to resources that don't already have it
            foreach (ResourceTagRelation relation in tagRelations) 
            {
                // Check if the resource already has the first tag
                bool hasFirstTag = await resourceManager.ResourceTagRelationExistsAsync(
                    r => r.ResourceId == relation.ResourceId && r.TagId == tagId1);
                
                // If the resource doesn't have the first tag, add it
                if (!hasFirstTag)
                {
                    await resourceManager.AddTagToResourceAsync(relation.ResourceId, tagId1);
                }
            }
            
            // Delete the second tag
            if (!await resourceManager.DeleteTagAsync(id2))
            {
                await resourceManager.Rollback();
                Log.Error("Failed to delete tag {TagId2} during merge", id2);
                return StatusCode(500, new { message = "Failed to delete old tag during merge." });
            }
            
            // Commit the changes made
            await resourceManager.Commit();
            return Ok(new { message = "Tags merged successfully." });
        }
        catch (Exception e) 
        {
            // Rollback if there was an error
            await resourceManager.Rollback();
        
            Log.Error(e, "Error merging tags {TagId1} and {TagId2}", id1, id2);
            return StatusCode(500, new { message = "Internal Server Error" });
        }
    }
    
    
    #region Helper Methods
    
    /// <summary>
    /// Constructs a dynamic predicate expression for filtering <see cref="Tag"/> entities based on the specified filter options.
    /// Each non-null or enabled option in <paramref name="options"/> is translated into a logical condition that is 
    /// combined with the others using a logical OR.
    /// </summary>
    /// <param name="options">
    /// The filter criteria used to build the predicate, including creator, approval status, date ranges, and text search.
    /// </param>
    /// <returns>
    /// A predicate expression that can be used to filter <see cref="Tag"/> records in a LINQ query,
    /// or <c>null</c> if no filtering options are provided.
    /// </returns>
    private Expression<Func<Tag, bool>>? BuildPredicate(TagFilterOptions options)
    {
        // Start with a predicate that matches everything
        Expression<Func<Tag, bool>>? predicate = null;

        // Filter by creator user ID
        if (options.CreatedBy.HasValue)
        {
            predicate = AddPredicate(predicate, t => t.CreatedBy == options.CreatedBy);
        }
        
        // Filter to only show current user's tags
        if (options.OnlyOwnedByCurrentUser)
        {
            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
            if (userId.HasValue)
            {
                predicate = AddPredicate(predicate, t => t.CreatedBy == userId);
            }
        }

        // Filter by approval status
        if (options.IsApproved.HasValue)
        {
            predicate = AddPredicate(predicate, t => t.IsApproved == options.IsApproved.Value);
        }

        // Filter by standardization status
        if (options.IsStandardized.HasValue)
        {
            predicate = AddPredicate(predicate, t => t.IsStandardized == options.IsStandardized.Value);
        }

        // Filter by text search
        if (!string.IsNullOrEmpty(options.SearchQuery))
        {
            var searchTerm = options.SearchQuery.ToLower();
            predicate = AddPredicate(predicate, t => t.Name.ToLower().Contains(searchTerm));
        }

        // Filter by creation date range
        if (options.CreatedFromDate.HasValue)
        {
            predicate = AddPredicate(predicate, t => t.CreatedOn >= options.CreatedFromDate.Value);
        }

        if (options.CreatedToDate.HasValue)
        {
            predicate = AddPredicate(predicate, t => t.CreatedOn <= options.CreatedToDate.Value);
        }

        // Filter by approval date range
        if (options.ApprovedFromDate.HasValue)
        {
            predicate = AddPredicate(predicate, t => t.ApprovedOn >= options.ApprovedFromDate.Value);
        }

        if (options.ApprovedToDate.HasValue)
        {
            predicate = AddPredicate(predicate, t => t.ApprovedOn <= options.ApprovedToDate.Value);
        }

        return predicate;
    }

    /// <summary>
    /// Combines two predicate expressions for <see cref="Tag"/> into a single expression using a logical OR.
    /// If an existing predicate is provided, the new predicate is merged with it. 
    /// Otherwise, the new predicate is returned as-is.
    /// </summary>
    /// <param name="existingPredicate">
    /// The existing predicate to extend. Can be <c>null</c>, in which case the <paramref name="newPredicate"/> is returned.
    /// </param>
    /// <param name="newPredicate">
    /// The new predicate to combine with the existing one.
    /// </param>
    /// <returns>
    /// A combined predicate that evaluates to <c>true</c> if either the existing or new predicate evaluates to <c>true</c>.
    /// </returns>
    private Expression<Func<Tag, bool>> AddPredicate(Expression<Func<Tag, bool>>? existingPredicate, Expression<Func<Tag, bool>> newPredicate)
    {
        if (existingPredicate == null)
        {
            return newPredicate;
        }

        // Parameter for the combined expression
        ParameterExpression? parameter = Expression.Parameter(typeof(Tag), "t");

        // Replace parameters in both expressions
        ReplaceExpressionVisitor? leftVisitor = new ReplaceExpressionVisitor(existingPredicate.Parameters[0], parameter);
        Expression? left = leftVisitor.Visit(existingPredicate.Body) 
                    ?? throw new InvalidOperationException("Left expression visitor returned null.");

        ReplaceExpressionVisitor? rightVisitor = new ReplaceExpressionVisitor(newPredicate.Parameters[0], parameter);
        Expression? right = rightVisitor.Visit(newPredicate.Body)
                    ?? throw new InvalidOperationException("Right expression visitor returned null.");

        // Combine with OR
        BinaryExpression? combined = Expression.OrElse(left, right);

        return Expression.Lambda<Func<Tag, bool>>(combined, parameter);
    }

    /// <summary>
    /// Applies sorting to the tags array
    /// TODO: not sure if using the switch/cases is the nicest way to do this?
    /// </summary>
    private async Task<Tag[]> ApplySortingAsync(Tag[] tags, string sortBy, bool descending)
    {
        // Convert sortBy to lowercase for case-insensitive comparison
        switch (sortBy.ToLower())
        {
            case "id":
                return descending 
                    ? tags.OrderByDescending(t => t.Id).ToArray() 
                    : tags.OrderBy(t => t.Id).ToArray();
                    
            case "name":
                return descending 
                    ? tags.OrderByDescending(t => t.Name).ToArray() 
                    : tags.OrderBy(t => t.Name).ToArray();
            
            case "createdon":
                return descending 
                    ? tags.OrderByDescending(t => t.CreatedOn).ToArray() 
                    : tags.OrderBy(t => t.CreatedOn).ToArray();
            
            case "approvedon":
                return descending 
                    ? tags.OrderByDescending(t => t.ApprovedOn).ToArray() 
                    : tags.OrderBy(t => t.ApprovedOn).ToArray();
            
            case "usagecount":
            // Create a dictionary to store usage counts
            var usageCounts = new Dictionary<Guid, int>();
            
            // Get usage count for each tag
            foreach (var tag in tags)
            {
                usageCounts[tag.Id] = await resourceManager.ResourceTagRelationCountAsync(r => r.TagId == tag.Id);
            }
            
            // Sort by usage count
            return descending 
                ? tags.OrderByDescending(t => usageCounts[t.Id]).ToArray() 
                : tags.OrderBy(t => usageCounts[t.Id]).ToArray();
            
            default:
                // Default sort by Name if unknown sort field
                return tags.OrderBy(t => t.Name).ToArray();
        }
    }

    /// <summary>
    /// An ExpressionVisitor that replaces all occurrences of a specified expression with a new one.
    /// Useful for parameter substitution in expression trees.
    /// </summary>
    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplaceExpressionVisitor"/> class.
        /// </summary>
        /// <param name="oldValue">The expression to be replaced.</param>
        /// <param name="newValue">The expression to replace with.</param>
        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }
        
        /// <summary>
        /// Visits an expression and replaces it if it matches the target expression.
        /// </summary>
        /// <param name="node">The current expression node being visited.</param>
        /// <returns>The original node, the replacement node, or a recursively visited version.</returns>
        public override Expression? Visit(Expression? node)
        {
            if (node == null)
                return null;
            if (node == _oldValue)
                return _newValue;
            return base.Visit(node);
        }
    }


    /// <summary>
    /// DTO for tag filtering options
    /// </summary>
    public class TagFilterOptions
    {
        // Pagination
        public bool UsePaging { get; set; } = false;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 100;

        // Filtering
        public string? SearchQuery { get; set; }
        public Guid? CreatedBy { get; set; }
        public bool OnlyOwnedByCurrentUser { get; set; } = false;
        public bool? IsApproved { get; set; }
        public bool? IsStandardized { get; set; }
        public DateTime? CreatedFromDate { get; set; }
        public DateTime? CreatedToDate { get; set; }
        public DateTime? ApprovedFromDate { get; set; }
        public DateTime? ApprovedToDate { get; set; }

        // Additional processing
        public bool IncludeCanEditAndDelete { get; set; } = true;

        // Sorting
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }
    
    #endregion
} 


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


