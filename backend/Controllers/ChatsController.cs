using System.Security.Claims;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using KnowledgeBank.Data;

namespace KnowledgeBank.Controllers;

[Route("chats")]
[Authorize]
public class ChatsController(ChatService chatService, MessageAttachmentService messageAttachmentService) : AppControllerBase
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
    [SwaggerOperation(Summary = "Get messages for a chat, most recent page first")]
    [SwaggerResponse(200, "Messages")]
    [SwaggerResponse(403, "Not your chat")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> GetMessages(Guid id, [FromQuery] DateTime? before, [FromQuery] int limit = 3)
    {
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Chats? chat = await chatService.GetByIdAsync(id);

        if (chat == null) return NotFound();
        if (chat.UserId != userId) return Forbid();

        var (items, hasMore) = await chatService.GetMessagesAsync(id, before, limit);
        return Ok(new { items, hasMore });
    }

    [HttpPost("{id}/attachments")]
    [SwaggerOperation(Summary = "Attach an uploaded file to a chat, extracting its text")]
    [SwaggerResponse(200, "Attachment created")]
    [SwaggerResponse(400, "Unsupported file type")]
    [SwaggerResponse(403, "Not your chat")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> AttachFile(Guid id, [FromBody]MessageAttachmentCreateDto dto)
    {
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Chats? chat = await chatService.GetByIdAsync(id);

        if (chat == null) return NotFound();
        if (chat.UserId != userId) return Forbid();

        if (!Guid.TryParse(dto.ObjectName, out Guid objectId))
            return Problem("Invalid object name.", statusCode: 400);

        string extension = Path.GetExtension(dto.FileName);
        if (!Filetype.SupportedText(extension))
            return Problem($"File type '{extension}' is not supported for chat attachments.", statusCode: 400);

        MessageAttachments attachment = await messageAttachmentService.CreatePendingAttachmentAsync(id, objectId, dto.FileName);

        return Ok(new { attachment.Id, attachment.FileName });
    }

    [HttpGet("{id}/attachments")]
    [SwaggerOperation(Summary = "Get all attachments for a chat")]
    [SwaggerResponse(200, "Attachments")]
    [SwaggerResponse(403, "Not your chat")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> GetAttachments(Guid id)
    {
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Chats? chat = await chatService.GetByIdAsync(id);

        if (chat == null) return NotFound();
        if (chat.UserId != userId) return Forbid();

        List<MessageAttachments> attachments = await chatService.GetAttachmentsForChatAsync(id);
        return Ok(attachments.Select(a => new { a.Id, a.FileName, a.Detached }));
    }

    [HttpDelete("{id}/attachments/{attachmentId}")]
    [SwaggerOperation(Summary = "Detach an attachment from a chat so it's no longer used in future AI turns")]
    [SwaggerResponse(204, "Detached")]
    [SwaggerResponse(403, "Not your chat")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> RemoveAttachment(Guid id, Guid attachmentId)
    {
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Chats? chat = await chatService.GetByIdAsync(id);

        if (chat == null) return NotFound();
        if (chat.UserId != userId) return Forbid();

        bool removed = await messageAttachmentService.DetachAttachmentAsync(id, attachmentId);
        return NoContentOrNotFound(removed);
    }
}