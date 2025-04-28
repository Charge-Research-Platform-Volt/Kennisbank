using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class MiscController(ResourceManager resourceManager) : ControllerBase
{
    private readonly ResourceManager resourceManager = resourceManager;

    /// <summary>
    /// Gets filetype ID from name.
    /// </summary>
    /// <returns>
    /// Returns a 200 OK response containing the ID of the filetype.
    /// </returns>
    [HttpGet("get-filetype-id/{name}")]
    [SwaggerOperation(
            Summary = "Get filetype id.",
            Description = "Get filetype id by searching on name."
        )]
    [SwaggerResponse(200, "FileType ID", typeof(Tag[]))]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "FileType not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetFileType(string name)
    {
        if(string.IsNullOrEmpty(name))
        {
            return BadRequest("FileType name should not be null or empty");
        }

        try
        {
            if(! await resourceManager.ResourceTypeExistsAsync(t => t.Name == name))
            {
                return NotFound("FileType name not found");
            }

            ResourceType fetchTypeID = await resourceManager.GetResourceTypeAsync(predicate: t => t.Name == name);
            return Ok(fetchTypeID.Id.ToString());
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to retrieve type id");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

