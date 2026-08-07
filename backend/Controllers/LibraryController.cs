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
    [SwaggerResponse(200, "Library result")]
    public async Task<IActionResult> Browse([FromBody] LibraryRequest request)
        => Ok(await libraryService.GetLibraryAsync(request));

    [HttpPost("search-content")]
    [SwaggerOperation(Summary = "Search the library with content-aware chunk matching and snippets")]
    [SwaggerResponse(200, "Search results with matched content snippets")]
    public async Task<IActionResult> SearchContent([FromBody]LibraryContentSearchRequest request)
    {
        List<LibraryItemWithChunks> results = await libraryService.SearchContentAsync(request.Search, request.PageSize, excludeIds: request.ExcludeIds);

        var items = results.Select(r => new
        {
            r.Item.Id,
            r.Item.Name,
            r.Item.Description,
            r.Item.Type,
            r.Item.FileType,
            r.Item.SourceUrl,
            r.Item.CreatedOn,
            r.Item.PublicationDate,
            r.Item.PublicationDatePrecision,
            Chunks = r.MatchedChunks
        });

        return Ok(new { items, hasMore = results.Count == request.PageSize });
    }

    [HttpPost("items")]
    [SwaggerOperation(Summary = "Get library items by IDs")]
    [SwaggerResponse(200, "Library items")]
    public async Task<IActionResult> GetItemsByIds([FromBody] Guid[] ids, [FromQuery] bool includeAuthors = false)
    {
        LibraryItem[] items = await libraryService.GetLibraryItemsByIdsAsync(ids);
        if (!includeAuthors) return Ok(items);

        Guid[] resourceIds = [.. items.Where(i => i.Type == "resource").Select(i => i.Id)];
        Dictionary<Guid, List<RelationItemDto>> authorsByResource = await libraryService.GetAuthorNamesForResourcesAsync(resourceIds);

        var result = items.Select(i => new
        {
            i.Id,
            i.Name,
            i.Description,
            i.PublicationDate,
            i.PublicationDatePrecision,
            i.Type,
            i.FileType,
            i.SourceUrl,
            i.CreatedOn,
            Authors = authorsByResource.TryGetValue(i.Id, out List<RelationItemDto>? authors) ? authors : null
        });

        return Ok(result);
    }

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