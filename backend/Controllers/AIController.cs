using KnowledgeBank.Services;
using KnowledgeBank.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
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
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

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
    public async Task<IActionResult> RenameChat(string chatId, [FromBody] string title)
    {
        if (!ValidityUtil.IsValidId(chatId))
            return BadRequest(new ApiResponse(false, "Invalid chat ID."));

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new ApiResponse(false, "Title cannot be empty."));

        try
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new ApiResponse(false, "User ID not found."));

            var chat = await resourceManager.GetChatAsync(c => c.Id == Guid.Parse(chatId) && c.UserId == Guid.Parse(userId));
            if (chat == null)
                return NotFound(new ApiResponse(false, "Chat not found or you are not authorized."));

            await resourceManager.UpdateChatAsync(Guid.Parse(chatId), c => c.Title, title.Trim());
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
    [HttpDelete("delete-chat/{chatId}")]
    public async Task<IActionResult> DeleteChat(string chatId)
    {
        if (!ValidityUtil.IsValidId(chatId))
            return BadRequest(new ApiResponse(false, "Invalid chat ID."));

        try
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                logger.Warning("User ID not found in claims.");
                return BadRequest(new ApiResponse(false, "User ID not found."));
            }

            var result = await resourceManager.DeleteChatAsync(Guid.Parse(chatId), Guid.Parse(userId));
            if (result)
            {
                logger.Information("Chat {ChatId} deleted successfully.", chatId);
                return Ok(new ApiResponse(true, "Chat deleted successfully."));
            }
            else
            {
                logger.Warning("Failed to delete chat {ChatId}. Not found or user not authorized.", chatId);
                return BadRequest(new ApiResponse(false, "Chat not found or you are not authorized to delete it."));
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while deleting chat {ChatId}.", chatId);
            return StatusCode(500, new ApiResponse(false, "An error occurred while deleting the chat."));
        }
    }
    #endregion

    #region Get Messages by ID
    [HttpGet("messages/{chatId}")]
    public async Task<IActionResult> GetMessagesByChatId(string chatId)
    {
        if (!ValidityUtil.IsValidId(chatId))
            return BadRequest(new ApiResponse(false, "Invalid chat ID."));

        try
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                logger.Warning("User ID not found in claims.");
                return BadRequest(new ApiResponse(false, "User ID not found."));
            }

            var chat = await resourceManager.GetChatAsync(c => c.Id == Guid.Parse(chatId));
            if (chat == null)
            {
                logger.Warning("Chat {ChatId} not found", chatId);
                return NotFound(new ApiResponse(false, "Chat not found."));
            }

            if (chat.UserId != Guid.Parse(userId))
            {
                logger.Warning("User {UserId} is not authorized to access chat {ChatId}", userId, chatId);
                return Unauthorized(new ApiResponse(false, "You are not authorized to access this chat."));
            }

            var messages = await resourceManager.GetMessagesByChatIdAsync(Guid.Parse(chatId));
            return Ok(new ApiResponse(true, "Messages retrieved successfully", new { Messages = messages ?? [] }));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while retrieving messages for chat {ChatId}", chatId);
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
        if (type != "file" && type != "web")
            return BadRequest(new ApiResponse(false, "Invalid extraction type"));

        if ((type == "file" && !ValidityUtil.IsValidId(value)) || (type == "web" && !ValidityUtil.IsValidUrl(value)))
            return BadRequest(new ApiResponse(false, "Invalid " + (type == "file" ? "ID" : "URL")));

        var jobService = HttpContext.RequestServices.GetRequiredService<MetadataExtractionJobService>();
        var job = jobService.CreateJob(type, value);

        logger.Information("Created metadata extraction job {JobId} for {Type}: {Value}", job.JobId, type, value);

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
        using var scope = serviceScopeFactory.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<MetadataExtractionJobService>();
        var storageServiceScoped = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var textExtractionServiceScoped = scope.ServiceProvider.GetRequiredService<TextExtractionService>();
        var metadataExtractionService = scope.ServiceProvider.GetRequiredService<MetadataExtractionService>();

        try
        {
            jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Starting extraction...", 10);
            ExtractedMetadata? metadata = null;

            if (type == "file")
            {
                logger.Information("Metadata extraction requested for file with ID '{id}'", value);
                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Retrieving file...", 20);

                ObjectDownloadResponse response = await storageServiceScoped.DownloadObjectAsync(bucketName, value);
                string extension = response.Metadata["extension"];
                string fileName = Uri.UnescapeDataString(response.Metadata["originalFileName"]);

                if (!Filetype.SupportedText(extension))
                {
                    jobService.SetJobError(jobId, "This filetype is not supported for metadata extraction");
                    return;
                }

                logger.Information("Extracting text from {FileType} document", extension);
                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Extracting text from document...", 30);

                // S3 stream is not seekable, copy to MemoryStream first
                using Stream blobStream = response.Stream;
                using MemoryStream memoryStream = new();
                await blobStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                string extractedText = await textExtractionServiceScoped.ExtractTextFromFileAsync(memoryStream, extension);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    jobService.SetJobError(jobId, "No text could be extracted from the document");
                    return;
                }

                logger.Information("Text extracted successfully. Length: {TextLength} characters", extractedText.Length);
                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Analyzing with AI...", 50);

                metadata = await metadataExtractionService.ExtractMetadataFromFileAsync(extractedText, fileName, () =>
                {
                    jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Finding similar entities...", 70);
                });
            }
            else if (Filetype.IsDocumentUrl(value))
            {
                logger.Information("Metadata extraction requested for document URL '{url}'", value);
                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Downloading document...", 20);

                using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
                using HttpResponseMessage httpResponse = await httpClient.GetAsync(value);
                httpResponse.EnsureSuccessStatusCode();

                string extension = Filetype.GetDocumentUrlExtension(value);
                string fileName = Path.GetFileNameWithoutExtension(new Uri(value).LocalPath);

                if (!Filetype.SupportedText(extension))
                {
                    jobService.SetJobError(jobId, "This filetype is not supported for metadata extraction");
                    return;
                }

                using Stream downloadStream = await httpResponse.Content.ReadAsStreamAsync();
                using MemoryStream memoryStream = new();
                await downloadStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Extracting text from document...", 30);
                string extractedText = await textExtractionServiceScoped.ExtractTextFromFileAsync(memoryStream, extension);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    jobService.SetJobError(jobId, "No text could be extracted from the document");
                    return;
                }

                logger.Information("Text extracted successfully. Length: {TextLength} characters", extractedText.Length);
                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Analyzing with AI...", 50);

                metadata = await metadataExtractionService.ExtractMetadataFromFileAsync(extractedText, fileName, () =>
                {
                    jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Finding similar entities...", 70);
                });
            }
            else
            {
                logger.Information("Metadata extraction requested for web with URL '{url}'", value);
                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Fetching webpage...", 20);

                ReadabilityResult readabilityResult = await textExtractionServiceScoped.ExtractTextFromWebAsync(value);

                if (string.IsNullOrWhiteSpace(readabilityResult.TextContent))
                {
                    jobService.SetJobError(jobId, "No text could be extracted from the webpage");
                    return;
                }

                if (readabilityResult.TextContent.Length < 200)
                {
                    jobService.SetJobError(jobId, "The webpage returned too little content. It may be behind a login or access restriction. Try downloading the file directly and uploading it instead.");
                    return;
                }

                logger.Information("Text extracted successfully. Length: {TextLength} characters", readabilityResult.TextContent.Length);
                jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Analyzing with AI...", 50);

                metadata = await metadataExtractionService.ExtractMetadataFromWebAsync(readabilityResult, value, () =>
                {
                    jobService.UpdateJobStatus(jobId, JobStatus.Processing, "Finding similar entities...", 70);
                });
            }

            if (metadata == null)
            {
                jobService.SetJobError(jobId, "Failed to extract metadata");
                return;
            }

            logger.Information("Metadata extraction completed successfully for {Title}", metadata.Title);
            jobService.SetJobResult(jobId, metadata);
        }
        catch (InvalidOperationException e)
        {
            logger.Warning(e, "Metadata extraction failed for {Type} '{Value}'.", type, value);
            jobService.SetJobError(jobId, e.Message);
        }
        catch (Exception e)
        {
            logger.Error(e, "Error extracting metadata from {Type} '{Value}'.", type, value);
            jobService.SetJobError(jobId, $"Internal error: {e.Message}");
        }
    }
    #endregion
}
