
using KnowledgeBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text.Json;
using KnowledgeBank.Responses;
using KnowledgeBank.Utils;
using KnowledgeBank.Data;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using System.Net.Http;
using SmartReader;
using HandlebarsDotNet.Helpers.BlockHelpers;
using PuppeteerSharp;
using KnowledgeBank.Models;

namespace KnowledgeBank.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
[Produces("application/json")]
public class AIController(RAGManager ragManager, ResourceManager resourceManager, IAzureBlobService blobService, TextExtractionService textExtractionService) : ControllerBase
{
    private readonly Serilog.ILogger logger = Log.ForContext<AIController>();

    #region Generate Tags
    [HttpPost("generate-tags")]
    public async Task<IActionResult> GenerateTags(string id)
    {
        // Check if the resource exists
        if (!ValidityUtil.IsValidId(id))
            return BadRequest(new ApiResponse(false, "Invalid ID."));

        try
        {
            // Check if resource exists
            if (!await resourceManager.ResourceExistsAsync(id))
                return NotFound(new ApiResponse(false, "Resource not found."));


            // Get the existing AI-generated tags from the resource
            var existingAiTags = await resourceManager.GetResourcePropertyOrDefaultAsync(Guid.Parse(id), "AiGeneratedTags");

            if (!string.IsNullOrWhiteSpace(existingAiTags))
            {
                try
                {
                    var existingTagsList = JsonSerializer.Deserialize<List<string>>(existingAiTags);
                    if (existingTagsList != null)
                    {
                        logger.Information("Returning existing AI-generated tags for resource {ResourceId}", id);
                        return Ok(new ApiResponse(true, "AI-generated tags already exist", new
                        {
                            Tags = existingTagsList
                        }));
                    }
                }
                catch (JsonException)
                {
                    logger.Warning("Failed to deserialize existing AI-generated tags for resource {ResourceId}. Regenerating tags.", id);
                }
            }

            return Ok(new ApiResponse(true, "Tags generated successfully", new { Tags = new List<string>() }));
        }
        catch (Exception)
        {
            logger.Error("An error occurred while generating tags.");
            return StatusCode(500, new ApiResponse(false, "An error occurred while generating tags."));
        }
    }
    #endregion

    #region Get Chats
    [HttpGet("all-chats")]
    public async Task<IActionResult> GetAllChats()
    {
        try
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                logger.Warning("User ID not found in claims.");
                return BadRequest(new ApiResponse(false, "User ID not found."));
            }

            var chats = await resourceManager.GetChatsGroupedByDateAsync(predicate: c => c.UserId == Guid.Parse(userId));

            if (chats == null)
            {
                logger.Warning("No chats found.");
                return NotFound(new ApiResponse(false, "No chats found."));
            }

            return Ok(new ApiResponse(true, "Chats retrieved successfully", new { Chats = chats }));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while retrieving all chats.");
            return StatusCode(500, new ApiResponse(false, "An error occurred while retrieving chats."));
        }
    }
    #endregion

    #region Delete Chat
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
                logger.Warning("User ID not found in claims.");
                return BadRequest(new ApiResponse(false, "User ID not found."));
            }

            var result = await resourceManager.DeleteChatAsync(chatId, Guid.Parse(userId));
            if (result)
            {
                logger.Information("Chat with ID {ChatId} deleted successfully.", chatId);
                return Ok(new ApiResponse(true, "Chat deleted successfully."));
            }
            else
            {
                logger.Warning("Failed to delete chat with ID {ChatId}. Chat not found or user not authorized.", chatId);
                return BadRequest(new ApiResponse(false, "Chat not found or you are not authorized to delete it."));
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while deleting chat with ID {ChatId}.", chatId);
            return StatusCode(500, new ApiResponse(false, "An error occurred while deleting the chat."));
        }
    }
    #endregion

    #region Get Messages by ID
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
                logger.Warning("User ID not found in claims.");
                return BadRequest(new ApiResponse(false, "User ID not found."));
            }

            // First check if the chat exists
            var chatExists = await resourceManager.GetChatAsync(c => c.Id == chatId);
            if (chatExists == null)
            {
                logger.Warning("Chat with ID {ChatId} not found", chatId);
                return NotFound(new ApiResponse(false, "Chat not found."));
            }

            // Check if the user is authorized to access this chat
            if (chatExists.UserId != Guid.Parse(userId))
            {
                logger.Warning("User {UserId} is not authorized to access chat {ChatId}", userId, chatId);
                return Unauthorized(new ApiResponse(false, "You are not authorized to access this chat."));
            }

            var messages = await resourceManager.GetMessagesByChatIdAsync(chatId);

            if (messages == null || !messages.Any())
            {
                logger.Warning("No messages found for chat ID {ChatId}", chatId);
                return NotFound(new ApiResponse(false, "No messages found for this chat."));
            }

            return Ok(new ApiResponse(true, "Messages retrieved successfully", new { Messages = messages }));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while retrieving messages for chat ID {ChatId}", chatId);
            return StatusCode(500, new ApiResponse(false, "An error occurred while retrieving messages."));
        }
    }
    #endregion
    
    #region Extract Metadata
    [HttpGet("extract-metadata")]
    [SwaggerOperation(Summary = "Extract metadata from document by ID")]
    [SwaggerResponse(200, "The extracted metadata", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
    [SwaggerResponse(404, "File Not Found", typeof(ApiResponse))]
    [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
    public async Task<IActionResult> ExtractMetadata(string type, string value) 
    {
        // Verify type
        if (type != "file" && type != "web")
            return BadRequest(new ApiResponse(false, "Invalid extraction type"));
    
        // Verify string
        if ((type == "file" && !ValidityUtil.IsValidId(value)) || (type == "web" && !ValidityUtil.IsValidUrl(value)))
            return BadRequest(new ApiResponse(false, "Invalid " + (type == "file" ? "ID" : "URL")));
            
        try 
        {
            ExtractedMetadata? metadata = null;
        
            if (type == "file") 
            {
                logger.Information("Metadata extraction requested for file with ID '{id}'", value);
            
                // Retrieve blob from Azure Storage
                BlobDownloadResponse? response = await blobService.DownloadBlobAsync("files", value);

                // Check if response is not empty, if so no file exists with this ID
                if (response == null)
                    return NotFound(new ApiResponse(false, $"There is no file with ID '{value}'"));

                // Extract extension from metadata
                string extension = ((BlobDownloadResponse)response).Metadata["extension"];
                string fileName = ((BlobDownloadResponse)response).Metadata["originalFileName"];

                // Verify that extension is supported
                if (!Filetype.SupportedText(extension))
                    return BadRequest(new ApiResponse(false, "This filetype is not supported for metadata extraction."));

                logger.Information("Extracting text from {FileType} document", extension);

                // Copy blob stream to MemoryStream (Azure stream is not seekable)
                using Stream blobStream = ((BlobDownloadResponse)response).FileStream;
                using MemoryStream memoryStream = new();
                await blobStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                string extractedText = await textExtractionService.ExtractTextFromFileAsync(memoryStream, extension);
                
                // Validate extracted text
                if (string.IsNullOrWhiteSpace(extractedText))
                    return BadRequest(new ApiResponse(false, "No text could be extracted from the document."));

                logger.Information("Text extracted successfully. Length: {TextLength} characters", extractedText.Length);

                // Extract metadata using LLM
                logger.Information("Analyzing document with LLM to extract metadata");

                metadata = await ragManager.ExtractMetadataFromFileAsync(extractedText, fileName);
            }
            else 
            {
                logger.Information("Metadata extraction requested for web with URL '{url}'", value);

                // Retrieve text and metadata from web using Readability
                ReadabilityResult readabilityResult = await textExtractionService.ExtractTextFromWebAsync(value);
                
                // Validate extracted text
                if (string.IsNullOrWhiteSpace(readabilityResult.TextContent))
                    return BadRequest(new ApiResponse(false, "No text could be extracted from the webpage."));
                    
                logger.Information("Text extracted successfully. Length: {TextLength} characters", readabilityResult.TextContent.Length);

                // Extract metadata using LLM
                logger.Information("Analyzing webpage with LLM to extract metadata");

                metadata = await ragManager.ExtractMetadataFromWebAsync(readabilityResult, value);
            }

            if (metadata == null)
                return StatusCode(500, new ApiResponse(false, "Failed to extract metadata"));

            logger.Information("Metadata extraction completed successfully for {title}", metadata.Title);

            return Ok(new ApiResponse(true, "Metadata extracted successfully", metadata));
        }
        catch (Exception e) 
        {
            logger.Error(e, "Error extracting metadata from {type} '{id}'.", type, value);
            return StatusCode(500, new ApiResponse(false, $"Internal error during metadata extraction: {e.Message}"));
        }
    }
    #endregion
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


