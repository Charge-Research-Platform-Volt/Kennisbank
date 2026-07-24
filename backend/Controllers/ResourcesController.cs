using System.Linq.Expressions;
using System.Security.Claims;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Background;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("[controller]")]
[Authorize]
public class ResourcesController(ResourceService resourceService, ChunkSearchIndexService chunkSearchIndexService, IBackgroundTaskQueue taskQueue, IServiceScopeFactory serviceScopeFactory, EnvironmentConfig environmentConfig) : AppControllerBase
{
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    [HttpGet]
    [SwaggerOperation(Summary = "Get resources with optional search and pagination")]
    [SwaggerResponse(200, "List of resources")]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool trash = false)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var (items, totalCount) = await resourceService.SearchAsync(search, page, pageSize, trash);
            return Ok(new { items, totalCount });
        }

        Expression<Func<Resource, bool>> predicate = r => r.Trashed == trash;
        var (pagedItems, pagedTotalCount) = await resourceService.GetPageAsync(page, pageSize, predicate);

        return Ok(new { items = pagedItems, totalCount = pagedTotalCount });
    }

    [HttpPut]
    [SwaggerOperation(Summary = "Create a resource")]
    [SwaggerResponse(200, "Resource created", typeof(Guid))]
    public async Task<IActionResult> Create([FromBody] ResourceCreateDto dto)
    {
        Guid createdBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Guid id = await resourceService.CreateAsync(dto, createdBy);

        taskQueue.QueueBackgroundWorkItem(async token =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();

            if (!string.IsNullOrEmpty(dto.FileExtension))
            {
                var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
                var dlResponse = await storageService.DownloadObjectAsync(bucketName, id.ToString());
                using var memStream = new MemoryStream();
                await dlResponse.Stream.CopyToAsync(memStream, token);
                memStream.Position = 0;
                await ingestionService.RunResourcePipelineAsync(id, dto.FileExtension, memStream);
            }
            else
            {
                await ingestionService.RunResourcePipelineAsync(id);
            }
        });

        return Ok(id);
    }

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Get resource detail")]
    [SwaggerResponse(200, "Resource detail")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> Get(Guid id)
        => OkOrNotFound(await resourceService.GetDetailAsync(id));

    [HttpPatch("{id}")]
    [SwaggerOperation(Summary = "Update a resource")]
    [SwaggerResponse(204, "Updated")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ResourceUpdateDto dto)
    {
        bool found = await resourceService.UpdateAsync(id, r =>
        {
            if (dto.Title != null) r.Title = dto.Title;
            if (dto.Description != null) r.Description = dto.Description;
            if (dto.Abstract != null) r.Abstract = dto.Abstract;
            if (dto.TypeId != null) r.TypeId = Guid.TryParse(dto.TypeId, out var typeId) ? typeId : null;
            if (dto.LanguageCode != null) r.LanguageCode = dto.LanguageCode;
            if (dto.PublicationCode != null) r.PublicationCode = dto.PublicationCode;
            if (dto.PublicationDate.HasValue) r.PublicationDate = dto.PublicationDate;
            if (dto.JournalId != null) r.JournalId = Guid.TryParse(dto.JournalId, out var journalId) ? journalId : null;
            if (dto.PublicationDatePrecision.HasValue) r.PublicationDatePrecision = dto.PublicationDatePrecision;
            if (dto.License != null) r.License = dto.License;
            if (dto.Note != null) r.Note = dto.Note;
            if (dto.SourceUrl != null) r.SourceUrl = dto.SourceUrl;
        });

        if (found)
        {
            taskQueue.QueueBackgroundWorkItem(async token =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
                await ingestionService.UpdateResourceMetadataAsync(id);
            });
        }

        return NoContentOrNotFound(found);
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Permanently delete a resource")]
    [SwaggerResponse(204, "Deleted")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> Delete(Guid id)
        => NoContentOrNotFound(await resourceService.DeleteAsync(id));

    [HttpGet("{id}/similar")]
    [SwaggerOperation(Summary = "Get similar resources based on vector embeddings")]
    [SwaggerResponse(200, "Similar resources")]
    public async Task<IActionResult> Similar(Guid id, [FromQuery] int limit = 5)
    {
        var results = await chunkSearchIndexService.RecommendSimilarAsync(id, limit, 0.65f);
        var resourceIds = results.Select(r => r.ResourceId).ToArray();
        var resources = await resourceService.GetByIdsAsync(resourceIds);
        return Ok(resources.Select(r => new { r.Id, r.Title, r.FileType }));
    }

    [HttpPatch("{id}/trash")]
    [SwaggerOperation(Summary = "Move resource to trash")]
    [SwaggerResponse(204, "Trashed")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> Trash(Guid id)
        => NoContentOrNotFound(await resourceService.TrashAsync(id));

    [HttpPatch("{id}/untrash")]
    [SwaggerOperation(Summary = "Restore resource from trash")]
    [SwaggerResponse(204, "Restored")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> Untrash(Guid id)
        => NoContentOrNotFound(await resourceService.UntrashAsync(id));

    [HttpGet("exists")]
    [SwaggerOperation(Summary = "Check if resource exists by hash or URL")]
    [SwaggerResponse(200, "Exists result")]
    public async Task<IActionResult> Exists([FromQuery] string? hash, [FromQuery] string? url)
    {
        if (!string.IsNullOrEmpty(hash))
            return Ok(await resourceService.FindIdByHashAsync(hash));

        if (!string.IsNullOrEmpty(url))
            return Ok(await resourceService.FindIdByUrlAsync(url));

        return Problem("Provide hash or url.", statusCode: 400);
    }

    [HttpGet("{id}/file-type")]
    [SwaggerOperation(Summary = "Get file type of a resource")]
    [SwaggerResponse(200, "File type string")]
    [SwaggerResponse(404, "Not found")]
    public async Task<IActionResult> GetFileType(Guid id)
        => OkOrNotFound(await resourceService.GetFileTypeAsync(id));

    [HttpPost("{id}/authors/{authorId}")]
    [SwaggerOperation(Summary = "Add author to resource")]
    [SwaggerResponse(204, "Added")]
    [SwaggerResponse(404, "Resource not found")]
    public async Task<IActionResult> AddAuthor(Guid id, Guid authorId)
    {
        if (!await resourceService.ExistsAsync(id))
            return Problem("Resource not found.", statusCode: 404);
        await resourceService.AddAuthorAsync(id, authorId);
        return NoContent();
    }

    [HttpDelete("{id}/authors/{authorId}")]
    [SwaggerOperation(Summary = "Remove author from resource")]
    [SwaggerResponse(204, "Removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveAuthor(Guid id, Guid authorId)
        => NoContentOrNotFound(await resourceService.RemoveAuthorAsync(id, authorId));

    [HttpPost("{id}/organisations/{orgId}")]
    [SwaggerOperation(Summary = "Add organisation to resource")]
    [SwaggerResponse(204, "Added")]
    [SwaggerResponse(404, "Resource not found")]
    public async Task<IActionResult> AddOrganisation(Guid id, Guid orgId, [FromQuery] string? role)
    {
        if (!await resourceService.ExistsAsync(id))
            return Problem("Resource not found.", statusCode: 404);
        await resourceService.AddOrganisationAsync(id, orgId, role);
        return NoContent();
    }

    [HttpDelete("{id}/organisations/{orgId}")]
    [SwaggerOperation(Summary = "Remove organisation from resource")]
    [SwaggerResponse(204, "Removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveOrganisation(Guid id, Guid orgId)
        => NoContentOrNotFound(await resourceService.RemoveOrganisationAsync(id, orgId));

    [HttpPatch("{id}/organisations/{orgId}/role")]
    [SwaggerOperation(Summary = "Update organisation role on resource")]
    [SwaggerResponse(204, "Updated")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> UpdateOrganisationRole(Guid id, Guid orgId, [FromQuery] string newRole)
        => NoContentOrNotFound(await resourceService.UpdateOrganisationRoleAsync(id, orgId, newRole));

    [HttpPost("{id}/regions/{regionId}")]
    [SwaggerOperation(Summary = "Add region to resource")]
    [SwaggerResponse(204, "Added")]
    [SwaggerResponse(404, "Resource not found")]
    public async Task<IActionResult> AddRegion(Guid id, Guid regionId)
    {
        if (!await resourceService.ExistsAsync(id))
            return Problem("Resource not found.", statusCode: 404);
        await resourceService.AddRegionAsync(id, regionId);
        return NoContent();
    }

    [HttpDelete("{id}/regions/{regionId}")]
    [SwaggerOperation(Summary = "Remove region from resource")]
    [SwaggerResponse(204, "Removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveRegion(Guid id, Guid regionId)
        => NoContentOrNotFound(await resourceService.RemoveRegionAsync(id, regionId));

    [HttpPost("{id}/related-persons/{personId}")]
    [SwaggerOperation(Summary = "Add related person to resource")]
    [SwaggerResponse(204, "Added")]
    [SwaggerResponse(404, "Resource not found")]
    public async Task<IActionResult> AddRelatedPerson(Guid id, Guid personId, [FromQuery] string? role)
    {
        if (!await resourceService.ExistsAsync(id))
            return Problem("Resource not found.", statusCode: 404);
        await resourceService.AddRelatedPersonAsync(id, personId, role);
        return NoContent();
    }

    [HttpDelete("{id}/related-persons/{personId}")]
    [SwaggerOperation(Summary = "Remove related person from resource")]
    [SwaggerResponse(204, "Removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveRelatedPerson(Guid id, Guid personId)
        => NoContentOrNotFound(await resourceService.RemoveRelatedPersonAsync(id, personId));

    [HttpPatch("{id}/related-persons/{personId}/role")]
    [SwaggerOperation(Summary = "Update related person role on resource")]
    [SwaggerResponse(204, "Updated")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> UpdateRelatedPersonRole(Guid id, Guid personId, [FromQuery] string newRole)
        => NoContentOrNotFound(await resourceService.UpdateRelatedPersonRoleAsync(id, personId, newRole));

    [HttpPost("{id}/tags/{tagId}")]
    [SwaggerOperation(Summary = "Add tag to resource")]
    [SwaggerResponse(204, "Added")]
    [SwaggerResponse(404, "Resource not found")]
    public async Task<IActionResult> AddTag(Guid id, Guid tagId)
    {
        if (!await resourceService.ExistsAsync(id))
            return Problem("Resource not found.", statusCode: 404);
        await resourceService.AddTagAsync(id, tagId);
        return NoContent();
    }

    [HttpDelete("{id}/tags/{tagId}")]
    [SwaggerOperation(Summary = "Remove tag from resource")]
    [SwaggerResponse(204, "Removed")]
    [SwaggerResponse(404, "Relation not found")]
    public async Task<IActionResult> RemoveTag(Guid id, Guid tagId)
        => NoContentOrNotFound(await resourceService.RemoveTagAsync(id, tagId));
}