using System.Linq.Expressions;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class ResourceService(DatabaseContext db)
{
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

        foreach (Guid tagId in dto.Tags.Select(Guid.Parse).Distinct())
            db.ResourceTagRelations.Add(new ResourceTagRelation { ResourceId = resourceId, TagId = tagId });

        foreach (Guid authorId in dto.Authors.Select(a => Guid.Parse(a.Value)).Distinct())
            db.ResourceAuthorRelations.Add(new ResourceAuthorRelation { ResourceId = resourceId, AuthorId = authorId });

        foreach (var org in dto.Organisations.DistinctBy(o => o.Id))
            db.ResourceOrganisationRelations.Add(new ResourceOrganisationRelation { ResourceId = resourceId, OrganisationId = Guid.Parse(org.Id), Role = org.Relation });

        foreach (Guid regionId in dto.Regions.Select(Guid.Parse).Distinct())
            db.ResourceRegionRelations.Add(new ResourceRegionRelation { ResourceId = resourceId, RegionId = regionId });

        foreach (var person in dto.RelatedPersons.DistinctBy(p => p.Id))
            db.ResourceRelatedPersonRelations.Add(new ResourceRelatedPersonRelation { ResourceId = resourceId, PersonId = Guid.Parse(person.Id), Role = person.Relation });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return resourceId;
    }

    public async Task<bool> UpdateAsync(Guid id, Action<Resource> update)
    {
        Resource? resource = await db.Resources.FindAsync(id);
        if (resource == null) return false;

        update(resource);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TrashAsync(Guid id)
    {
        Resource? resource = await db.Resources.FindAsync(id);
        if (resource == null) return false;

        resource.Trashed = true;
        resource.TrashDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UntrashAsync(Guid id)
    {
        Resource? resource = await db.Resources.FindAsync(id);
        if (resource == null) return false;

        resource.Trashed = false;
        resource.TrashDate = null;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Resource? resource = await db.Resources.FindAsync(id);
        if (resource == null) return false;

        db.Resources.Remove(resource);
        await db.SaveChangesAsync();
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
    }

    public async Task<bool> RemoveRegionAsync(Guid resourceId, Guid regionId)
    {
        ResourceRegionRelation? rel = await db.ResourceRegionRelations.FindAsync(resourceId, regionId);
        if (rel == null) return false;
        db.ResourceRegionRelations.Remove(rel);
        await db.SaveChangesAsync();
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
    }

    public async Task<bool> RemoveTagAsync(Guid resourceId, Guid tagId)
    {
        ResourceTagRelation? rel = await db.ResourceTagRelations.FindAsync(resourceId, tagId);
        if (rel == null) return false;
        db.ResourceTagRelations.Remove(rel);
        await db.SaveChangesAsync();
        return true;
    }

    #endregion
}