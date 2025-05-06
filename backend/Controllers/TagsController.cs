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
public class TagsController(ResourceManager resourceManager) : ControllerBase
{
    // Database context
    private readonly ResourceManager resourceManager = resourceManager;

    // ----------- Endpoints:
    
    /// <summary>
    /// Retrieves tags with advanced filtering, searching, paging and sorting capabilities
    /// </summary>
    /// <returns>
    /// Returns a 200 OK response containing a list of tags.
    /// </returns>
    [HttpPost("tags")]
    [SwaggerOperation(
        Summary = "Get tags with advanced filtering and sorting",
        Description = "Retrieve tags with options for pagination, filtering by multiple properties, and sorting"
    )]
    [SwaggerResponse(200, "List of tags", typeof(Tag[]))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetTags([FromBody] TagFilterOptions filterOptions)
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
                    predicate: predicate,
                    includeProperties: filterOptions.IncludeUsageCount ? "ResourceTagRelations" : string.Empty
                );
            }
            else
            {
                tags = await resourceManager.GetAllTagsAsync(
                    predicate: predicate,
                    includeProperties: filterOptions.IncludeUsageCount ? "ResourceTagRelations": string.Empty
                );
            }

            // Handle empty result
            if (tags == null || tags.Length == 0)
            {
                if(filterOptions.UsePaging && filterOptions.PageIndex > 1)
                    return BadRequest(new { message = "The page index is invalid." });
                else
                    return Ok(new TagPageResponse("No tags found.", new Tag[0]));
            }

            // Set the UsageCount property for each tag if IncludeUsageCount is true
            if (filterOptions.IncludeUsageCount)
            {
                foreach (Tag tag in tags)
                {
                    // Try to use the navigation property, otherwise set to 0
                    tag.UsageCount = tag.ResourceTagRelations?.Count ?? 0;
                }
            }

            // Apply weighted sorting if specified
            if (!string.IsNullOrEmpty(filterOptions.WeightedSort) && !string.IsNullOrEmpty(filterOptions.SortBy))
            {
                tags = PropertyMatcher.SortByWeightedProperties(tags, filterOptions.WeightedSort, filterOptions.SortDescending, filterOptions.SortBy).ToArray();
            }
            // Otherwise apply regular sorting if specified
            else if (!string.IsNullOrEmpty(filterOptions.SortBy))
            {
                tags = PropertyMatcher.SortByProperty(tags, filterOptions.SortBy, filterOptions.SortDescending, filterOptions.SortBy).ToArray();
            }

            if(filterOptions.UsePaging)
            {
                // calculate the total number of tags
                int totalCount = await resourceManager.TagCountAsync(predicate);
                int pageCount = (int)Math.Ceiling((double)totalCount/filterOptions.PageSize);
                return Ok(new TagPageResponse($"{tags.Length} tags found.", tags, filterOptions.PageIndex, filterOptions.PageSize, pageCount));
            }
            return Ok(new TagPageResponse($"{tags.Length} tags found.", tags));

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
    [Obsolete("Deprecated, use GetTags instead. Method can be removed once frontend is updated")]
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
            Tag[] tags = await resourceManager.GetAllTagsAsync();
            return Ok(new ApiResponse(true, $"{tags.Length} tags found", tags));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve tags");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    [Obsolete("Deprecated, use GetTags instead. Method can be removed once frontend is updated")]
    [HttpGet("tag-page")]
    [SwaggerOperation(Summary = "List all tags paged.", Description = "List all tags paged.")]
    [SwaggerResponse(200, "List of tags", typeof(Tag[]))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetAllPaged(int pageIndex = 1, int pageSize = 100, string? searchQuery = null)
    {
        try
        {
            if (pageIndex < 1)
                return BadRequest(new { message = "Page index cannot be lower than 1." });

            if(pageSize < 1)
                return BadRequest(new { message = "Page size cannot be lower than 1." });

            Tag[] tags;
            // If there is a query, return the tag page that match that query and the index
            if(!string.IsNullOrEmpty(searchQuery))
            {
                tags = await resourceManager.GetTagPageAsync(
                    pageIndex: pageIndex, 
                    pageSize: pageSize, 
                    predicate: t => t.Name.ToLower().Contains(searchQuery.ToLower())
                );
            }

            // Otherwise page normally
            else
            {
                tags = await resourceManager.GetTagPageAsync(
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );
            }

            // Get the total amount of tags and pages
            int totalTags = (await resourceManager.GetAllTagsAsync()).Length; 
            int pageCount = (int)Math.Ceiling((double)totalTags / pageSize);
            
            // If no tags are returned, put in the message that no tags are found
            if(tags == null)
                return Ok(new TagPageResponse("No tags on this page.", Array.Empty<Tag>(), pageIndex, pageSize, pageCount));
            
            return Ok(new TagPageResponse($"{tags.Length} tags found", tags, pageIndex, pageSize, pageCount));
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
    [Obsolete("Deprecated, use GetTags instead. Method can be removed once frontend is updated")]
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

    [Obsolete("Deprecated, use GetTags instead. Method can be removed once frontend is updated")]
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
    [Obsolete("Deprecated, use GetTags instead. Method can be removed once frontend is updated")]
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

    [Obsolete("Deprecated, use GetTags instead. Method can be removed once frontend is updated")]
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

        if(dto.Name.Length > 50){
            Log.Error("Tag is too long");
            return BadRequest(new { message = "Tag is too long" }); 
        }

        // Check if the tag already exists in the UserTags table
        bool tagExists = await resourceManager.TagExistsAsync(t => t.Name == dto.Name);

        if (tagExists)
        {
            Log.Error("Tag already exists.");
            return Conflict(new { message = "Tag already exists." });
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

        if(dto.Name.Length > 50){
            Log.Error("Tag is too long");
            return BadRequest(new { message = "Tag is too long" }); 
        }

        // Check if the tag already exists in the UserTags table
        bool tagExists = await resourceManager.TagExistsAsync(t => t.Name == dto.Name);

        if (tagExists)
        {
            Log.Error("Tag already exists..");
            return Conflict(new { message = "Tag already exists." });
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
            Tag? tag = await resourceManager.GetTagAsync(id, includeProperties: "ResourceTagRelations");
            
            if (tag == null)
            {
                Log.Error("Tag not found.");
                return NotFound(new { message = "Tag not found."});
            }
            
            // Get the resource-tag relations count
            int relationCount = tag.ResourceTagRelations?.Count ?? 0;
                        
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
            Tag? tag = await resourceManager.GetTagAsync(id, includeProperties: "ResourceTagRelations");
            
            if (tag == null)
            {
                Log.Error("Tag not found.");
                return NotFound(new { message = "Tag not found."});
            }
            
            // Get the resource-tag relations count
            int relationCount = tag.ResourceTagRelations?.Count ?? 0;
            
            // Check if the user has permission to delete this tag
            if (!userIsAdmin && (tag.CreatedBy != userId || relationCount != 0)) 
            {
                Log.Warning("User {UserId} attempted to edit {TagId} without permissions.", userId, id);
                return StatusCode(403, new { message = "User cannot edit this tag." });
            }
        
            if (await resourceManager.TagExistsAsync(t => t.Name == newName))
            {
                Log.Error("Tag already exists.");
                return Conflict(new { message = "Tag already exists." });
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
    
    /// <summary>
    /// Merges the second tag into the first tag. This is done by 
    /// finding all the relations of the second tag, and then transferring
    /// these relations to the first tag. 
    /// <b>Keep in mind that the second tag is deleted after the relations have been transferred.</b>
    /// </summary>
    /// <param name="id1">The id of the first tag.</param>
    /// <param name="id2">The id of the second tag.</param>
    /// <returns>
    /// Returns a 200 OK response.
    /// </returns>
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
            predicate = PredicateBuilder.AddOr(predicate, t => t.CreatedBy == options.CreatedBy);
        }
        
        // Filter to only show current user's tags
        if (options.OnlyOwnedByCurrentUser)
        {
            Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid : null;
            if (userId.HasValue)
            {
                predicate = PredicateBuilder.AddOr(predicate, t => t.CreatedBy == userId);
            }
        }

        // Filter by approval status
        if (options.IsApproved.HasValue)
        {
            predicate = PredicateBuilder.AddOr(predicate, t => t.IsApproved == options.IsApproved.Value);
        }

        // Filter by standardization status
        if (options.IsStandardized.HasValue)
        {
            predicate = PredicateBuilder.AddOr(predicate, t => t.IsStandardized == options.IsStandardized.Value);
        }

        // Filter by text search
        if (!string.IsNullOrEmpty(options.SearchQuery))
        {
            predicate = PredicateBuilder.AddAnd(predicate, t => t.Name.ToLower().Contains(options.SearchQuery.ToLower()));
        }

        // Filter by creation date range
        if (options.CreatedFromDate.HasValue)
        {
            predicate = PredicateBuilder.AddOr(predicate, t => t.CreatedOn >= options.CreatedFromDate.Value);
        }

        if (options.CreatedToDate.HasValue)
        {
            predicate = PredicateBuilder.AddOr(predicate, t => t.CreatedOn <= options.CreatedToDate.Value);
        }

        // Filter by approval date range
        if (options.ApprovedFromDate.HasValue)
        {
            predicate = PredicateBuilder.AddOr(predicate, t => t.ApprovedOn >= options.ApprovedFromDate.Value);
        }

        if (options.ApprovedToDate.HasValue)
        {
            predicate = PredicateBuilder.AddOr(predicate, t => t.ApprovedOn <= options.ApprovedToDate.Value);
        }
        return predicate;
    }
    
    #endregion
} 


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


