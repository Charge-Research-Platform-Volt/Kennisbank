using System.Security.Claims;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("chats")]
[Authorize]
public class ChatsController(ChatService chatService) : AppControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Get all chats for the current user grouped by date")]
    [SwaggerResponse(200, "Chats grouped by date")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId)
    {
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await chatService.GetChatsGroupedByDateAsync(userId, projectId));
    }

    [HttpPatch("{id}/title")]
    [SwaggerOperation(Summary = "Rename a chat")]
    [SwaggerResponse(204, "Renamed")]
    [SwaggerResponse(403, "Not your chat")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Problem("Title cannot be empty.", statusCode: 400);

        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Chats? chat = await chatService.GetByIdAsync(id);

        if (chat == null) return NotFound();
        if (chat.UserId != userId) return Forbid();

        await chatService.UpdateTitleAsync(id, title.Trim());
        return NoContent();
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Delete a chat")]
    [SwaggerResponse(204, "Deleted")]
    [SwaggerResponse(403, "Not your chat")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Chats? chat = await chatService.GetByIdAsync(id);

        if (chat == null) return NotFound();
        if (chat.UserId != userId) return Forbid();

        await chatService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/messages")]
    [SwaggerOperation(Summary = "Get messages for a chat")]
    [SwaggerResponse(200, "Messages")]
    [SwaggerResponse(403, "Not your chat")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> GetMessages(Guid id)
    {
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Chats? chat = await chatService.GetByIdAsync(id);

        if (chat == null) return NotFound();
        if (chat.UserId != userId) return Forbid();

        return Ok(await chatService.GetMessagesAsync(id));
    }
}