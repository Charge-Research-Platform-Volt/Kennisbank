using System.Linq.Expressions;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class OrganisationService(DatabaseContext db)
{
    #region Queries

    public async Task<Organisation?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        IQueryable<Organisation> query = db.Organisations;

        if (includeRelations)
            query = query.Include(o => o.TargetRelationships!).ThenInclude(x => x.SourceOrganisation)
                         .Include(o => o.SourceRelationships!).ThenInclude(x => x.TargetOrganisation)
                         .Include(o => o.PersonOrganisationRelations!).ThenInclude(x => x.Person)
                         .Include(o => o.ResourceOrganisationRelations);

        return await query.FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<(Organisation[] Items, int TotalCount)> GetPageAsync(int page, int pageSize, Expression<Func<Organisation, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<Organisation> query = db.Organisations;

        if (includeRelations)
            query = query.Include(o => o.TargetRelationships!).ThenInclude(x => x.SourceOrganisation)
                         .Include(o => o.SourceRelationships!).ThenInclude(x => x.TargetOrganisation)
                         .Include(o => o.PersonOrganisationRelations!).ThenInclude(x => x.Person)
                         .Include(o => o.ResourceOrganisationRelations);

        if (predicate != null)
            query = query.Where(predicate);

        int totalCount = await query.CountAsync();

        Organisation[] items = await query
            .OrderBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        return (items, totalCount);
    }

    public async Task<Organisation[]> GetAllAsync(Expression<Func<Organisation, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<Organisation> query = db.Organisations;

        if (includeRelations)
            query = query.Include(o => o.TargetRelationships!).ThenInclude(x => x.SourceOrganisation)
                         .Include(o => o.SourceRelationships!).ThenInclude(x => x.TargetOrganisation)
                         .Include(o => o.PersonOrganisationRelations!).ThenInclude(x => x.Person)
                         .Include(o => o.ResourceOrganisationRelations);

        if (predicate != null)
            query = query.Where(predicate);

        return await query.OrderBy(o => o.Name).ToArrayAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await db.Organisations.AnyAsync(o => o.Id == id);

    public async Task<bool> ExistsAsync(Expression<Func<Organisation, bool>> predicate)
        => await db.Organisations.AnyAsync(predicate);

    public async Task<int> CountAsync(Expression<Func<Organisation, bool>>? predicate = null)
    {
        IQueryable<Organisation> query = db.Organisations;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public async Task<Guid?> FindIdByNameAsync(string name)
        => await db.Organisations.Where(o => o.Name == name).Select(o => (Guid?)o.Id).FirstOrDefaultAsync();

    public async Task<OrganisationDetailDto?> GetDetailAsync(Guid id)
        => await db.Organisations
            .Where(o => o.Id == id)
            .Select(o => new OrganisationDetailDto
            {
                Name = o.Name,
                Description = o.Description,
                EmailAddress = o.EmailAddress,
                Website = o.Website,
                CreatedOn = o.CreatedOn,
                Authored = o.ResourceAuthorRelations!
                    .Select(r => new RelationItemDto
                    {
                        Id = r.Resource!.Id,
                        Name = r.Resource.Title,
                        FileType = r.Resource.FileType
                    })
                    .ToArray(),
                RelatedResources = o.ResourceOrganisationRelations!
                    .Select(r => new RelationItemDto
                    {
                        Id = r.Resource!.Id,
                        Name = r.Resource.Title,
                        Role = r.Role,
                        FileType = r.Resource.FileType
                    })
                    .ToArray(),
                TargetOrganisations = o.TargetRelationships!
                    .Select(r => new RelationItemDto
                    {
                        Id = r.TargetOrganisation!.Id,
                        Name = r.TargetOrganisation.Name,
                        Relation = r.Relation
                    })
                    .ToArray(),
                SourceOrganisations = o.SourceRelationships!
                    .Select(r => new RelationItemDto
                    {
                        Id = r.SourceOrganisation!.Id,
                        Name = r.SourceOrganisation.Name,
                        Relation = r.Relation
                    })
                    .ToArray(),
                Persons = o.PersonOrganisationRelations!
                    .Select(r => new RelationItemDto
                    {
                        Id = r.Person!.Id,
                        Name = r.Person.Name,
                        Role = r.Role
                    })
                    .ToArray()
            })
            .FirstOrDefaultAsync();

    #endregion

    #region Commands

    public async Task<Guid> CreateAsync(OrganisationCreateDto dto, Guid createdBy)
    {
        Organisation organisation = new()
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            EmailAddress = dto.EmailAddress,
            Website = dto.Website,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };

        db.Organisations.Add(organisation);

        foreach (RelatedEntry entry in dto.OrganisationRelations.DistinctBy(e => e.Id))
        {
            if (!Guid.TryParse(entry.Id, out Guid targetOrgId)) continue;
            db.OrganisationRelationships.Add(new OrganisationRelationship
            {
                SourceOrganisationId = organisation.Id,
                TargetOrganisationId = targetOrgId,
                Relation = entry.Relation
            });
        }

        await db.SaveChangesAsync();
        return organisation.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, Action<Organisation> update)
    {
        Organisation? organisation = await db.Organisations.FindAsync(id);
        if (organisation == null) return false;

        update(organisation);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TrashAsync(Guid id)
    {
        Organisation? organisation = await db.Organisations.FindAsync(id);
        if (organisation == null) return false;

        organisation.Trashed = true;
        organisation.TrashDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UntrashAsync(Guid id)
    {
        Organisation? organisation = await db.Organisations.FindAsync(id);
        if (organisation == null) return false;

        organisation.Trashed = false;
        organisation.TrashDate = null;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Organisation? organisation = await db.Organisations.FindAsync(id);
        if (organisation == null) return false;

        db.Organisations.Remove(organisation);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task MergeAsync(Guid keepId, Guid removeId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        // Transfer Resource-Author relations
        List<ResourceAuthorRelation> authorRelations = await db.ResourceAuthorRelations.Where(r => r.AuthorId == removeId).ToListAsync();
        foreach (ResourceAuthorRelation relation in authorRelations)
        {
            bool exists = await db.ResourceAuthorRelations.AnyAsync(r => r.ResourceId == relation.ResourceId && r.AuthorId == keepId);
            if (!exists)
                db.ResourceAuthorRelations.Add(new ResourceAuthorRelation { ResourceId = relation.ResourceId, AuthorId = keepId });
        }

        // Transfer Resource-Organisation relations
        List<ResourceOrganisationRelation> resourceRelations = await db.ResourceOrganisationRelations.Where(r => r.OrganisationId == removeId).ToListAsync();
        foreach (ResourceOrganisationRelation relation in resourceRelations)
        {
            bool exists = await db.ResourceOrganisationRelations.AnyAsync(r => r.ResourceId == relation.ResourceId && r.OrganisationId == keepId);
            if (!exists)
                db.ResourceOrganisationRelations.Add(new ResourceOrganisationRelation { ResourceId = relation.ResourceId, OrganisationId = keepId, Role = relation.Role });
        }

        // Transfer Person-Organisation relations
        List<PersonOrganisationRelation> personRelations = await db.PersonOrganisationRelations.Where(r => r.OrganisationId == removeId).ToListAsync();
        foreach (PersonOrganisationRelation relation in personRelations)
        {
            bool exists = await db.PersonOrganisationRelations.AnyAsync(r => r.PersonId == relation.PersonId && r.OrganisationId == keepId);
            if (!exists)
                db.PersonOrganisationRelations.Add(new PersonOrganisationRelation { PersonId = relation.PersonId, OrganisationId = keepId, Role = relation.Role });
        }

        // Transfer Organisation-Organisation target relationships
        List<OrganisationRelationship> targetRelations = await db.OrganisationRelationships.Where(r => r.TargetOrganisationId == removeId).ToListAsync();
        foreach (OrganisationRelationship relation in targetRelations)
        {
            if (relation.SourceOrganisationId == keepId) continue;
            bool exists = await db.OrganisationRelationships.AnyAsync(r => r.SourceOrganisationId == relation.SourceOrganisationId && r.TargetOrganisationId == keepId);
            if (!exists)
                db.OrganisationRelationships.Add(new OrganisationRelationship { SourceOrganisationId = relation.SourceOrganisationId, TargetOrganisationId = keepId, Relation = relation.Relation });
        }

        // Transfer Organisation-Organisation source relationships
        List<OrganisationRelationship> sourceRelations = await db.OrganisationRelationships.Where(r => r.SourceOrganisationId == removeId).ToListAsync();
        foreach (OrganisationRelationship relation in sourceRelations)
        {
            if (relation.TargetOrganisationId == keepId) continue;
            bool exists = await db.OrganisationRelationships.AnyAsync(r => r.TargetOrganisationId == relation.TargetOrganisationId && r.SourceOrganisationId == keepId);
            if (!exists)
                db.OrganisationRelationships.Add(new OrganisationRelationship { SourceOrganisationId = keepId, TargetOrganisationId = relation.TargetOrganisationId, Relation = relation.Relation });
        }

        Organisation removeOrg = await db.Organisations.FindAsync(removeId) ?? throw new InvalidOperationException($"Organisation {removeId} not found during merge.");
        db.Organisations.Remove(removeOrg);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    // Authored resources
    public async Task AddAuthoredResourceAsync(Guid orgId, Guid resourceId)
    {
        db.ResourceAuthorRelations.Add(new ResourceAuthorRelation { AuthorId = orgId, ResourceId = resourceId });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveAuthoredResourceAsync(Guid orgId, Guid resourceId)
    {
        ResourceAuthorRelation? rel = await db.ResourceAuthorRelations.FindAsync(resourceId, orgId);
        if (rel == null) return false;
        db.ResourceAuthorRelations.Remove(rel);
        await db.SaveChangesAsync();
        return true;
    }

    // Related resources
    public async Task AddRelatedResourceAsync(Guid orgId, Guid resourceId, string? role)
    {
        db.ResourceOrganisationRelations.Add(new ResourceOrganisationRelation { OrganisationId = orgId, ResourceId = resourceId, Role = role });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveRelatedResourceAsync(Guid orgId, Guid resourceId)
    {
        ResourceOrganisationRelation? rel = await db.ResourceOrganisationRelations.FindAsync(resourceId, orgId);
        if (rel == null) return false;
        db.ResourceOrganisationRelations.Remove(rel);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRelatedResourceRoleAsync(Guid orgId, Guid resourceId, string newRole)
    {
        ResourceOrganisationRelation? rel = await db.ResourceOrganisationRelations.FindAsync(resourceId, orgId);
        if (rel == null) return false;
        rel.Role = newRole;
        await db.SaveChangesAsync();
        return true;
    }

    // Organisation relationships
    public async Task AddOrganisationRelationshipAsync(Guid orgId, Guid targetOrgId, string? relation)
    {
        db.OrganisationRelationships.Add(new OrganisationRelationship { SourceOrganisationId = orgId, TargetOrganisationId = targetOrgId, Relation = relation });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveOrganisationRelationshipAsync(Guid orgId, Guid targetOrgId)
    {
        OrganisationRelationship? rel = await db.OrganisationRelationships.FindAsync(orgId, targetOrgId);
        if (rel == null) return false;
        db.OrganisationRelationships.Remove(rel);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateOrganisationRelationshipRoleAsync(Guid orgId, Guid targetOrgId, string newRole)
    {
        OrganisationRelationship? rel = await db.OrganisationRelationships.FindAsync(orgId, targetOrgId);
        if (rel == null) return false;
        rel.Relation = newRole;
        await db.SaveChangesAsync();
        return true;
    }

    // Person relations
    public async Task AddPersonRelationAsync(Guid orgId, Guid personId, string? role)
    {
        db.PersonOrganisationRelations.Add(new PersonOrganisationRelation { OrganisationId = orgId, PersonId = personId, Role = role });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemovePersonRelationAsync(Guid orgId, Guid personId)
    {
        PersonOrganisationRelation? rel = await db.PersonOrganisationRelations.FindAsync(personId, orgId);
        if (rel == null) return false;
        db.PersonOrganisationRelations.Remove(rel);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdatePersonRelationRoleAsync(Guid orgId, Guid personId, string newRole)
    {
        PersonOrganisationRelation? rel = await db.PersonOrganisationRelations.FindAsync(personId, orgId);
        if (rel == null) return false;
        rel.Role = newRole;
        await db.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Suggestions

    public async Task<List<MergeSuggestion>> GetMergeSuggestionsAsync(float threshold = 0.6f, int limit = 20)
    {
        return await db.Database.SqlQuery<MergeSuggestion>($"""
            SELECT
                a.id        AS "Id1",
                a.name      AS "Name1",
                b.id        AS "Id2",
                b.name      AS "Name2",
                similarity(a.name, b.name) AS "Score"
            FROM organisations a
            JOIN organisations b ON a.id < b.id
            WHERE similarity(a.name, b.name) > {threshold}
            ORDER BY "Score" DESC
            LIMIT {limit}
        """).ToListAsync();
    }

    #endregion
}