using KnowledgeBank.Services.Domain;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Utils;
using KnowledgeBank.Data;
using Microsoft.AspNetCore.StaticFiles;
using System.Text;

namespace KnowledgeBank.Controllers;

[Route("[controller]")]
[Authorize]
public class FilesController(IStorageService storageService, ResourceService resourceService, EnvironmentConfig environmentConfig) : AppControllerBase
{
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    [HttpPost("upload/init")]
    [SwaggerOperation(Summary = "Initialize a multipart upload")]
    [SwaggerResponse(200, "Upload initialized")]
    [SwaggerResponse(400, "Bad request")]
    public async Task<IActionResult> UploadInit([FromBody] FileUploadInitDto dto)
    {
        string extension = Path.GetExtension(dto.FileName);

        if (string.IsNullOrEmpty(extension))
            return Problem("File must have an extension.", statusCode: 400);

        if (!Filetype.Supported(extension))
            return Problem($"File type '{extension}' not supported.", statusCode: 400);

        string objectName = Guid.NewGuid().ToString();

        var metadata = new Dictionary<string, string>
        {
            { "extension", extension },
            { "originalFileName", Uri.EscapeDataString(Path.GetFileNameWithoutExtension(dto.FileName)) }
        };

        string uploadId = await storageService.InitiateMultipartUploadAsync(bucketName, objectName, metadata);

        return Ok(new { objectName, uploadId });
    }

    [HttpPost("upload/part/{objectName}/{uploadId}/{partNumber}")]
    [SwaggerOperation(Summary = "Upload a chunk of a multipart upload")]
    [SwaggerResponse(200, "ETag of uploaded part")]
    [SwaggerResponse(400, "Bad request")]
    public async Task<IActionResult> UploadChunk(string objectName, string uploadId, int partNumber)
    {
        if (partNumber < 1)
            return Problem("Part number must be >= 1.", statusCode: 400);

        using MemoryStream memoryStream = new();
        await Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        string eTag = await storageService.UploadPartAsync(bucketName, objectName, uploadId, partNumber, memoryStream);

        return Ok(new { eTag });
    }

    [HttpPost("upload/finalize")]
    [SwaggerOperation(Summary = "Finalize a multipart upload")]
    [SwaggerResponse(204, "Upload finalized")]
    [SwaggerResponse(400, "Bad request")]
    public async Task<IActionResult> UploadFinalize([FromBody] FileUploadFinalizeDto dto)
    {
        if (dto.PartETags == null || dto.PartETags.Count == 0)
            return Problem("No ETags provided.", statusCode: 400);

        await storageService.CompleteMultipartUploadAsync(bucketName, dto.ObjectName, dto.UploadId, dto.PartETags);

        return NoContent();
    }

    [HttpDelete("upload/cancel/{objectName}/{uploadId}")]
    [SwaggerOperation(Summary = "Cancel a multipart upload")]
    [SwaggerResponse(204, "Upload cancelled")]
    public async Task<IActionResult> UploadCancel(string objectName, string uploadId)
    {
        await storageService.AbortMultipartUploadAsync(bucketName, objectName, uploadId);
        return NoContent();
    }

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Download a file")]
    [SwaggerResponse(200, "File stream")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> Download(Guid id)
    {
        ObjectDownloadResponse response = await storageService.DownloadObjectAsync(bucketName, id.ToString());
        await using var stream = response.Stream;

        string extension = response.Metadata["extension"];

        Resource? resource = await resourceService.GetByIdAsync(id);
        string title = !string.IsNullOrEmpty(resource?.Title)
            ? resource.Title
            : response.Metadata.TryGetValue("originalFileName", out string? origName)
                ? Uri.UnescapeDataString(origName)
                : "download";

        string fileName = $"{SanitizeFileName(title)}{extension}";

        string contentType = "application/octet-stream";
        if (!string.IsNullOrEmpty(extension))
        {
            FileExtensionContentTypeProvider provider = new();
            if (provider.TryGetContentType(fileName, out string? type))
                contentType = type;
        }

        const long maxInlineSize = 500 * 1024 * 1024;
        bool canInline = contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
                        contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
                        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                        contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

        string disposition = canInline && response.ContentLength <= maxInlineSize
            ? $"inline; filename=\"{fileName}\""
            : $"attachment; filename=\"{fileName}\"";

        Response.ContentType = contentType;
        Response.Headers.Append("Content-Disposition", disposition);
        await stream.CopyToAsync(Response.Body);

        return new EmptyResult();
    }

    private static string SanitizeFileName(string fileName, bool preserveSpaces = true)
    {
        if (string.IsNullOrEmpty(fileName))
            return "unnamed";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        char[] problematicChars = ['\u2018', '\u2019', '\u201C', '\u201D', '\u2014', '\u2013', '\u2026'];
        StringBuilder sb = new();

        foreach (char c in fileName)
        {
            if (c == ' ' && preserveSpaces) sb.Append(c);
            else if (c == ' ') sb.Append('_');
            else if (!invalidChars.Contains(c) && !problematicChars.Contains(c)) sb.Append(c);
            else sb.Append('_');
        }

        string result = sb.ToString().Trim();

        if (result.StartsWith('.'))
            result = "_" + result.TrimStart('.');

        if (string.IsNullOrEmpty(result))
            return "unnamed";

        return result.Length > 255 ? result[..255] : result;
    }
}