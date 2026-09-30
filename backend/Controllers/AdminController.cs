using KnowledgeBank.Models;
using KnowledgeBank.Services.Background;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Utils;
using static KnowledgeBank.Utils.ConcurrencyUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Controllers;

[Route("admin")]
[Authorize]
public class AdminController(
    IBackgroundTaskQueue taskQueue,
    IServiceScopeFactory serviceScopeFactory,
    EnvironmentConfig environmentConfig,
    ReembedRunState reembedRunState) : AppControllerBase
{
    private readonly Serilog.ILogger logger = Log.ForContext<AdminController>();
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    [HttpPost("reembed")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Re-embed resources, persons, and organisations (optionally scoped to only resources/entities, and/or only those not currently Completed)")]
    [SwaggerResponse(202, "Queued")]
    [SwaggerResponse(409, "A re-embed is already running")]
    public IActionResult ReEmbed([FromQuery] bool onlyIncomplete = false, [FromQuery] bool includeResources = true, [FromQuery] bool includeEntities = true)
    {
        if (!reembedRunState.TryStart(out CancellationToken runToken))
            return Conflict("A re-embed is already running.");

        taskQueue.QueueBackgroundWorkItem(async hostToken =>
        {
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(hostToken, runToken);
            CancellationToken token = linkedCts.Token;

            try
            {
                Resource[] resources = [];
                Person[] persons = [];
                Organisation[] organisations = [];

                using (var scope = serviceScopeFactory.CreateScope())
                {
                    if (includeResources)
                        resources = await scope.ServiceProvider.GetRequiredService<ResourceService>().GetAllAsync(r => !r.Trashed);

                    if (includeEntities)
                    {
                        persons = await scope.ServiceProvider.GetRequiredService<PersonService>().GetAllAsync(p => !p.Trashed);
                        organisations = await scope.ServiceProvider.GetRequiredService<OrganisationService>().GetAllAsync(o => !o.Trashed);
                    }
                }

                if (onlyIncomplete)
                {
                    // Covers Pending (never started), Processing (stuck mid-pipeline), and Failed —
                    // not just Failed, since a resource/entity can end up stuck at Pending if something
                    // throws before its pipeline even starts.
                    resources = resources.Where(r => r.EmbeddingStatus != EmbeddingStatus.Completed).ToArray();
                    persons = persons.Where(p => p.EmbeddingStatus != EmbeddingStatus.Completed).ToArray();
                    organisations = organisations.Where(o => o.EmbeddingStatus != EmbeddingStatus.Completed).ToArray();
                }

                // Reset everything selected for this run to Pending up front, rather than leaving stale
                // Completed/Failed statuses sitting there until each item's turn comes up in the bounded
                // concurrency loops below — otherwise the status dashboard can't tell "queued for this run"
                // apart from "untouched since before".
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                    Guid[] resourceIds = [.. resources.Select(r => r.Id)];
                    Guid[] entityIds = [.. persons.Select(p => p.Id), .. organisations.Select(o => o.Id)];

                    await db.Resources.Where(r => resourceIds.Contains(r.Id))
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(r => r.EmbeddingStatus, EmbeddingStatus.Pending)
                            .SetProperty(r => r.EmbeddingError, (string?)null)
                            .SetProperty(r => r.EmbeddingFailures, 0));

                    await db.Entities.Where(e => entityIds.Contains(e.Id))
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(e => e.EmbeddingStatus, EmbeddingStatus.Pending)
                            .SetProperty(e => e.EmbeddingError, (string?)null)
                            .SetProperty(e => e.EmbeddingFailures, 0));
                }

                logger.Information("Re-embedding {Count} resources", resources.Length);
                await RunBoundedAsync(resources, 10, async resource =>
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
                    var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

                    try
                    {
                        if (resource.FileType == "document" && resource.FileExt != null)
                        {
                            var dlResponse = await storageService.DownloadObjectAsync(bucketName, resource.Id.ToString());
                            using var memStream = new MemoryStream();
                            await dlResponse.Stream.CopyToAsync(memStream, token);
                            memStream.Position = 0;
                            await ingestionService.RunResourcePipelineAsync(resource.Id, resource.FileExt, memStream);
                        }
                        else
                        {
                            await ingestionService.RunResourcePipelineAsync(resource.Id);
                        }
                    }
                    catch (Exception ex) { logger.Error(ex, "Failed to re-embed resource {Id}", resource.Id); }
                }, token);

                logger.Information("Re-embedding {Count} persons", persons.Length);
                await RunBoundedAsync(persons, 15, async person =>
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();

                    try { await ingestionService.RunPersonEntityPipelineAsync(person.Id); }
                    catch (Exception ex) { logger.Error(ex, "Failed to re-embed person {Id}", person.Id); }
                }, token);

                logger.Information("Re-embedding {Count} organisations", organisations.Length);
                await RunBoundedAsync(organisations, 15, async organisation =>
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();

                    try { await ingestionService.RunOrganisationEntityPipelineAsync(organisation.Id); }
                    catch (Exception ex) { logger.Error(ex, "Failed to re-embed organisation {Id}", organisation.Id); }
                }, token);

                logger.Information(token.IsCancellationRequested ? "Re-embedding cancelled" : "Re-embedding complete");
            }
            finally
            {
                reembedRunState.Complete();
            }
        });

        return Accepted();
    }

    [HttpPost("reembed/cancel")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Cancel the currently running re-embed, if any")]
    [SwaggerResponse(204, "Cancellation requested")]
    [SwaggerResponse(404, "No re-embed is currently running")]
    public IActionResult CancelReEmbed()
    {
        if (!reembedRunState.IsRunning) return NotFound();
        reembedRunState.Cancel();
        return NoContent();
    }

    [HttpGet("reembed/status")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Get embedding status counts and currently-failed items for resources, persons, and organisations")]
    [SwaggerResponse(200, "Status summary")]
    public async Task<IActionResult> ReEmbedStatus()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        Dictionary<string, int> resourceCounts = await db.Resources
            .Where(r => !r.Trashed)
            .GroupBy(r => r.EmbeddingStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status.ToString(), x => x.Count);

        Dictionary<string, int> entityCounts = await db.Entities
            .Where(e => !e.Trashed)
            .GroupBy(e => e.EmbeddingStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status.ToString(), x => x.Count);

        // Independent of EmbeddingStatus entirely — catches drift the status field alone can miss,
        // e.g. historical data from before today's fixes, or any future bug that marks something
        // Completed without actually creating its chunks. Content is resource-only: entities never
        // get a ContentText chunk (RunEntityPipelineAsync always embeds a single MetaData chunk), so
        // checking it for entities would just always read "100%" — not a useful number.
        int resourcesWithoutMetadataEmbeddings = await db.Resources
            .Where(r => !r.Trashed && !db.ResourceChunks.Any(c => c.ResourceId == r.Id && c.ChunkType == ChunkType.MetaData))
            .CountAsync();

        int resourcesWithoutContentEmbeddings = await db.Resources
            .Where(r => !r.Trashed && !db.ResourceChunks.Any(c => c.ResourceId == r.Id && c.ChunkType == ChunkType.ContentText))
            .CountAsync();

        int entitiesWithoutEmbeddings = await db.Entities
            .Where(e => !e.Trashed && !db.EntityChunks.Any(c => c.EntityId == e.Id && c.ChunkType == ChunkType.MetaData))
            .CountAsync();

        var incompleteResources = await db.Resources
            .Where(r => !r.Trashed && r.EmbeddingStatus != EmbeddingStatus.Completed)
            .Select(r => new FailedEmbeddingItemDto { Id = r.Id, Name = r.Title, Type = "resource", Status = r.EmbeddingStatus.ToString(), Error = r.EmbeddingError })
            .ToListAsync();

        var incompletePersons = await db.Persons
            .Where(p => !p.Trashed && p.EmbeddingStatus != EmbeddingStatus.Completed)
            .Select(p => new FailedEmbeddingItemDto { Id = p.Id, Name = p.Name, Type = "person", Status = p.EmbeddingStatus.ToString(), Error = p.EmbeddingError })
            .ToListAsync();

        var incompleteOrganisations = await db.Organisations
            .Where(o => !o.Trashed && o.EmbeddingStatus != EmbeddingStatus.Completed)
            .Select(o => new FailedEmbeddingItemDto { Id = o.Id, Name = o.Name, Type = "organisation", Status = o.EmbeddingStatus.ToString(), Error = o.EmbeddingError })
            .ToListAsync();

        return Ok(new EmbeddingStatusSummaryDto
        {
            IsRunning = reembedRunState.IsRunning,
            ResourceCounts = resourceCounts,
            EntityCounts = entityCounts,
            ResourcesWithoutMetadataEmbeddings = resourcesWithoutMetadataEmbeddings,
            ResourcesWithoutContentEmbeddings = resourcesWithoutContentEmbeddings,
            EntitiesWithoutEmbeddings = entitiesWithoutEmbeddings,
            IncompleteItems = [.. incompleteResources, .. incompletePersons, .. incompleteOrganisations]
        });
    }

    [HttpPost("reindex-search")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Rebuild the Meilisearch index for all resources, persons, and organisations")]
    [SwaggerResponse(202, "Queued")]
    public IActionResult ReindexSearch()
    {
        taskQueue.QueueBackgroundWorkItem(async token =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var searchIndexService = scope.ServiceProvider.GetRequiredService<LibrarySearchIndexService>();
            var resourceService = scope.ServiceProvider.GetRequiredService<ResourceService>();
            var personService = scope.ServiceProvider.GetRequiredService<PersonService>();
            var organisationService = scope.ServiceProvider.GetRequiredService<OrganisationService>();

            var resources = await resourceService.GetAllAsync();
            logger.Information("Reindexing {Count} resources", resources.Length);

            await RunBoundedAsync(resources, 10, async resource =>
            {
                try { await searchIndexService.SyncResourceAsync(resource.Id); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex resource {Id}", resource.Id); }
            }, token);

            var persons = await personService.GetAllAsync();
            logger.Information("Reindexing {Count} persons", persons.Length);

            await RunBoundedAsync(persons, 10, async person =>
            {
                try { await searchIndexService.SyncPersonAsync(person.Id); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex person {Id}", person.Id); }
            }, token);

            var organisations = await organisationService.GetAllAsync();
            logger.Information("Reindexing {Count} organisations", organisations.Length);

            await RunBoundedAsync(organisations, 10, async organisation =>
            {
                try { await searchIndexService.SyncOrganisationAsync(organisation.Id); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex organisation {Id}", organisation.Id); }
            }, token);

            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var chunkSearchIndexService = scope.ServiceProvider.GetRequiredService<ChunkSearchIndexService>();

            var resourceChunks = await db.ResourceChunks.Where(c => c.Embedding != null).ToListAsync(token);
            logger.Information("Reindexing {Count} resource chunks", resourceChunks.Count);
            var resourceChunkDocs = resourceChunks.Select(c => new ChunkSearchDocument
            {
                Id = c.Id.ToString(),
                ParentId = c.ResourceId.ToString(),
                ParentType = "resource",
                ChunkType = c.ChunkType.ToString(),
                ChunkPart = c.ChunkPart,
                ChunkText = c.ChunkText,
                Vectors = new() { ["default"] = c.Embedding!.ToArray() }
            });

            await chunkSearchIndexService.IndexChunksAsync(resourceChunkDocs);

            var entityChunks = await db.EntityChunks.Where(c => c.Embedding != null).ToListAsync();
            logger.Information("Reindexing {Count} entity chunks", entityChunks.Count);
            var personIds = (await db.Persons.Select(p => p.Id).ToListAsync()).ToHashSet();
            var entityChunkDocs = entityChunks.Select(c => new ChunkSearchDocument
            {
                Id = c.Id.ToString(),
                ParentId = c.EntityId.ToString(),
                ParentType = personIds.Contains(c.EntityId) ? "person" : "organisation",
                ChunkType = c.ChunkType.ToString(),
                ChunkPart = c.ChunkPart,
                ChunkText = c.ChunkText,
                Vectors = new() { ["default"] = c.Embedding!.ToArray() }
            });

            await chunkSearchIndexService.IndexChunksAsync(entityChunkDocs);

            await RunBoundedAsync(resourceChunks.Where(c => c.ChunkType == ChunkType.MetaData).ToList(), 10, async c =>
                await searchIndexService.UpdateVectorAsync(c.ResourceId, c.Embedding!.ToArray()), token);

            await RunBoundedAsync(entityChunks.Where(c => c.ChunkType == ChunkType.MetaData).ToList(), 10, async c =>
                await searchIndexService.UpdateVectorAsync(c.EntityId, c.Embedding!.ToArray()), token);

            var taxonomySearchIndexService = scope.ServiceProvider.GetRequiredService<TaxonomySearchIndexService>();
            var tagService = scope.ServiceProvider.GetRequiredService<TagService>();
            var regionService = scope.ServiceProvider.GetRequiredService<RegionService>();
            var resourceTypeService = scope.ServiceProvider.GetRequiredService<ResourceTypeService>();
            var journalService = scope.ServiceProvider.GetRequiredService<JournalService>();

            var tags = await tagService.GetAllAsync();
            logger.Information("Reindexing {Count} tags", tags.Length);
            await RunBoundedAsync(tags, 10, async tag =>
            {
                try { await taxonomySearchIndexService.SyncAsync(tag.Id, tag.Name, TagService.TypeTag); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex tag {Id}", tag.Id); }
            }, token);

            var regions = await regionService.GetAllAsync();
            logger.Information("Reindexing {Count} regions", regions.Length);
            await RunBoundedAsync(regions, 10, async region =>
            {
                try { await taxonomySearchIndexService.SyncAsync(region.Id, region.Name, RegionService.TypeTag); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex region {Id}", region.Id); }
            }, token);

            var resourceTypes = await resourceTypeService.GetAllAsync();
            logger.Information("Reindexing {Count} resource types", resourceTypes.Length);
            await RunBoundedAsync(resourceTypes, 10, async resourceType =>
            {
                try { await taxonomySearchIndexService.SyncAsync(resourceType.Id, resourceType.Name, ResourceTypeService.TypeTag); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex resource type {Id}", resourceType.Id); }
            }, token);

            var journals = await journalService.GetAllAsync();
            logger.Information("Reindexing {Count} journals", journals.Length);
            await RunBoundedAsync(journals, 10, async journal =>
            {
                try { await taxonomySearchIndexService.SyncAsync(journal.Id, journal.Name, JournalService.TypeTag); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex journal {Id}", journal.Id); }
            }, token);

            var projects = await db.Projects.ToListAsync();
            logger.Information("Reindexing {Count} projects", projects.Count);
            await RunBoundedAsync(projects, 10, async project =>
            {
                try { await taxonomySearchIndexService.SyncAsync(project.Id, project.Title, ProjectService.TypeTag); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex project {Id}", project.Id); }
            }, token);

            logger.Information("Search reindex complete");
        });

        return Accepted();
    }

    [HttpPost("cleanup-orphans")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Find and remove orphaned chunks and search index documents whose parent entity no longer exists")]
    [SwaggerResponse(200, "Cleanup summary")]
    public async Task<IActionResult> CleanupOrphans()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var chunkSearchIndexService = scope.ServiceProvider.GetRequiredService<ChunkSearchIndexService>();
        var librarySearchIndexService = scope.ServiceProvider.GetRequiredService<LibrarySearchIndexService>();
        var taxonomySearchIndexService = scope.ServiceProvider.GetRequiredService<TaxonomySearchIndexService>();

        HashSet<Guid> resourceIds = (await db.Resources.Select(r => r.Id).ToListAsync()).ToHashSet();
        HashSet<Guid> personIds = (await db.Persons.Select(p => p.Id).ToListAsync()).ToHashSet();
        HashSet<Guid> orgIds = (await db.Organisations.Select(o => o.Id).ToListAsync()).ToHashSet();
        HashSet<Guid> entityIds = personIds.Concat(orgIds).ToHashSet();

        logger.Information("Cleaning up orphaned resource chunks");
        int orphanedResourceChunks = await db.ResourceChunks.Where(c => !resourceIds.Contains(c.ResourceId)).ExecuteDeleteAsync();

        logger.Information("Cleaning up orphaned entity chunks");
        int orphanedEntityChunks = await db.EntityChunks.Where(c => !entityIds.Contains(c.EntityId)).ExecuteDeleteAsync();

        logger.Information("Cleaning up orphaned chunks index documents");
        int chunksIndexOrphans = await chunkSearchIndexService.DeleteOrphanedAsync(resourceIds, entityIds);

        logger.Information("Cleaning up orphaned library index documents");
        HashSet<Guid> libraryValidIds = resourceIds.Concat(entityIds).ToHashSet();
        int libraryIndexOrphans = await librarySearchIndexService.DeleteOrphanedAsync(libraryValidIds);

        logger.Information("Cleaning up orphaned taxonomy index documents");
        HashSet<Guid> tagIds = (await db.Tags.Select(t => t.Id).ToListAsync()).ToHashSet();
        HashSet<Guid> regionIds = (await db.Regions.Select(r => r.Id).ToListAsync()).ToHashSet();
        HashSet<Guid> resourceTypeIds = (await db.ResourceTypes.Select(rt => rt.Id).ToListAsync()).ToHashSet();
        HashSet<Guid> journalIds = (await db.Journals.Select(j => j.Id).ToListAsync()).ToHashSet();
        HashSet<Guid> projectIds = (await db.Projects.Select(p => p.Id).ToListAsync()).ToHashSet();
        HashSet<Guid> taxonomyValidIds = tagIds.Concat(regionIds).Concat(resourceTypeIds).Concat(journalIds).Concat(projectIds).ToHashSet();
        int taxonomyIndexOrphans = await taxonomySearchIndexService.DeleteOrphanedAsync(taxonomyValidIds);

        logger.Information("Orphan cleanup complete");

        return Ok(new
        {
            orphanedResourceChunks,
            orphanedEntityChunks,
            chunksIndexOrphans,
            libraryIndexOrphans,
            taxonomyIndexOrphans
        });
    }
}
