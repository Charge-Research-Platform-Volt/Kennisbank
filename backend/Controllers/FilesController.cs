using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using System.Text;
using KnowledgeBank.Services.Storage;

namespace KnowledgeBank.Controllers
{
    /// <summary>
    /// Controller for handling file upload operations to Azure Blob Storage.
    /// Uses chunked upload strategy for all files regardless of size.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class FilesController(IStorageService storageService, ResourceManager resourceManager, EnvironmentConfig environmentConfig) : ControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<FilesController>();
        private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

        #region Upload Init
        /// <summary>
        /// Initializes a file upload session and returns a GUID for the upload.
        /// This endpoint must be called before uploading chunks.
        /// </summary>
        /// <param name="dto">File initialization information</param>
        /// <returns>Upload session information including GUID</returns>
        [HttpPost("upload/init")]
        [SwaggerOperation(Summary = "Initialize a file upload session")]
        [SwaggerResponse(200, "Upload session initialized", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> UploadInit([FromBody] FileUploadInitDto dto)
        {
            // Validation
            if (string.IsNullOrEmpty(dto.FileName))
                return BadRequest(new ApiResponse(false, "File name is required."));

            if (dto.FileSize <= 0)
                return BadRequest(new ApiResponse(false, "Invalid file size."));

            try
            {
                // Generate a unique GUID for this upload
                Guid uploadGuid = Guid.NewGuid();

                // Extract file information
                string extension = Path.GetExtension(dto.FileName);

                if (string.IsNullOrEmpty(extension))
                    return BadRequest(new ApiResponse(false, "File must have an extension."));

                // Check if the filetype is supported
                if (!Filetype.Supported(extension))
                    return BadRequest(new ApiResponse(false, $"File type '{extension}' is not supported."));

                Dictionary<string, string> metadata = new()
                {
                    { "extension", extension },
                    { "originalFileName", Uri.EscapeDataString(Path.GetFileNameWithoutExtension(dto.FileName)) }
                };

                string uploadId = await storageService.InitiateMultipartUploadAsync(bucketName, uploadGuid.ToString(), metadata);
                
                logger.Information("Upload session initialized for file '{FileName}' with GUID {Guid}", dto.FileName, uploadGuid);

                // Return upload session information
                return Ok(new ApiResponse(true, "Upload session initialized successfully.", new
                {
                    objectName = uploadGuid,
                    uploadId,
                    fileName = dto.FileName,
                    fileSize = dto.FileSize,
                    extension
                }));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error initializing upload session for file '{FileName}'", dto.FileName);
                return StatusCode(500, new ApiResponse(false, "Error initializing upload session.", e.Message));
            }
        }
        #endregion

        #region Upload Chunk
        /// <summary>
        /// Uploads a single chunk of a file. Can be called multiple times for large files,
        /// or once for small files. Each chunk is staged with a unique block ID.
        /// </summary>
        /// <param name="objectName">Name of object being uploaded</param>
        /// <param name="uploadId">The ID of the current upload</param>
        /// <param name="partNumber">The chunk number that is uploaded</param>
        [HttpPost("upload/part/{objectName}/{uploadId}/{partNumber}")]
        [SwaggerOperation(Summary = "Upload a chunk of a file")]
        [SwaggerResponse(200, "Chunk uploaded successfully", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> UploadChunk(string objectName, string uploadId, int partNumber)
        {
            // Validation
            if (Request.Body == null)
                return BadRequest(new ApiResponse(false, "No chunk data was provided."));

            if (string.IsNullOrEmpty(objectName) || !ValidityUtil.IsValidId(objectName))
                return BadRequest(new ApiResponse(false, "Invalid objectName."));

            if (string.IsNullOrEmpty(uploadId))
                return BadRequest(new ApiResponse(false, "Upload ID is required."));

            if (partNumber == 0)
                return BadRequest(new ApiResponse(false, "Part number cannot be 0, count starts at 1"));

            try
            {
                // Read request body into memory stream
                using MemoryStream memoryStream = new MemoryStream();
                await Request.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                string ETag = await storageService.UploadPartAsync(bucketName, objectName, uploadId, partNumber, memoryStream);

                logger.Information("Part {PartNumber} uploaded for object {ObjectName} ({Size} bytes)", partNumber, objectName, memoryStream.Length);

                return Ok(new ApiResponse(true, "Chunk uploaded successfully.", new { eTag = ETag }));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error uploading part {PartNumber} for object {ObjectName}", partNumber, objectName);
                return StatusCode(500, new ApiResponse(false, "Error uploading chunk.", e.Message));
            }
        }
        #endregion

        #region Upload Finalize
        /// <summary>
        /// Finalizes a file upload by committing all uploaded chunks in order.
        /// This creates the final blob from all staged chunks.
        /// </summary>
        /// <param name="dto">Finalization information including block IDs</param>
        [HttpPost("upload/finalize")]
        [SwaggerOperation(Summary = "Finalize a file upload")]
        [SwaggerResponse(200, "File upload finalized successfully", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> UploadFinalize([FromBody] FileUploadFinalizeDto dto)
        {
            // Validation
            if (string.IsNullOrEmpty(dto.ObjectName) || !ValidityUtil.IsValidId(dto.ObjectName))
                return BadRequest(new ApiResponse(false, "Invalid upload object name."));

            if (string.IsNullOrEmpty(dto.UploadId))
                return BadRequest(new ApiResponse(false, "File name is required."));

            if (dto.PartETags == null || dto.PartETags.Count == 0)
                return BadRequest(new ApiResponse(false, "No ETags were provided."));

            try
            {
                logger.Information("Finalizing file upload for object {ObjectName} with {PartCount} parts", dto.ObjectName, dto.PartETags.Count);

                await storageService.CompleteMultipartUploadAsync(bucketName, dto.ObjectName, dto.UploadId, dto.PartETags);

                return Ok(new ApiResponse(true, "File upload successful.", new { objectName = dto.ObjectName }));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error finalizing file upload for object {ObjectName}", dto.ObjectName);
                return StatusCode(500, new ApiResponse(false, "Error finalizing upload.", e.Message));
            }
        }
        #endregion

        #region Upload Cancel
        /// <summary>
        /// Cancels an upload by deleting all staged chunks.
        /// </summary>
        [HttpDelete("upload/cancel/{objectName}/{uploadId}")]
        [SwaggerOperation(Summary = "Cancel an upload session")]
        [SwaggerResponse(200, "Upload cancelled successfully", typeof(ApiResponse))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> UploadCancel(string objectName, string uploadId)
        {
            // Validation
            if (string.IsNullOrEmpty(objectName) || !ValidityUtil.IsValidId(objectName))
                return BadRequest(new ApiResponse(false, "Invalid upload object name"));

            if (string.IsNullOrEmpty(uploadId))
                return BadRequest(new ApiResponse(false, "Invalid upload ID"));

            try
            {
                logger.Information("Cancelling upload for object {ObjectName}", objectName);

                await storageService.AbortMultipartUploadAsync(bucketName, objectName, uploadId);

                return Ok(new ApiResponse(true, "Successfully aborted upload."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error cancelling upload for object {ObjectName}", objectName);
                return StatusCode(500, new ApiResponse(false, "Error cancelling upload.", e.Message));
            }
        }
        #endregion
        
        #region Download
        /// <summary>
        /// Downloads a file by its GUID. Uses the resource title from database as the filename.
        /// </summary>
        /// <param name="id">The GUID of the file (resource ID)</param>
        [HttpGet("download/{id}")]
        [SwaggerOperation(Summary = "Download a file from storage")]
        [SwaggerResponse(200, "The requested file")]
        [SwaggerResponse(404, "File not found", typeof(ApiResponse))]
        [SwaggerResponse(400, "Invalid ID", typeof(ApiResponse))]
        [SwaggerResponse(500, "Internal Server Error", typeof(ApiResponse))]
        public async Task<IActionResult> Download(string id)
        {
            // Check if ID is valid
            if (!ValidityUtil.IsValidId(id))
                return BadRequest(new ApiResponse(false, "Invalid ID"));

            try
            {
                logger.Information("Downloading file with ID: {ID}", id);

                ObjectDownloadResponse response = await storageService.DownloadObjectAsync(bucketName, id);
                await using var stream = response.Stream;

                // Get the extension from metadata
                string extension = response.Metadata["extension"];

                // Get the title from the database to use as filename
                // This provides a better user experience than using originalFileName
                string? title = await resourceManager.GetResourcePropertyOrDefaultAsync(id, "Title");
                if (string.IsNullOrEmpty(title))
                {
                    // Fallback to original filename if resource not found in database
                    title = response.Metadata.TryGetValue("originalFileName", out string? origName)
                        ? Uri.UnescapeDataString(Path.GetFileNameWithoutExtension(origName))
                        : "download";
                }
                string fileName = $"{SanitizeFileName(title)}{extension}";

                // Set the content type
                string contentType = "application/octet-stream";
                if (!string.IsNullOrEmpty(extension))
                {
                    FileExtensionContentTypeProvider provider = new();
                    if (provider.TryGetContentType(fileName, out string? type) && !string.IsNullOrEmpty(type))
                        contentType = type;
                }

                logger.Information("Streaming file with ID '{ID}' to client.", id);

                // Determine whether to inline (open in browser) or download
                const long maxFileSize = 500 * 1024 * 1024; // 500 MB max for inline
                bool canBeOpened = (
                    contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
                    contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
                    contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                    contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase));

                string contentDisposition;
                if (canBeOpened && response.ContentLength <= maxFileSize)
                    contentDisposition = $"inline; filename=\"{fileName}\"";
                else
                    contentDisposition = $"attachment; filename=\"{fileName}\"";

                Response.ContentType = contentType;
                Response.Headers.Append("Content-Disposition", contentDisposition);

                // Stream the file directly to the response body (no buffering)
                await stream.CopyToAsync(Response.Body);

                logger.Information("File with ID '{ID}' downloaded successfully.", id);
                return new EmptyResult();
            }
            catch (Exception e)
            {
                logger.Error(e, "Error downloading file with ID {ID}.", id);
                return StatusCode(500, new ApiResponse(false, "Error downloading file", e.Message));
            }
        }

        // Helper method to sanitize filenames
        private static string SanitizeFileName(string fileName, bool preserveSpaces = true)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            // Get invalid chars
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder sb = new();

            // Define problematic characters to replace
            char[] problematicChars = { '\u2018', '\u2019', '\u201C', '\u201D', '\u2014', '\u2013', '\u2026' };

            foreach (char c in fileName)
            {
                // If space and preserve spaces is on, append
                if (c == ' ' && preserveSpaces)
                    sb.Append(c);
                // If space and preserve spaces is not on, put underscore
                else if (c == ' ' && !preserveSpaces)
                    sb.Append('_');
                // If char is valid, append
                else if (!invalidChars.Contains(c) && !problematicChars.Contains(c))
                    sb.Append(c);
                // If char is invalid, put underscore
                else
                    sb.Append('_');
            }

            // Build string and trim
            string result = sb.ToString().Trim();

            // Remove the '.' at start
            if (result.StartsWith('.'))
                result = "_" + result.TrimStart('.');

            // If resulting string is empty put in unnamed name
            if (string.IsNullOrEmpty(result))
                return "unnamed";

            // Limit length of name
            const int maxLength = 255;
            if (result.Length > maxLength)
                result = result[..maxLength];

            return result;
        }
        #endregion
    }
}
