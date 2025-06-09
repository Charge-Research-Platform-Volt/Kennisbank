
using KnowledgeBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text.Json;
using KnowledgeBank.Responses;
using KnowledgeBank.Utils;
using KnowledgeBank.Data;

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


            _logger.Information("No existing AI-generated tags found for resource {ResourceId}. Generating new tags.", id);
            return Ok(new ApiResponse(true, "Tags generated successfully", new { Tags = _ragManger.GenerateTagsAsync(id) }));
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
            var chats = await _resourceManager.GetChatsGroupedByDateAsync();
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
            var result = await _resourceManager.DeleteChatAsync(chatId);
            if (result)
            {
                _logger.Information("Chat with ID {ChatId} deleted successfully.", chatId);
                return Ok(new ApiResponse(true, "Chat deleted successfully."));
            }
            else
            {
                _logger.Warning("Chat with ID {ChatId} not found.", chatId);
                return NotFound(new ApiResponse(false, "Chat not found."));
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
            // First check if the chat exists
            var chatExists = await _resourceManager.GetChatAsync(chatId);
            if (chatExists == null)
            {
                _logger.Warning("Chat with ID {ChatId} not found", chatId);
                return NotFound(new ApiResponse(false, "Chat not found."));
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
