using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("library")]
[Authorize]
public class LibraryController(LibraryService libraryService) : AppControllerBase
{
    [HttpPost]
    [SwaggerOperation(Summary = "Browse or search the library with filters and pagination")]
    [SwaggerResponse(200, "Grid result")]
    public async Task<IActionResult> Browse([FromBody] GridRequest request)
        => Ok(await libraryService.GetGridAsync(request));

    [HttpPost("items")]
    [SwaggerOperation(Summary = "Get grid items by IDs")]
    [SwaggerResponse(200, "Grid items")]
    public async Task<IActionResult> GetItemsByIds([FromBody] Guid[] ids)
        => Ok(await libraryService.GetGridItemsByIdsAsync(ids));

    [HttpGet("trash")]
    [SwaggerOperation(Summary = "Get all trashed items")]
    [SwaggerResponse(200, "List of trash items")]
    public async Task<IActionResult> Trash()
        => Ok(await libraryService.GetTrashItemsAsync());

    [HttpGet("supported-extensions")]
    [SwaggerOperation(Summary = "Get a list of supported extensions")]
    [SwaggerResponse(200, "List of supported file extensions")]
    public IActionResult SupportedExtensions()
        => Ok(Filetype.SupportedExtensions);
}