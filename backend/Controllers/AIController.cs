using KnowledgeBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text.Json;
using KnowledgeBank.Models;
using KnowledgeBank.Utils;
using KnowledgeBank.Data;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using KnowledgeBank.Services.Storage;

namespace KnowledgeBank.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
[Produces("application/json")]
public class AIController(ResourceManager resourceManager, IServiceScopeFactory serviceScopeFactory, EnvironmentConfig environmentConfig) : ControllerBase
{
    private readonly Serilog.ILogger logger = Log.ForContext<AIController>();
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

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
            if (!await resourceManager.ResourceExistsAsync(Guid.Parse(id)))
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

    #region Rename Chat
    [HttpPatch("rename-chat/{chatId}")]
    public async Task<IActionResult> RenameChat(Guid chatId, [FromBody] string title)
    {
        if (!ValidityUtil.IsValidId(chatId.ToString()))
            return BadRequest(new ApiResponse(false, "Invalid chat ID."));

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new ApiResponse(false, "Title cannot be empty."));

        try
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new ApiResponse(false, "User ID not found."));

            // Verify ownership
            var chat = await resourceManager.GetChatAsync(c => c.Id == chatId && c.UserId == Guid.Parse(userId));
            if (chat == null)
                return NotFound(new ApiResponse(false, "Chat not found or you are not authorized."));

            await resourceManager.UpdateChatAsync(chatId, c => c.Title, title.Trim());
            return Ok(new ApiResponse(true, "Chat renamed successfully."));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while renaming chat {ChatId}.", chatId);
            return StatusCode(500, new ApiResponse(false, "An error occurred while renaming the chat."));
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

            return Ok(new ApiResponse(true, "Messages retrieved successfully", new { Messages = messages ?? [] }));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while retrieving messages for chat ID {ChatId}", chatId);
            return StatusCode(500, new ApiResponse(false, "An error occurred while retrieving messages."));
        }
    }
    #endregion
    
    #region Extract Metadata

    [HttpPost("extract-metadata/start")]
    [SwaggerOperation(Summary = "Start metadata extraction job")]
    [SwaggerResponse(200, "Job created", typeof(ApiResponse))]
    [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
    public IActionResult StartMetadataExtraction([FromQuery] string type, [FromQuery] string value)
    {
        // Verify type
        if (type != "file" && type != "web")
            return BadRequest(new ApiResponse(false, "Invalid extraction type"));

        // Verify string
        if ((type == "file" && !ValidityUtil.IsValidId(value)) || (type == "web" && !ValidityUtil.IsValidUrl(value)))
            return BadRequest(new ApiResponse(false, "Invalid " + (type == "file" ? "ID" : "URL")));

        // Create job
        var jobService = HttpContext.RequestServices.GetRequiredService<MetadataExtractionJobService>();
        var job = jobService.CreateJob(type, value);

        logger.Information("Created metadata extraction job {JobId} for {Type}: {Value}", job.JobId, type, value);

        // Start background processing
        _ = Task.Run(async () => await ProcessMetadataExtractionJob(job.JobId, type, value));

        return Ok(new ApiResponse(true, "Job created successfully", new { jobId = job.JobId }));
    }

    [HttpGet("extract-metadata/status/{jobId}")]
    [SwaggerOperation(Summary = "Get metadata extraction job status")]
    [SwaggerResponse(200, "Job status", typeof(ApiResponse))]
    [SwaggerResponse(404, "Job not found", typeof(ApiResponse))]
    public IActionResult GetJobStatus(string jobId)
    {
        var jobService = HttpContext.RequestServices.GetRequiredService<MetadataExtractionJobService>();
        var job = jobService.GetJob(jobId);

        if (job == null)
            return NotFound(new ApiResponse(false, "Job not found"));

        return Ok(new ApiResponse(true, "Job status retrieved", new
        {
            jobId = job.JobId,
            status = job.Status.ToString(),
            statusMessage = job.StatusMessage,
            progressPercentage = job.ProgressPercentage,
            result = job.Result,
            errorMessage = job.ErrorMessage,
            createdAt = job.CreatedAt,
            completedAt = job.CompletedAt
        }));
    }

    private async Task ProcessMetadataExtractionJob(string jobId, string type, string value)
    {
        // Create a new scope for this background task
        using var scope = _serviceScopeFactory.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<MetadataExtractionJobService>();
        var storageServiceScoped = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var textExtractionServiceScoped = scope.ServiceProvider.GetRequiredService<TextExtractionService>();
        var ragManagerScoped = scope.ServiceProvider.GetRequiredService<RAGManager>();

        try
        {
            jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Starting extraction...", 10);
            ExtractedMetadata? metadata = null;

            if (type == "file")
            {
                logger.Information("Metadata extraction requested for file with ID '{id}'", value);

                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Retrieving file...", 20);

                // Retrieve blob from Azure Storage
                ObjectDownloadResponse response = await storageServiceScoped.DownloadObjectAsync(bucketName, value);

                // Extract extension from metadata
                string extension = response.Metadata["extension"];
                string fileName = response.Metadata["originalFileName"];

                // Verify that extension is supported
                if (!Filetype.SupportedText(extension))
                {
                    jobService.SetJobError(jobId, "This filetype is not supported for metadata extraction");
                    return;
                }

                logger.Information("Extracting text from {FileType} document", extension);
                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Extracting text from document...", 30);

                // Copy blob stream to MemoryStream (Azure stream is not seekable)
                using Stream blobStream = response.Stream;
                using MemoryStream memoryStream = new();
                await blobStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                string extractedText = await textExtractionServiceScoped.ExtractTextFromFileAsync(memoryStream, extension);

                // Validate extracted text
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    jobService.SetJobError(jobId, "No text could be extracted from the document");
                    return;
                }

                logger.Information("Text extracted successfully. Length: {TextLength} characters", extractedText.Length);

                // Extract metadata using LLM
                logger.Information("Analyzing document with LLM to extract metadata");
                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Analyzing with AI...", 50);

                metadata = await ragManagerScoped.ExtractMetadataFromFileAsync(extractedText, fileName, () =>
                {
                    jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Finding similar entities...", 70);
                });
            }
            else
            {
                logger.Information("Metadata extraction requested for web with URL '{url}'", value);
                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Fetching webpage...", 20);

                // Retrieve text and metadata from web using Readability
                ReadabilityResult readabilityResult = await textExtractionServiceScoped.ExtractTextFromWebAsync(value);

                // Validate extracted text
                if (string.IsNullOrWhiteSpace(readabilityResult.TextContent))
                {
                    jobService.SetJobError(jobId, "No text could be extracted from the webpage");
                    return;
                }

                logger.Information("Text extracted successfully. Length: {TextLength} characters", readabilityResult.TextContent.Length);

                // Extract metadata using LLM
                logger.Information("Analyzing webpage with LLM to extract metadata");
                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Analyzing with AI...", 50);

                metadata = await ragManagerScoped.ExtractMetadataFromWebAsync(readabilityResult, value, () =>
                {
                    jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Finding similar entities...", 70);
                });
            }

            if (metadata == null)
            {
                jobService.SetJobError(jobId, "Failed to extract metadata");
                return;
            }

            logger.Information("Metadata extraction completed successfully for {title}", metadata.Title);

            // Set the result
            jobService.SetJobResult(jobId, metadata);
        }
        catch (Exception e)
        {
            logger.Error(e, "Error extracting metadata from {type} '{value}'.", type, value);
            jobService.SetJobError(jobId, $"Internal error: {e.Message}");
        }
    }
    #endregion
}
