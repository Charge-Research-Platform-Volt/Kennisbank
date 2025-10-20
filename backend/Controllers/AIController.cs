
using KnowledgeBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text.Json;
using KnowledgeBank.Responses;
using KnowledgeBank.Utils;
using KnowledgeBank.Data;
using System.Security.Claims;

namespace KnowledgeBank.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
[Produces("application/json")]
public class AIController : ControllerBase
{
    private readonly Serilog.ILogger _logger;
    private readonly RAGManger _ragManger;
    private readonly ResourceManager _resourceManager;

    public AIController(RAGManger ragManger, ResourceManager resourceManager)
    {
        _logger = Log.ForContext<AIController>();
        _resourceManager = resourceManager;
        _ragManger = ragManger;
    }


    [HttpPost("generate-tags")]
    public async Task<IActionResult> GenerateTags(string id)
    {
        // Check if the resource exists
        if (!ValidityUtil.IsValidId(id))
            return BadRequest(new ApiResponse(false, "Invalid ID."));

        try
        {
            // Check if resource exists
            if (!await _resourceManager.ResourceExistsAsync(id))
                return NotFound(new ApiResponse(false, "Resource not found."));


            // Get the existing AI-generated tags from the resource
            var existingAiTags = await _resourceManager.GetResourcePropertyOrDefaultAsync(Guid.Parse(id), "AiGeneratedTags");

            if (!string.IsNullOrWhiteSpace(existingAiTags))
            {
                try
                {
                    var existingTagsList = JsonSerializer.Deserialize<List<string>>(existingAiTags);
                    if (existingTagsList != null)
                    {
                        _logger.Information("Returning existing AI-generated tags for resource {ResourceId}", id);
                        return Ok(new ApiResponse(true, "AI-generated tags already exist", new
                        {
                            Tags = existingTagsList
                        }));
                    }
                }
                catch (JsonException)
                {
                    _logger.Warning("Failed to deserialize existing AI-generated tags for resource {ResourceId}. Regenerating tags.", id);
                }
            }

            return Ok(new ApiResponse(true, "Tags generated successfully", new { Tags = new List<string>() }));
        }
        catch (Exception)
        {
            _logger.Error("An error occurred while generating tags.");
            return StatusCode(500, new ApiResponse(false, "An error occurred while generating tags."));
        }
    }


    [HttpGet("all-chats")]
    public async Task<IActionResult> GetAllChats()
    {
        try
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.Warning("User ID not found in claims.");
                return BadRequest(new ApiResponse(false, "User ID not found."));
            }

            var chats = await _resourceManager.GetChatsGroupedByDateAsync(predicate: c => c.UserId == Guid.Parse(userId));

            if (chats == null)
            {
                _logger.Warning("No chats found.");
                return NotFound(new ApiResponse(false, "No chats found."));
            }

            return Ok(new ApiResponse(true, "Chats retrieved successfully", new { Chats = chats }));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while retrieving all chats.");
            return StatusCode(500, new ApiResponse(false, "An error occurred while retrieving chats."));
        }
    }

    // Delete chat by ID
    [HttpDelete("delete-chat/{chatId}")]
    public async Task<IActionResult> DeleteChat(Guid chatId)
    {
        if (!ValidityUtil.IsValidId(chatId.ToString()))
            return BadRequest(new ApiResponse(false, "Invalid chat ID."));

        try
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.Warning("User ID not found in claims.");
                return BadRequest(new ApiResponse(false, "User ID not found."));
            }

            var result = await _resourceManager.DeleteChatAsync(chatId, Guid.Parse(userId));
            if (result)
            {
                _logger.Information("Chat with ID {ChatId} deleted successfully.", chatId);
                return Ok(new ApiResponse(true, "Chat deleted successfully."));
            }
            else
            {
                _logger.Warning("Failed to delete chat with ID {ChatId}. Chat not found or user not authorized.", chatId);
                return BadRequest(new ApiResponse(false, "Chat not found or you are not authorized to delete it."));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while deleting chat with ID {ChatId}.", chatId);
            return StatusCode(500, new ApiResponse(false, "An error occurred while deleting the chat."));
        }
    }


    [HttpGet("messages/{chatId}")]
    public async Task<IActionResult> GetMessagesByChatId(Guid chatId)
    {
        if (!ValidityUtil.IsValidId(chatId.ToString()))
            return BadRequest(new ApiResponse(false, "Invalid chat ID."));

        try
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.Warning("User ID not found in claims.");
                return BadRequest(new ApiResponse(false, "User ID not found."));
            }

            // First check if the chat exists
            var chatExists = await _resourceManager.GetChatAsync(c => c.Id == chatId);
            if (chatExists == null)
            {
                _logger.Warning("Chat with ID {ChatId} not found", chatId);
                return NotFound(new ApiResponse(false, "Chat not found."));
            }

            // Check if the user is authorized to access this chat
            if (chatExists.UserId != Guid.Parse(userId))
            {
                _logger.Warning("User {UserId} is not authorized to access chat {ChatId}", userId, chatId);
                return Unauthorized(new ApiResponse(false, "You are not authorized to access this chat."));
            }

            var messages = await _resourceManager.GetMessagesByChatIdAsync(chatId);

            if (messages == null || !messages.Any())
            {
                _logger.Warning("No messages found for chat ID {ChatId}", chatId);
                return NotFound(new ApiResponse(false, "No messages found for this chat."));
            }

            return Ok(new ApiResponse(true, "Messages retrieved successfully", new { Messages = messages }));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while retrieving messages for chat ID {ChatId}", chatId);
            return StatusCode(500, new ApiResponse(false, "An error occurred while retrieving messages."));
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


