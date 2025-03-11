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
    public async Task<IActionResult> SearchByName(
        [FromQuery] string query,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
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

            // Filter on query, then skip and take 
            FileItem[]? items = await database.Files.Where(f => f.Name.Contains(query.ToLower()))
                .Skip(skip).Take(pageSize).ToArrayAsync();

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


