using Microsoft.AspNetCore.Mvc;
using KnowledgeBank.Data;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Responses;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace KnowledgeBank.Controllers;


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
        this.logger = Log.ForContext<ResourcesController>();
        this.database = databaseContext;
    }

    [HttpPost("search-title")]
    [SwaggerOperation(
        Summary = "Search database by title.",
        Description = "Searches for resources in database based on a given title, with pagination."
    )]
    [SwaggerResponse(200, "List of search results", typeof(List<Resource>))]
    [SwaggerResponse(400, "Invalid search query")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> SearchByTitle(
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

            IQueryable<Resource> queryBuilder = filter?.ToQueryBuilder(database) ?? database.Resources.AsQueryable();

            Resource[]? items = await queryBuilder
                .OrderByDescending(f => EF.Functions.TrigramsSimilarity(f.Title, query))
                .Skip(skip).Take(pageSize)
                .ToArrayAsync();

            if (items == null)
                return Ok(new PageResponse("No resources found.", pageIndex, pageSize, Array.Empty<Resource>()));

            return Ok(new PageResponse($"{items.Length} resources found.", pageIndex, pageSize, items));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing resources on page {pageIndex} of size {pageSize}.", pageIndex, pageSize);
            return StatusCode(500, new StorageResponse("Error listing resources."));
        }
    }

    [HttpPost("search-description")]
    [SwaggerOperation(
        Summary = "Search database by description.",
        Description = "Searches for resources in database based on a given description, with pagination."
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

            IQueryable<Resource> queryBuilder = filter?.ToQueryBuilder(database) ?? database.Resources.AsQueryable();

            Resource[]? items = await queryBuilder
                .OrderByDescending(f => EF.Functions.TrigramsSimilarity(f.Description ?? "", query))
                .Skip(skip).Take(pageSize)
                .ToArrayAsync();

            if (items == null)
                return Ok(new PageResponse("No resources found.", pageIndex, pageSize, Array.Empty<Resource>()));

            return Ok(new PageResponse($"{items.Length} resources found.", pageIndex, pageSize, items));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing resources on page {pageIndex} of size {pageSize}.", pageIndex, pageSize);
            return StatusCode(500, new StorageResponse("Error listing resources."));
        }
    }

    [HttpPost("search-full-text")]
    [SwaggerOperation(
        Summary = "FTS the database by title and description.",
        Description = "FTS for resources in database based by title and description, with pagination."
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

        // If the query is empty, return all resources
        try
        {
            int skip = (pageIndex - 1) * pageSize;
            Resource[]? items;

            IQueryable<Resource> queryBuilder = filter?.ToQueryBuilder(database) ?? database.Resources.AsQueryable();

            if (string.IsNullOrEmpty(query))
            {
                // No query provided: return all resources with default ordering
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

                // Converts the user query to a tsvector and compares this to the resource vector
                string tsQuery = string.Join(" & ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(term => term + ":*"));

                // Execute query
                IQueryable<Resource> itemsFromRawSql = database.Resources
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
                List<Guid> orderedIds = await itemsFromRawSql
                    .Select(f => f.Id)
                    .ToListAsync();

                // combine filters with query result
                items = await queryBuilder
                    .Where(f => itemsFromRawSql.Any(sqlItem => sqlItem.Id == f.Id)) // filter the resources based on the IDs from the SQL query
                    .OrderBy(f => orderedIds.IndexOf(f.Id)) // maintain the order of the IDs from the SQL query
                    .Skip(skip).Take(pageSize)
                    .ToArrayAsync();
            }

            if (items == null)
                return Ok(new PageResponse("No resources found.", pageIndex, pageSize, Array.Empty<Resource>()));

            return Ok(new PageResponse($"{items.Length} resources found.", pageIndex, pageSize, items));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing resources on page {pageIndex} of size {pageSize}.", pageIndex, pageSize);
            return StatusCode(500, new StorageResponse("Error listing resources."));
        }
    }

    [HttpGet("get-oldest-document")]
    [SwaggerOperation(
        Summary = "Returns the oldest document year.",
        Description = "Searches the whole Resources table and returns the oldest one from that table."
    )]
    [SwaggerResponse(200, "Oldest document", typeof(List<Resource>))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetOldestDocument()
    {
        try
        {
            // Get the oldest document from the resources table
            Resource[]? items = await database.Resources
            .OrderBy(f => f.PublicationDate)
            .Take(1)
            .ToArrayAsync();

            if (items == null)
                return Ok(new ResourceInfoResponse("No resources found", ""));

            return Ok(new ResourceInfoResponse("Oldest resources found", items[0].PublicationDate.Year));
        }
        catch(Exception)
        {
            return StatusCode(500, new StorageResponse("Error checking resources."));
        }

    }
}




// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


