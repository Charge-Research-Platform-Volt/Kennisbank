using KnowledgeBank.Services.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("merge-suggestions")]
[Authorize]
public class MergeSuggestionsController(MergeSuggestionService mergeSuggestionService) : AppControllerBase
{
    [HttpPost("dismiss/{entityType}/{id1:guid}/{id2:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Dismiss a merge suggestion so it stops appearing")]
    [SwaggerResponse(204, "Dismissed")]
    public async Task<IActionResult> Dismiss(string entityType, Guid id1, Guid id2)
    {
        await mergeSuggestionService.DismissAsync(entityType, id1, id2);
        return NoContent();
    }

    [HttpDelete("dismiss/{entityType}/{id1:guid}/{id2:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "(Admin only) Undo a dismissed merge suggestion")]
    [SwaggerResponse(204, "Restored")]
    public async Task<IActionResult> Undo(string entityType, Guid id1, Guid id2)
    {
        await mergeSuggestionService.UndoAsync(entityType, id1, id2);
        return NoContent();
    }
}