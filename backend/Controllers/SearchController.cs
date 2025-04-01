using Microsoft.AspNetCore.Mvc;
using backend.Data;
using Microsoft.AspNetCore.StaticFiles;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using backend.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;


[ApiController]
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

    [HttpGet("search-name")]
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
        [FromQuery] int pageSize = 20)
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

            Resource[]? items = await database.Resources
                .FromSqlRaw(@"
                    SELECT * FROM resources 
                    ORDER BY similarity(name, {0}) DESC", query)
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

    [HttpGet("search-description")]
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
        [FromQuery] int pageSize = 20)
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

            Resource[]? items = await database.Resources
                .FromSqlRaw(@"
                    SELECT * FROM resources 
                    ORDER BY similarity(description, {0}) DESC", query)
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

    [HttpGet("search-full-text")]
    [SwaggerOperation(
        Summary = "FTS the database by name and description.",
        Description = "FTS for files in database based by name and description, with pagination."
    )]
    [SwaggerResponse(200, "List of search results", typeof(List<Resource>))]
    [SwaggerResponse(400, "Invalid search query")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> FullTextSearch(
        [FromQuery] string query,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20)
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
            Resource[]? items = await database.Resources
                .FromSqlRaw(@"
                    SELECT DISTINCT ON (f.id)
                        f.*, 
                        GREATEST(
                            ts_rank(fv.vector, websearch_to_tsquery('english', {0})),
                            similarity(f.name, {0}),
                            similarity(f.description, {0})
                        ) AS rank 
                    FROM resources f
                    JOIN resource-vectors fv ON fv.resource-id = f.id
                    WHERE fv.vector @@ websearch_to_tsquery('english', {0})
                        OR similarity(f.name, {0}) > 0.3
                        OR similarity(f.description, {0}) > 0.3
                    ORDER BY f.id, rank DESC", query)
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
}


