using backend.Data;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
public class DriveController : ControllerBase
{
    // Database context
    private readonly DatabaseContext _context;
    public DriveController(DatabaseContext context)
    {
        _context = context;
    }

    // ----------- Endpoints:

    /// <summary>
    /// Retrieves all documents from the drive.
    /// </summary>
    /// <returns>
    /// Returns a 200 OK response containing a list of all Drive entities.
    /// </returns>
    [HttpGet]
    [Route("all-documents")]
    public IActionResult Get()
    {
        try
        {
            return Ok(_context.Files.ToList());
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve documents");
            return StatusCode(500, new { message = "An error occurred while retrieving documents." });
        }
    }


    [HttpPost]
    [Route("add-document")]
    public async Task<IActionResult> Post([FromForm] StorageUploadDto driveDto)
    {
        // Make sure we have the required fields from body
        if (string.IsNullOrEmpty(driveDto.Name) || string.IsNullOrEmpty(driveDto.Description))
        {
            Log.Error("Name and Description are required.");
            return BadRequest(new { message = "Name and Description are required." });
        }

        string extension = Path.GetExtension(driveDto.File.FileName);

        if (!Filetype.Supported(extension))
            return BadRequest(new { message = "Filetype is not supported." });

        FileItem drive = new()
        {
            Id = Guid.NewGuid(), // Generate a new GUID
            Name = driveDto.Name,
            Description = driveDto.Description,
            FileType = Filetype.ConvertExtensionToFiletype(extension)
        };

        await _context.Files.AddAsync(drive);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Document created." });
    }


    // Delete a document and update a document can be added here
}
