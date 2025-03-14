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
    [SwaggerResponse(200, "List of search results", typeof(List<FileItem>))]
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

            FileItem[]? items = await database.Files
                .FromSqlRaw(@"
                    SELECT * FROM files 
                    ORDER BY similarity(name, {0}) DESC", query)
                .Skip(skip).Take(pageSize)
                .ToArrayAsync();


            if (items == null)
                return Ok(new PageResponse("No files found.", pageIndex, pageSize, Array.Empty<FileItem>()));

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
    [SwaggerResponse(200, "List of search results", typeof(List<FileItem>))]
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

            FileItem[]? items = await database.Files
                .FromSqlRaw(@"
                    SELECT * FROM files 
                    ORDER BY similarity(description, {0}) DESC", query)
                .Skip(skip).Take(pageSize)
                .ToArrayAsync();

            if (items == null)
                return Ok(new PageResponse("No files found.", pageIndex, pageSize, Array.Empty<FileItem>()));

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
    [SwaggerResponse(200, "List of search results", typeof(List<FileItem>))]
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
            FileItem[]? items = await database.Files
                .FromSqlRaw(@"
                    SELECT 
                        f.*, 
                        ts_rank(fv.vector, websearch_to_tsquery('english', {0})) AS rank 
                    FROM files f
                    JOIN file_vectors fv ON fv.file_id = f.id
                    WHERE fv.vector @@ websearch_to_tsquery('english', {0})
                    ORDER BY rank DESC", query)
                .Skip(skip).Take(pageSize)
                .ToArrayAsync();

            if (items == null)
                return Ok(new PageResponse("No files found.", pageIndex, pageSize, Array.Empty<FileItem>()));

            return Ok(new PageResponse($"{items.Length} files found.", pageIndex, pageSize, items));
        }
        catch (Exception e)
        {
                logger.Error(e, "Error listing files on page {pageIndex} of size {pageSize}.", pageIndex, pageSize);
                return StatusCode(500, new StorageResponse("Error listing files."));
        }
    }
}


