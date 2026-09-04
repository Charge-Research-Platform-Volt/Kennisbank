using System.Linq.Expressions;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Background;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Services.Vector;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class ResourceService(DatabaseContext db, TagService tagService, PersonService personService, OrganisationService organisationService, RegionService regionService, IServiceScopeFactory scopeFactory, LibrarySearchIndexService librarySearchIndexService, EmbeddingService embeddingService, IVectorStore vectorStore, IBackgroundTaskQueue taskQueue)
{
    public const string TypeTag = "resource";

    #region Queries

    public async Task<Resource?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        IQueryable<Resource> query = db.Resources;

        if (includeRelations)
            query = query.Include(r => r.ResourceType)
                         .Include(r => r.Journal)
                         .Include(r => r.ResourceAuthorRelations!).ThenInclude(x => x.Author)
                         .Include(r => r.ResourceOrganisationRelations!).ThenInclude(x => x.Organisation)
                         .Include(r => r.ResourceRegionRelations!).ThenInclude(x => x.Region)
                         .Include(r => r.ResourceRelatedPersonRelations!).ThenInclude(x => x.Person)
                         .Include(r => r.ResourceTagRelations!).ThenInclude(x => x.Tag);

        return await query.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Resource[]> GetAllAsync(Expression<Func<Resource, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<Resource> query = db.Resources;

        if (includeRelations)
            query = query.Include(r => r.ResourceType)
                         .Include(r => r.Journal)
                         .Include(r => r.ResourceAuthorRelations!).ThenInclude(x => x.Author)
                         .Include(r => r.ResourceOrganisationRelations!).ThenInclude(x => x.Organisation)
                         .Include(r => r.ResourceRegionRelations!).ThenInclude(x => x.Region)
                         .Include(r => r.ResourceRelatedPersonRelations!).ThenInclude(x => x.Person)
                         .Include(r => r.ResourceTagRelations!).ThenInclude(x => x.Tag);

        if (predicate != null)
            query = query.Where(predicate);

        return await query.OrderBy(r => r.Title).ToArrayAsync();
    }

    public async Task<Resource[]> GetByIdsAsync(IEnumerable<Guid> ids)
        => await db.Resources.Where(r => ids.Contains(r.Id)).ToArrayAsync();

    public async Task<(Resource[] Items, int TotalCount)> GetPageAsync(int page, int pageSize, Expression<Func<Resource, bool>>? predicate, bool includeRelations = false)
    {
        IQueryable<Resource> query = db.Resources;

        if (includeRelations)
            query = query.Include(r => r.ResourceType)
                         .Include(r => r.Journal)
                         .Include(r => r.ResourceAuthorRelations!).ThenInclude(x => x.Author)
                         .Include(r => r.ResourceOrganisationRelations!).ThenInclude(x => x.Organisation)
                         .Include(r => r.ResourceRegionRelations!).ThenInclude(x => x.Region)
                         .Include(r => r.ResourceRelatedPersonRelations!).ThenInclude(x => x.Person)
                         .Include(r => r.ResourceTagRelations!).ThenInclude(x => x.Tag);

        if (predicate != null)
            query = query.Where(predicate);

        int totalCount = await query.CountAsync();

        Resource[] items = await query
            .OrderBy(r => r.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        return (items, totalCount);
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await db.Resources.AnyAsync(r => r.Id == id);

    public async Task<bool> ExistsAsync(Expression<Func<Resource, bool>> predicate)
        => await db.Resources.AnyAsync(predicate);

    public async Task<Guid?> FindIdByTitleAsync(string title)
        => await db.Resources.Where(r => r.Title == title).Select(r => (Guid?)r.Id).FirstOrDefaultAsync();

    public async Task<(Resource[] Items, int TotalCount)> SearchAsync(string query, int page, int pageSize, bool trash)
    {
        if (!trash)
        {
            float[] queryEmbedding = await embeddingService.GenerateEmbedding(query);
            var (ids, totalCount) = await librarySearchIndexService.SearchAsync(query, new LibraryFilterOptions { TypeFilter = [TypeTag] }, page, pageSize, queryEmbedding);
            Resource[] resources = await GetAllAsync(predicate: r => ids.Contains(r.Id));
            Dictionary<Guid, Resource> lookup = resources.ToDictionary(r => r.Id);
            Resource[] items = ids.Where(lookup.ContainsKey).Select(id => lookup[id]).ToArray();
            return (items, totalCount);
        }

        Expression<Func<Resource, bool>> predicate = r =>
            (EF.Functions.TrigramsAreSimilar(r.Title, query) ||
             EF.Functions.ILike(r.Title, $"%{query}%")) && r.Trashed == trash;

        return await GetPageAsync(page, pageSize, predicate);
    }

    public async Task<Guid?> FindIdByHashAsync(string hash)
        => await db.Resources.Where(r => r.Hash == hash).Select(r => (Guid?)r.Id).FirstOrDefaultAsync();

    public async Task<Guid?> FindIdByUrlAsync(string url)
        => await db.Resources.Where(r => r.SourceUrl == url).Select(r => (Guid?)r.Id).FirstOrDefaultAsync();

    public async Task<string?> GetFileTypeAsync(Guid id)
        => await db.Resources.Where(r => r.Id == id).Select(r => r.FileType).FirstOrDefaultAsync();

    public async Task<ResourceDetailDto?> GetDetailAsync(Guid id)
        => await db.Resources
            .Where(r => r.Id == id)
            .Select(r => new ResourceDetailDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Abstract = r.Abstract,
                TypeId = r.TypeId,
                TypeName = r.ResourceType != null ? r.ResourceType.Name : null,
                LanguageCode = r.LanguageCode,
                PublicationCode = r.PublicationCode,
                PublicationDate = r.PublicationDate,
                PublicationDatePrecision = r.PublicationDatePrecision,
                JournalId = r.JournalId,
                JournalName = r.Journal != null ? r.Journal.Name : null,
                CreatedOn = r.CreatedOn,
                CreatedBy = r.CreatedBy,
                License = r.License,
                Note = r.Note,
                FileType = r.FileType,
                FileExt = r.FileExt,
                Hash = r.Hash,
                SourceUrl = r.SourceUrl,
                Trashed = r.Trashed,
                TrashDate = r.TrashDate,
                EmbeddingStatus = r.EmbeddingStatus,
                Authors = r.ResourceAuthorRelations!
                    .Select(a => new RelationItemDto { Id = a.AuthorId, Name = a.Author!.Name, FileType = a.Author.EntityType })
                    .ToArray(),
                Organisations = r.ResourceOrganisationRelations!
                    .Select(o => new RelationItemDto { Id = o.OrganisationId, Name = o.Organisation!.Name, Role = o.Role })
                    .ToArray(),
                Regions = r.ResourceRegionRelations!
                    .Select(rg => new RelationItemDto { Id = rg.RegionId, Name = rg.Region!.Name })
                    .ToArray(),
                RelatedPersons = r.ResourceRelatedPersonRelations!
                    .Select(p => new RelationItemDto { Id = p.PersonId, Name = p.Person!.Name, Role = p.Role })
                    .ToArray(),
                Tags = r.ResourceTagRelations!
                    .Select(t => new RelationItemDto { Id = t.TagId, Name = t.Tag!.Name })
                    .ToArray(),
            })
            .FirstOrDefaultAsync();

    #endregion

    #region Commands

    public async Task<Guid> CreateAsync(ResourceCreateDto dto, Guid createdBy)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        List<(Guid Id, string Type)> entitiesToEmbed = [];

        bool isFile = !string.IsNullOrEmpty(dto.FileExtension);

        Guid resourceId = isFile && Guid.TryParse(dto.FileId, out Guid fileId)
            ? fileId
            : Guid.NewGuid();

        Resource resource = new()
        {
            Id = resourceId,
            Title = dto.Title,
            Description = dto.Description,
            Abstract = dto.Abstract,
            TypeId = Guid.TryParse(dto.TypeId, out Guid typeId) ? typeId : null,
            LanguageCode = dto.LanguageCode,
            PublicationCode = dto.PublicationCode,
            PublicationDate = dto.PublicationDate,
            JournalId = Guid.TryParse(dto.JournalId, out Guid journalId) ? journalId : null,
            PublicationDatePrecision = dto.PublicationDatePrecision,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = createdBy,
            License = dto.License,
            Note = dto.Note,
            SourceUrl = dto.SourceUrl,
            FileType = isFile ? Filetype.ConvertExtensionToFiletype(dto.FileExtension!) : "website",
            FileExt = isFile ? Filetype.TrimExtension(dto.FileExtension!) : null,
            Hash = dto.Hash,
        };

        db.Resources.Add(resource);

        foreach (string tag in dto.Tags.Distinct())
        {
            Guid tagId = Guid.TryParse(tag, out Guid parsed)
                ? parsed
                : await tagService.FindIdByNameAsync(tag) ?? await tagService.CreateAsync(tag, createdBy);
            db.ResourceTagRelations.Add(new ResourceTagRelation { ResourceId = resourceId, TagId = tagId });
        }

        foreach (var author in dto.Authors.DistinctBy(a => a.Value))
        {
            bool isExisting = Guid.TryParse(author.Value, out Guid parsedAuthor);
            Guid authorId = isExisting ? parsedAuthor
                : author.Type?.ToLower() == "organisation"
                    ? await organisationService.FindIdByNameAsync(author.Value) ?? await organisationService.CreateAsync(new OrganisationCreateDto { Name = author.Value, Website = author.Website, EmailAddress = author.Email }, createdBy)
                    : await personService.FindIdByNameAsync(author.Value) ?? await personService.CreateAsync(new PersonCreateDto { Name = author.Value, Occupation = author.Occupation, EmailAddress = author.Email }, createdBy);
            db.ResourceAuthorRelations.Add(new ResourceAuthorRelation { ResourceId = resourceId, AuthorId = authorId });
            entitiesToEmbed.Add((authorId, author.Type?.ToLower() == "organisation" ? "organisation" : "person"));

            if (author.SuggestedAliases.Count > 0)
                foreach (string alias in author.SuggestedAliases)
                    await TryAppendAliasAsync(authorId, alias, author.Type?.ToLower() == "organisation" ? "organisation" : "person", entitiesToEmbed);
            if (isExisting)
                await TryPatchEntityMetadataAsync(authorId, author.Type?.ToLower() == "organisation" ? "organisation" : "person", author.Occupation, author.Website, author.Email);
        }

        foreach (var org in dto.Organisations.DistinctBy(o => o.Id))
        {
            bool isExisting = Guid.TryParse(org.Id, out Guid parsedOrg);
            Guid orgId = isExisting ? parsedOrg
                : await organisationService.FindIdByNameAsync(org.Id) ?? await organisationService.CreateAsync(new OrganisationCreateDto { Name = org.Id, Website = org.Website, EmailAddress = org.Email }, createdBy);
            db.ResourceOrganisationRelations.Add(new ResourceOrganisationRelation { ResourceId = resourceId, OrganisationId = orgId, Role = org.Relation });
            entitiesToEmbed.Add((orgId, "organisation"));

            if (org.SuggestedAliases.Count > 0)
                foreach (string alias in org.SuggestedAliases)
                    await TryAppendAliasAsync(orgId, alias, "organisation", entitiesToEmbed);
            if (isExisting)
                await TryPatchEntityMetadataAsync(orgId, "organisation", null, org.Website, org.Email);
        }

        foreach (string region in dto.Regions.Distinct())
        {
            Guid regionId = Guid.TryParse(region, out Guid parsedRegion) ? parsedRegion
                : await regionService.FindIdByNameAsync(region) ?? await regionService.CreateAsync(region, createdBy);
            db.ResourceRegionRelations.Add(new ResourceRegionRelation { ResourceId = resourceId, RegionId = regionId });
        }

        foreach (var person in dto.RelatedPersons.DistinctBy(p => p.Id))
        {
            bool isExisting = Guid.TryParse(person.Id, out Guid parsedPerson);
            Guid personId = isExisting ? parsedPerson
                : await personService.FindIdByNameAsync(person.Id) ?? await personService.CreateAsync(new PersonCreateDto { Name = person.Id, Occupation = person.Occupation, EmailAddress = person.Email }, createdBy);
            db.ResourceRelatedPersonRelations.Add(new ResourceRelatedPersonRelation { ResourceId = resourceId, PersonId = personId, Role = person.Relation });
            entitiesToEmbed.Add((personId, "person"));

            if (person.SuggestedAliases.Count > 0)
                foreach (string alias in person.SuggestedAliases)
                    await TryAppendAliasAsync(personId, alias, "person", entitiesToEmbed);
            if (isExisting)
                await TryPatchEntityMetadataAsync(personId, "person", person.Occupation, null, person.Email);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await librarySearchIndexService.SyncResourceAsync(resourceId);

        if (entitiesToEmbed.Count > 0)
        {
            List<(Guid Id, string Type)> toEmbed = entitiesToEmbed.DistinctBy(e => e.Id).ToList();

            taskQueue.QueueBackgroundWorkItem(async token =>
            {
                using var scope = scopeFactory.CreateScope();
                var ingestion = scope.ServiceProvider.GetRequiredService<IngestionService>();
                foreach (var (entityId, entityType) in toEmbed)
                {
                    if (entityType == "organisation")
                        await ingestion.RunOrganisationEntityPipelineAsync(entityId);
                    else
                        await ingestion.RunPersonEntityPipelineAsync(entityId);
                }
            });
        }

        return resourceId;
    }

    private async Task TryAppendAliasAsync(Guid entityId, string alias, string type, List<(Guid Id, string Type)> collected)
    {
        var entity = await db.Entities.FindAsync(entityId);
        if (entity == null || entity.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase)) return;
        entity.Aliases.Add(alias);
        collected.Add((entityId, type));
    }

    private async Task TryPatchEntityMetadataAsync(Guid entityId, string type, string? occupation, string? website, string? email)
    {
        if (type == "organisation")
        {
            var org = await db.Organisations.FindAsync(entityId);
            if (org == null) return;
            if (!string.IsNullOrWhiteSpace(website) && string.IsNullOrWhiteSpace(org.Website)) org.Website = website;
            if (!string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(org.EmailAddress)) org.EmailAddress = email;
        }
        else
        {
            var person = await db.Persons.FindAsync(entityId);
            if (person == null) return;
            if (!string.IsNullOrWhiteSpace(occupation) && string.IsNullOrWhiteSpace(person.Occupation)) person.Occupation = occupation;
            if (!string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(person.EmailAddress)) person.EmailAddress = email;
        }
    }

    public async Task<bool> UpdateAsync(Guid id, Action<Resource> update)
    {
        Resource? resource = await db.Resources.FindAsync(id);
        if (resource == null) return false;

        update(resource);
        await db.SaveChangesAsync();
        await librarySearchIndexService.SyncResourceAsync(id);
        return true;
    }

    public async Task<bool> TrashAsync(Guid id)
    {
        Resource? resource = await db.Resources.FindAsync(id);
        if (resource == null) return false;

        resource.Trashed = true;
        resource.TrashDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await librarySearchIndexService.SyncResourceAsync(id);
        return true;
    }

    public async Task<bool> UntrashAsync(Guid id)
    {
        Resource? resource = await db.Resources.FindAsync(id);
        if (resource == null) return false;

        resource.Trashed = false;
        resource.TrashDate = null;
        await db.SaveChangesAsync();
        await librarySearchIndexService.SyncResourceAsync(id);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Resource? resource = await db.Resources.FindAsync(id);
        if (resource == null) return false;

        db.Resources.Remove(resource);
        await db.SaveChangesAsync();
        await librarySearchIndexService.SyncResourceAsync(id);
        await vectorStore.DeletePointsByResourceIdAsync(id);
        return true;
    }

    #endregion

    #region Relation Commands

    public async Task AddAuthorAsync(Guid resourceId, Guid authorId)
    {
        db.ResourceAuthorRelations.Add(new ResourceAuthorRelation { ResourceId = resourceId, AuthorId = authorId });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveAuthorAsync(Guid resourceId, Guid authorId)
    {
        ResourceAuthorRelation? rel = await db.ResourceAuthorRelations.FindAsync(resourceId, authorId);
        if (rel == null) return false;
        db.ResourceAuthorRelations.Remove(rel);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task AddOrganisationAsync(Guid resourceId, Guid organisationId, string? role)
    {
        db.ResourceOrganisationRelations.Add(new ResourceOrganisationRelation { ResourceId = resourceId, OrganisationId = organisationId, Role = role });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveOrganisationAsync(Guid resourceId, Guid organisationId)
    {
        ResourceOrganisationRelation? rel = await db.ResourceOrganisationRelations.FindAsync(resourceId, organisationId);
        if (rel == null) return false;
        db.ResourceOrganisationRelations.Remove(rel);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateOrganisationRoleAsync(Guid resourceId, Guid organisationId, string newRole)
    {
        ResourceOrganisationRelation? rel = await db.ResourceOrganisationRelations.FindAsync(resourceId, organisationId);
        if (rel == null) return false;
        rel.Role = newRole;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task AddRegionAsync(Guid resourceId, Guid regionId)
    {
        db.ResourceRegionRelations.Add(new ResourceRegionRelation { ResourceId = resourceId, RegionId = regionId });
        await db.SaveChangesAsync();
        await librarySearchIndexService.SyncResourceAsync(resourceId);
    }

    public async Task<bool> RemoveRegionAsync(Guid resourceId, Guid regionId)
    {
        ResourceRegionRelation? rel = await db.ResourceRegionRelations.FindAsync(resourceId, regionId);
        if (rel == null) return false;
        db.ResourceRegionRelations.Remove(rel);
        await db.SaveChangesAsync();
        await librarySearchIndexService.SyncResourceAsync(resourceId);
        return true;
    }

    public async Task AddRelatedPersonAsync(Guid resourceId, Guid personId, string? role)
    {
        db.ResourceRelatedPersonRelations.Add(new ResourceRelatedPersonRelation { ResourceId = resourceId, PersonId = personId, Role = role });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveRelatedPersonAsync(Guid resourceId, Guid personId)
    {
        ResourceRelatedPersonRelation? rel = await db.ResourceRelatedPersonRelations.FindAsync(resourceId, personId);
        if (rel == null) return false;
        db.ResourceRelatedPersonRelations.Remove(rel);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRelatedPersonRoleAsync(Guid resourceId, Guid personId, string newRole)
    {
        ResourceRelatedPersonRelation? rel = await db.ResourceRelatedPersonRelations.FindAsync(resourceId, personId);
        if (rel == null) return false;
        rel.Role = newRole;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task AddTagAsync(Guid resourceId, Guid tagId)
    {
        db.ResourceTagRelations.Add(new ResourceTagRelation { ResourceId = resourceId, TagId = tagId });
        await db.SaveChangesAsync();
        await librarySearchIndexService.SyncResourceAsync(resourceId);
    }

    public async Task<bool> RemoveTagAsync(Guid resourceId, Guid tagId)
    {
        ResourceTagRelation? rel = await db.ResourceTagRelations.FindAsync(resourceId, tagId);
        if (rel == null) return false;
        db.ResourceTagRelations.Remove(rel);
        await db.SaveChangesAsync();
        await librarySearchIndexService.SyncResourceAsync(resourceId);
        return true;
    }

    #endregion
}