using KnowledgeBank.Models;
using KnowledgeBank.Services.Background;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Utils;
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
    EnvironmentConfig environmentConfig) : AppControllerBase
{
    private readonly Serilog.ILogger logger = Log.ForContext<AdminController>();
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    [HttpPost("reembed")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Re-embed all resources, persons, and organisations")]
    [SwaggerResponse(202, "Queued")]
    public IActionResult ReEmbed()
    {
        taskQueue.QueueBackgroundWorkItem(async token =>
        {
            Resource[] resources;
            Person[] persons;
            Organisation[] organisations;

            using (var scope = serviceScopeFactory.CreateScope())
            {
                resources = await scope.ServiceProvider.GetRequiredService<ResourceService>().GetAllAsync();
                persons = await scope.ServiceProvider.GetRequiredService<PersonService>().GetAllAsync();
                organisations = await scope.ServiceProvider.GetRequiredService<OrganisationService>().GetAllAsync();
            }

            logger.Information("Re-embedding {Count} resources", resources.Length);
            await RunBoundedAsync(resources, 3, async resource =>
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
            });

            logger.Information("Re-embedding {Count} persons", persons.Length);
            await RunBoundedAsync(persons, 5, async person =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();

                try { await ingestionService.RunPersonEntityPipelineAsync(person.Id); }
                catch (Exception ex) { logger.Error(ex, "Failed to re-embed person {Id}", person.Id); }
            });

            logger.Information("Re-embedding {Count} organisations", organisations.Length);
            await RunBoundedAsync(organisations, 5, async organisation =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();

                try { await ingestionService.RunOrganisationEntityPipelineAsync(organisation.Id); }
                catch (Exception ex) { logger.Error(ex, "Failed to re-embed organisation {Id}", organisation.Id); }
            });

            logger.Information("Re-embedding complete");
        });

        return Accepted();
    }

    private static async Task RunBoundedAsync<T>(IEnumerable<T> items, int maxConcurrency, Func<T, Task> action)
    {
        using SemaphoreSlim semaphore = new(maxConcurrency);

        IEnumerable<Task> tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync();
            try { await action(item); }
            finally { semaphore.Release(); }
        });

        await Task.WhenAll(tasks);
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

            foreach (var resource in resources)
            {
                try { await searchIndexService.SyncResourceAsync(resource.Id); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex resource {Id}", resource.Id); }
            }

            var persons = await personService.GetAllAsync();
            logger.Information("Reindexing {Count} persons", persons.Length);

            foreach (var person in persons)
            {
                try { await searchIndexService.SyncPersonAsync(person.Id); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex person {Id}", person.Id); }
            }

            var organisations = await organisationService.GetAllAsync();
            logger.Information("Reindexing {Count} organisations", organisations.Length);

            foreach (var organisation in organisations)
            {
                try { await searchIndexService.SyncOrganisationAsync(organisation.Id); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex organisation {Id}", organisation.Id); }
            }

            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var chunkSearchIndexService = scope.ServiceProvider.GetRequiredService<ChunkSearchIndexService>();

            var resourceChunks = await db.ResourceChunks.Where(c => c.Embedding != null).ToListAsync();
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

            foreach (var c in resourceChunks.Where(c => c.ChunkType == ChunkType.MetaData))
                await searchIndexService.UpdateVectorAsync(c.ResourceId, c.Embedding!.ToArray());

            foreach (var c in entityChunks.Where(c => c.ChunkType == ChunkType.MetaData))
                await searchIndexService.UpdateVectorAsync(c.EntityId, c.Embedding!.ToArray());

            var taxonomySearchIndexService = scope.ServiceProvider.GetRequiredService<TaxonomySearchIndexService>();
            var tagService = scope.ServiceProvider.GetRequiredService<TagService>();
            var regionService = scope.ServiceProvider.GetRequiredService<RegionService>();
            var resourceTypeService = scope.ServiceProvider.GetRequiredService<ResourceTypeService>();
            var journalService = scope.ServiceProvider.GetRequiredService<JournalService>();

            var tags = await tagService.GetAllAsync();
            logger.Information("Reindexing {Count} tags", tags.Length);
            foreach (var tag in tags)
            {
                try { await taxonomySearchIndexService.SyncAsync(tag.Id, tag.Name, TagService.TypeTag); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex tag {Id}", tag.Id); }
            }

            var regions = await regionService.GetAllAsync();
            logger.Information("Reindexing {Count} regions", regions.Length);
            foreach (var region in regions)
            {
                try { await taxonomySearchIndexService.SyncAsync(region.Id, region.Name, RegionService.TypeTag); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex region {Id}", region.Id); }
            }

            var resourceTypes = await resourceTypeService.GetAllAsync();
            logger.Information("Reindexing {Count} resource types", resourceTypes.Length);
            foreach (var resourceType in resourceTypes)
            {
                try { await taxonomySearchIndexService.SyncAsync(resourceType.Id, resourceType.Name, ResourceTypeService.TypeTag); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex resource type {Id}", resourceType.Id); }
            }

            var journals = await journalService.GetAllAsync();
            logger.Information("Reindexing {Count} journals", journals.Length);
            foreach (var journal in journals)
            {
                try { await taxonomySearchIndexService.SyncAsync(journal.Id, journal.Name, JournalService.TypeTag); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex journal {Id}", journal.Id); }
            }

            var projects = await db.Projects.ToListAsync();
            logger.Information("Reindexing {Count} projects", projects.Count);
            foreach (var project in projects)
            {
                try { await taxonomySearchIndexService.SyncAsync(project.Id, project.Title, ProjectService.TypeTag); }
                catch (Exception ex) { logger.Error(ex, "Failed to reindex project {Id}", project.Id); }
            }

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
