using Microsoft.Net.Http.Headers;
using System.Security.Claims;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Models.ProjectDocuments;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("projects/{projectId}/documents")]
[Authorize]
public class ProjectDocumentsController(
    ProjectDocumentService projectDocumentService,
    ProjectDocumentTypeService projectDocumentTypeService,
    ProjectService projectService,
    IStorageService storageService,
    IAuthorizationService authorizationService,
    EnvironmentConfig environmentConfig
) : AppControllerBase
{
    private const int MAX_TITLE_LENGTH = 300;
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    [SwaggerOperation(Summary = "List the project documents the current user can view")]
    [SwaggerResponse(200, "Document list")]
    [SwaggerResponse(404, "Not a root project")]
    public async Task<IActionResult> List(Guid projectId)
    {
        if (!await IsRootProjectAsync(projectId)) return NotFound();
        return Ok(await projectDocumentService.ListAsync(projectId, UserId, await IsOwnerAsync()));
    }

    [HttpGet("{documentId}")]
    [SwaggerOperation(Summary = "Get a document with its version and access list")]
    [SwaggerResponse(200, "Document detail")]
    [SwaggerResponse(404, "Not found or no access")]
    public async Task<IActionResult> Get(Guid projectId, Guid documentId)
    {
        var (document, level) = await LoadAsync(projectId, documentId);
        if (document == null || level == null) return NotFound();

        return Ok(await projectDocumentService.GetDetailAsync(document, level.Value)); 
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a document from a finished project document upload")]
    [SwaggerResponse(200, "Document created", typeof(Guid))]
    [SwaggerResponse(400, "Invalid input")]
    [SwaggerResponse(403, "Not a project member")]
    [SwaggerResponse(404, "Not a root project")]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] ProjectDocumentCreateDto dto)
    {
        if (!await IsRootProjectAsync(projectId)) return NotFound();

        if (!await projectDocumentService.IsMemberAsync(projectId, UserId) && !await IsOwnerAsync())
            return Problem("Only project members can add documents.", statusCode: 403);

        if (ValidateTitle(dto.Title) is string titleError) return Problem(titleError, statusCode: 400);
        if (await ValidateUploadAsync(dto.ObjectName, dto.FileName) is string uploadError) return Problem(uploadError, statusCode: 400);
        if (!await projectDocumentTypeService.ExistsAsync(dto.TypeId)) return Problem("Document type not found.", statusCode: 400);
        if (await ValidateAccessAsync(dto.Visibility, dto.GeneralAccessType, dto.Grants) is string accessError) return Problem(accessError, statusCode: 400);

        Guid documentId = await projectDocumentService.CreateAsync(projectId, dto, UserId);
        return Ok(documentId);
    }

    [HttpPatch("{documentId}")]
    [SwaggerOperation(Summary = "Rename a document or change its type")]
    [SwaggerResponse(204, "Updated")]
    [SwaggerResponse(400, "Invalid input")]
    [SwaggerResponse(403, "View-only access")]
    [SwaggerResponse(404, "Not found or no access")]
    public async Task<IActionResult> Update(Guid projectId, Guid documentId, [FromBody] ProjectDocumentUpdateDto dto)
    {
        var (document, level) = await LoadAsync(projectId, documentId);
        if (EditCheck(document, level) is IActionResult denied) return denied;

        if (dto.Title != null && ValidateTitle(dto.Title) is string titleError) return Problem(titleError, statusCode: 400);
        if (dto.TypeId != null && !await projectDocumentTypeService.ExistsAsync(dto.TypeId.Value))
            return Problem("Document type not found.", statusCode: 400);

        await projectDocumentService.UpdateAsync(document!, dto);
        return NoContent();
    }

    [HttpPut("{documentId}/access")]
    [SwaggerOperation(Summary = "Replace a document's general access and personal grants")]
    [SwaggerResponse(204, "Access updated")]
    [SwaggerResponse(400, "Invalid input")]
    [SwaggerResponse(403, "View-only access")]
    [SwaggerResponse(404, "Not found or no access")]
    public async Task<IActionResult> SetAccess(Guid projectId, Guid documentId, [FromBody] ProjectDocumentAccessUpdateDto dto)
    {
        var (document, level) = await LoadAsync(projectId, documentId);
        if (EditCheck(document, level) is IActionResult denied) return denied;

        if (await ValidateAccessAsync(dto.Visibility, dto.GeneralAccessType, dto.Grants) is string accessError)
            return Problem(accessError, statusCode: 400);

        await projectDocumentService.SetAccessAsync(document!, dto);
        return NoContent();
    }

    [HttpPost("{documentId}/versions")]
    [SwaggerOperation(Summary = "Add a new version from a finished project document upload")]
    [SwaggerResponse(200, "Version added", typeof(Guid))]
    [SwaggerResponse(400, "Invalid input")]
    [SwaggerResponse(403, "View-only access")]
    [SwaggerResponse(404, "Not found or no access")]
    [SwaggerResponse(409, "Someone else added a version at the same time")]
    public async Task<IActionResult> AddVersion(Guid projectId, Guid documentId, [FromBody] ProjectDocumentVersionCreateDto dto)
    {
        var (document, level) = await LoadAsync(projectId, documentId);
        if (EditCheck(document, level) is IActionResult denied) return denied;

        if (await ValidateUploadAsync(dto.ObjectName, dto.FileName) is string uploadError) return Problem(uploadError, statusCode: 400);

        Guid? versionId = await projectDocumentService.AddVersionAsync(document!, dto, UserId);
        if (versionId == null)
            return Problem("Someone else added a version at the same time. Please try again.", statusCode: 409);

        return Ok(versionId);
    }

    [HttpDelete("{documentId}")]
    [SwaggerOperation(Summary = "Permanently delete a document and all its versions")]
    [SwaggerResponse(204, "Deleted")]
    [SwaggerResponse(403, "View-only access")]
    [SwaggerResponse(404, "Not found or no access")]
    public async Task<IActionResult> Delete(Guid projectId, Guid documentId)
    {
        var (document, level) = await LoadAsync(projectId, documentId);
        if (EditCheck(document, level) is IActionResult denied) return denied;

        await projectDocumentService.DeleteAsync(document!, UserId);
        return NoContent();
    }

    [HttpGet("{documentId}/versions/{versionId}/download")]
    [SwaggerOperation(Summary = "Download the original file of a version")]
    [SwaggerResponse(200, "File stream")]
    [SwaggerResponse(404, "Not found or no access")]
    public async Task<IActionResult> Download(Guid projectId, Guid documentId, Guid versionId)
    {
        var (document, level) = await LoadAsync(projectId, documentId);
        ProjectDocumentVersion? version = document?.Versions!.FirstOrDefault(v => v.Id == versionId);
        if (version == null || level == null) return NotFound();

        string fileName = $"{FilesController.SanitizeFileName(document!.Title)} v{version.VersionNumber}.{version.Extension}";
        ObjectDownloadResponse response = await storageService.DownloadObjectAsync(bucketName, ProjectDocumentService.StorageKey(version.Id));

        return File(response.Stream, Filetype.GetMimeType(version.Extension), fileName);
    }

    [HttpGet("{documentId}/versions/{versionId}/preview")]
    [SwaggerOperation(Summary = "View a version as PDF in the browser")]
    [SwaggerResponse(200, "PDF stream")]
    [SwaggerResponse(404, "Not found or no access")]
    [SwaggerResponse(409, "Preview not ready yet")]
    public async Task<IActionResult> Preview(Guid projectId, Guid documentId, Guid versionId)
    {
        var (document, level) = await LoadAsync(projectId, documentId);
        ProjectDocumentVersion? version = document?.Versions!.FirstOrDefault(v => v.Id == versionId);
        if (version == null || level == null) return NotFound();

        // A PDF is its own preview; Word files get a converted PDF from the processing pipeline
        bool isPdf = version.Extension == "pdf";
        if (!isPdf && version.PreviewStatus != EmbeddingStatus.Completed)
            return Problem("The preview is not ready yet.", statusCode: 409);

        string key = isPdf ? ProjectDocumentService.StorageKey(version.Id) : ProjectDocumentService.PreviewKey(version.Id);
        ObjectDownloadResponse response = await storageService.DownloadObjectAsync(bucketName, key);

        string fileName = $"{FilesController.SanitizeFileName(document!.Title)} v{version.VersionNumber}.pdf";
        
        // SetHttpFileName escapes quotes and adds a UTF-8 filename*, raw non-ASCII in header makes Kestrel throw
        ContentDispositionHeaderValue disposition = new("inline");
        disposition.SetHttpFileName(fileName);
        Response.Headers.ContentDisposition = disposition.ToString();
        return File(response.Stream, "application/pdf");
    }

    #region Helpers

    private async Task<bool> IsOwnerAsync()
        => (await authorizationService.AuthorizeAsync(User, "RequireOwner")).Succeeded;

    // Documents belong to root projects only; folders share their root's documents
    private async Task<bool> IsRootProjectAsync(Guid projectId)
        => await projectService.ExistsAsync(p => p.Id == projectId && p.ProjectType == "root");

    private async Task<(ProjectDocument? Document, ProjectDocumentAccessType? Level)> LoadAsync(Guid projectId, Guid documentId)
    {
        ProjectDocument? document = await projectDocumentService.GetAsync(projectId, documentId);
        if (document == null) return (null, null);

        bool isMember = await projectDocumentService.IsMemberAsync(projectId, UserId);
        return (document, ProjectDocumentService.GetAccessLevel(document, UserId, isMember, await IsOwnerAsync()));
    }

    // 404 without any access, so the response doesn't reveal that the document exists; 403 for view-only access
    private IActionResult? EditCheck(ProjectDocument? document, ProjectDocumentAccessType? level)
    {
        if (document == null || level == null) return NotFound();
        if (level != ProjectDocumentAccessType.Editor) return Problem("You only have view access to this document.", statusCode: 403);
        return null;
    }

    private static string? ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Title is required.";
        if (title.Trim().Length > MAX_TITLE_LENGTH) return $"Title too long (max {MAX_TITLE_LENGTH} characters).";
        return null;
    }

    private async Task<string?> ValidateUploadAsync(string objectname, string fileName)
    {
        string extension = Filetype.TrimExtension(Path.GetExtension(fileName)).ToLowerInvariant();
        
        if (!ProjectDocumentService.SupportedExtensions.Contains(extension))
            return "Project documents must be PDF or Word (.docx).";

        if (!Guid.TryParse(objectname, out Guid objectId) || !await projectDocumentService.IsUploadAvailableAsync(objectId))
            return "Upload not found. Please upload the file again";

        return null;
    }

    private async Task<string?> ValidateAccessAsync(ProjectDocumentVisibility visibility, ProjectDocumentAccessType generalAccessType, ProjectDocumentGrantDto[] grants)
    {
        // The JSON enum converter also accepts numbers, so out-of-range values can get through binding
        if (!Enum.IsDefined(visibility) || !Enum.IsDefined(generalAccessType) || grants.Any(g => !Enum.IsDefined(g.AccessType)))
            return "Invalid access setting.";

        if (grants.Select(g => g.UserId).Distinct().Count() != grants.Length)
            return "Each person can only be added once.";

        if (!await projectDocumentService.AllUsersActiveAsync(grants.Select(g => g.UserId)))
            return "One or more people could not be found.";

        return null;
    }

    #endregion
}