using Microsoft.AspNetCore.Mvc;
using KnowledgeBank.Data;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Responses;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using KnowledgeBank.Controllers;

namespace backend.Controllers;


[ApiController]
[Authorize]
[Route("[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly IAzureBlobService blobService;
    private readonly Serilog.ILogger logger;
    private readonly DatabaseContext database;

    public SearchController(IAzureBlobService blobService, DatabaseContext databaseContext)
    {
        this.blobService = blobService;
        this.logger = Log.ForContext<StorageController>();
        this.database = databaseContext;
    }

    [HttpPost("search-name")]
    [SwaggerOperation(
        Summary = "Search database by name.",
        Description = "Searches for files in database based on a given name, with pagination."
    )]
    [SwaggerResponse(200, "List of search results", typeof(List<Resource>))]
    [SwaggerResponse(400, "Invalid search query")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> SearchByName(
        [FromQuery] string query,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromBody] FilterDto? filter = null)
    {
        if (pageIndex < 1)
            return BadRequest(new StorageResponse("Page index cannot be lower than 1."));

        if (pageSize < 1)
            return BadRequest(new StorageResponse("Page size cannot be lower than 1."));

        if (string.IsNullOrEmpty(query))
            return BadRequest(new StorageResponse("Query is required."));

        try
        {
            int skip = (pageIndex - 1) * pageSize;

            var queryBuilder = filter?.ToQueryBuilder(database) ?? database.Resources.AsQueryable();

            Resource[]? items = await queryBuilder
                .OrderByDescending(f => EF.Functions.TrigramsSimilarity(f.Title, query))
                .Skip(skip).Take(pageSize)
                .ToArrayAsync();

            if (items == null)
                return Ok(new PageResponse("No files found.", pageIndex, pageSize, Array.Empty<Resource>()));

            return Ok(new PageResponse($"{items.Length} files found.", pageIndex, pageSize, items));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing files on page {pageIndex} of size {pageSize}.", pageIndex, pageSize);
            return StatusCode(500, new StorageResponse("Error listing files."));
        }
    }

    [HttpPost("search-description")]
    [SwaggerOperation(
        Summary = "Search database by description.",
        Description = "Searches for files in database based on a given description, with pagination."
    )]
    [SwaggerResponse(200, "List of search results", typeof(List<Resource>))]
    [SwaggerResponse(400, "Invalid search query")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> SearchByDescription(
        [FromQuery] string query,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromBody] FilterDto? filter = null)
    {
        if (pageIndex < 1)
            return BadRequest(new StorageResponse("Page index cannot be lower than 1."));

        if (pageSize < 1)
            return BadRequest(new StorageResponse("Page size cannot be lower than 1."));

        if (string.IsNullOrEmpty(query))
            return BadRequest(new StorageResponse("Query is required."));

        try
        {
            int skip = (pageIndex - 1) * pageSize;

            var queryBuilder = filter?.ToQueryBuilder(database) ?? database.Resources.AsQueryable();

            Resource[]? items = await queryBuilder
                .OrderByDescending(f => EF.Functions.TrigramsSimilarity(f.Description ?? "", query))
                .Skip(skip).Take(pageSize)
                .ToArrayAsync();

            if (items == null)
                return Ok(new PageResponse("No files found.", pageIndex, pageSize, Array.Empty<Resource>()));

            return Ok(new PageResponse($"{items.Length} files found.", pageIndex, pageSize, items));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing files on page {pageIndex} of size {pageSize}.", pageIndex, pageSize);
            return StatusCode(500, new StorageResponse("Error listing files."));
        }
    }

    [HttpPost("search-full-text")]
    [SwaggerOperation(
        Summary = "FTS the database by name and description.",
        Description = "FTS for files in database based by name and description, with pagination."
    )]
    [SwaggerResponse(200, "List of search results", typeof(List<Resource>))]
    [SwaggerResponse(400, "Invalid search query")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> FullTextSearch(
        [FromQuery] string? query,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromBody] FilterDto? filter = null)
    {
        if (pageIndex < 1)
            return BadRequest(new StorageResponse("Page index cannot be lower than 1."));

        if (pageSize < 1)
            return BadRequest(new StorageResponse("Page size cannot be lower than 1."));

        // If the query is empty, return all files
        try
        {
            int skip = (pageIndex - 1) * pageSize;
            Resource[]? items;

            var queryBuilder = filter?.ToQueryBuilder(database) ?? database.Resources.AsQueryable();

            if (string.IsNullOrEmpty(query))
            {
                // No query provided: return all files with default ordering
                items = await queryBuilder
                    .OrderByDescending(f => f.Id)
                    .Skip(skip).Take(pageSize)
                    .ToArrayAsync();
            }
            else
            {
                //  Queries for websearch_to_tsquery are interpreted as following:
                //
                //  integration europe 1995             =>  integration AND europe AND 1995
                //  "integration europe" 1995           =>  integration europe AND 1995
                //  integration europe -1995            =>  integration AND europe NOT 1995
                //  "integration europe" 1995           =>  integration europe NOT 1995
                //  integration europe or asia 1995     =>  (integration AND europe) OR (asia AND 1995)
                //
                //  Keep in mind these examples do not take stemming into consideration, the words 
                //  in the query and database are stemmed to improve search results.  

                // Converts the user query to a tsvector and compares this to the file vector
                var tsQuery = string.Join(" & ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(term => term + ":*"));
                
                // Execute query
                var itemsFromRawSql = database.Resources
                    .FromSqlRaw(@"
                        SELECT DISTINCT ON (f.id)
                            f.*,
                            GREATEST(
                                ts_rank(fv.vector, websearch_to_tsquery('english', {0})),
                                similarity(f.title, {0}),
                                similarity(f.description, {0})
                            ) AS rank 
                        FROM resources f
                        JOIN ""resource-vectors"" fv ON fv.""resource-id"" = f.id
                        WHERE fv.vector @@ websearch_to_tsquery('english', {0})
                            OR similarity(f.title, {1}) > 0.3
                            OR similarity(f.description, {1}) > 0.3
                        ORDER BY f.id, rank DESC", tsQuery, query)
                    .AsQueryable();

                // save the order
                var orderedIds = await itemsFromRawSql
                    .Select(f => f.Id)
                    .ToListAsync();

                // combine filters with query result
                items = await queryBuilder
                    .Where(f => itemsFromRawSql.Any(sqlItem => sqlItem.Id == f.Id)) // filter the files based on the IDs from the SQL query
                    .OrderBy(f => orderedIds.IndexOf(f.Id)) // maintain the order of the IDs from the SQL query
                    .Skip(skip).Take(pageSize)
                    .ToArrayAsync();
            }

            if (items == null)
                return Ok(new PageResponse("No files found.", pageIndex, pageSize, Array.Empty<Resource>()));

            return Ok(new PageResponse($"{items.Length} files found.", pageIndex, pageSize, items));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing files on page {pageIndex} of size {pageSize}.", pageIndex, pageSize);
            return StatusCode(500, new StorageResponse("Error listing files."));
        }
    }

    [HttpGet("get-oldest-document")]
    [SwaggerOperation(
        Summary = "Returns the oldest document.",
        Description = "Searches the whole Files table and returns the oldest one from that table."
    )]
    [SwaggerResponse(200, "Oldest document", typeof(List<Resource>))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetOldestDocument()
    {
        try
        {
            // Get the oldest document from the files table
            // TODO: change created_at to published_at when metadata is merged
            Resource[]? items = await database.Resources
            .FromSqlRaw("SELECT * FROM files ORDER BY created_at")
            .Take(1)
            .ToArrayAsync();

            if (items == null)
                return Ok(new ResourceInfoResponse("No files found", null));

            return Ok(new ResourceInfoResponse("Oldest file found", items[0]));
        }
        catch(Exception)
        {
            return StatusCode(500, new StorageResponse("Error checking files."));
        }

    }
}


