using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.controllers;

/// <summary>
/// This controller provides miscellaneous endpoints for the KnowledgeBank application.
/// 
/// Author: Justin Liem
/// </summary>
/// <param name="resourceManager"></param>
[ApiController]
[Obsolete]
[Route("[controller]")]
[Produces("application/json")]
public class MiscController(ResourceManager resourceManager) : ControllerBase
{
    private readonly ResourceManager resourceManager = resourceManager;

    /// <summary>
    /// Gets filetype ID from name.
    /// 
    /// Author: Justin Liem
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)

