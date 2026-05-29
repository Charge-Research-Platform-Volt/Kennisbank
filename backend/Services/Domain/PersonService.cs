using System.Linq.Expressions;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class PersonService(DatabaseContext db)
{
    #region Queries

    public async Task<Person?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        IQueryable<Person> query = db.Persons;

        if (includeRelations)
            query = query.Include(p => p.TargetRelationships!).ThenInclude(x => x.SourcePerson)
                            .Include(p => p.SourceRelationships!).ThenInclude(x => x.TargetPerson)
                            .Include(p => p.PersonOrganisationRelations!).ThenInclude(x => x.Organisation)
                            .Include(p => p.ResourceRelatedPersonRelations);

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<(Person[] Items, int TotalCount)> GetPageAsync(int page, int pageSize, Expression<Func<Person, bool>>? predicate, bool includeRelations = false)
    {
        IQueryable<Person> query = db.Persons;

        if (includeRelations)
            query = query.Include(p => p.TargetRelationships!).ThenInclude(x => x.SourcePerson)
                            .Include(p => p.SourceRelationships!).ThenInclude(x => x.TargetPerson)
                            .Include(p => p.PersonOrganisationRelations!).ThenInclude(x => x.Organisation)
                            .Include(p => p.ResourceRelatedPersonRelations);

        if (predicate != null)
            query = query.Where(predicate);

        int totalCount = await query.CountAsync();

        Person[] items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        return (items, totalCount);
    }

    public async Task<Person[]> GetAllAsync(Expression<Func<Person, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<Person> query = db.Persons;

        if (includeRelations)
            query = query.Include(p => p.TargetRelationships!).ThenInclude(x => x.SourcePerson)
                         .Include(p => p.SourceRelationships!).ThenInclude(x => x.TargetPerson)
                         .Include(p => p.PersonOrganisationRelations!).ThenInclude(x => x.Organisation)
                         .Include(p => p.ResourceRelatedPersonRelations);

        if (predicate != null)
            query = query.Where(predicate);

        return await query.OrderBy(p => p.Name).ToArrayAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await db.Persons.AnyAsync(p => p.Id == id);

    public async Task<bool> ExistsAsync(Expression<Func<Person, bool>> predicate)
        => await db.Persons.AnyAsync(predicate);

    public async Task<int> CountAsync(Expression<Func<Person, bool>>? predicate = null)
    {
        IQueryable<Person> query = db.Persons;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public async Task<Guid?> FindIdByNameAsync(string name)
        => await db.Persons.Where(p => p.Name == name).Select(p => (Guid?)p.Id).FirstOrDefaultAsync();

    public async Task<PersonDetailDto?> GetDetailAsync(Guid id)
        => await db.Persons
            .Where(p => p.Id == id)
            .Select(p => new PersonDetailDto
            {
                Name = p.Name,
                Description = p.Description,
                Occupation = p.Occupation,
                EmailAddress = p.EmailAddress,
                Linkedin = p.Linkedin,
                Authored = p.ResourceAuthorRelations!
                    .Select(r => new RelationItemDto { Id = r.Resource!.Id, Name = r.Resource.Title })
                    .ToArray(),
                RelatedResources = p.ResourceRelatedPersonRelations!
                    .Select(r => new RelationItemDto { Id = r.Resource!.Id, Name = r.Resource.Title, Role = r.Role, FileType = r.Resource.FileType.ToString() })
                    .ToArray(),
                TargetPersons = p.TargetRelationships!
                    .Select(r => new RelationItemDto { Id = r.TargetPersonId, Name = r.TargetPerson!.Name, Relation = r.Relation })
                    .ToArray(),
                SourcePersons = p.SourceRelationships!
                    .Select(r => new RelationItemDto { Id = r.SourcePersonId, Name = r.SourcePerson!.Name, Relation = r.Relation })
                    .ToArray(),
                RelatedOrganisations = p.PersonOrganisationRelations!
                    .Select(r => new RelationItemDto { Id = r.OrganisationId, Name = r.Organisation!.Name, Role = r.Role })
                    .ToArray()
            }).FirstOrDefaultAsync();

    #endregion

    #region Commands

    public async Task<Guid> CreateAsync(PersonCreateDto dto, Guid createdBy)
    {
        Person person = new()
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Occupation = dto.Occupation,
            Description = dto.Description,
            EmailAddress = dto.EmailAddress,
            Linkedin = dto.Linkedin,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };

        db.Persons.Add(person);

        foreach (RelatedEntry entry in dto.OrganisationRelations.DistinctBy(e => e.Id))
        {
            if (!Guid.TryParse(entry.Id, out Guid orgId)) continue;
            db.PersonOrganisationRelations.Add(new PersonOrganisationRelation
            {
                PersonId = person.Id,
                OrganisationId = orgId,
                Role = entry.Relation
            });
        }

        foreach (RelatedEntry entry in dto.PersonRelations.DistinctBy(e => e.Id))
        {
            if (!Guid.TryParse(entry.Id, out Guid targetId)) continue;
            db.PersonRelationships.Add(new PersonRelationship
            {
                SourcePersonId = person.Id,
                TargetPersonId = targetId,
                Relation = entry.Relation
            });
        }

        await db.SaveChangesAsync();
        return person.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, Action<Person> update)
    {
        Person? person = await db.Persons.FindAsync(id);
        if (person == null) return false;

        update(person);
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> TrashAsync(Guid id)
    {
        Person? person = await db.Persons.FindAsync(id);
        if (person == null) return false;

        person.Trashed = true;
        person.TrashDate = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UntrashAsync(Guid id)
    {
        Person? person = await db.Persons.FindAsync(id);
        if (person == null) return false;

        person.Trashed = false;
        person.TrashDate = null;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Person? person = await db.Persons.FindAsync(id);
        if (person == null) return false;

        db.Persons.Remove(person);
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
            bool alreadyHasAuthor = await db.ResourceAuthorRelations.AnyAsync(r => r.ResourceId == relation.ResourceId && r.AuthorId == keepId);

            if (!alreadyHasAuthor)
                db.ResourceAuthorRelations.Add(new ResourceAuthorRelation { ResourceId = relation.ResourceId, AuthorId = keepId });
        }

        // Transfer Person-Organisation relations
        List<PersonOrganisationRelation> organisationRelations = await db.PersonOrganisationRelations.Where(r => r.PersonId == removeId).ToListAsync();

        foreach (PersonOrganisationRelation relation in organisationRelations)
        {
            bool alreadyHasPerson = await db.PersonOrganisationRelations.AnyAsync(r => r.OrganisationId == relation.OrganisationId && r.PersonId == keepId);

            if (!alreadyHasPerson)
                db.PersonOrganisationRelations.Add(new PersonOrganisationRelation { OrganisationId = relation.OrganisationId, PersonId = keepId, Role = relation.Role });
        }

        // Transfer Resource-Person relations
        List<ResourceRelatedPersonRelation> resourceRelations = await db.ResourceRelatedPersonRelations.Where(r => r.PersonId == removeId).ToListAsync();

        foreach (ResourceRelatedPersonRelation relation in resourceRelations)
        {
            bool alreadyHasPerson = await db.ResourceRelatedPersonRelations.AnyAsync(r => r.ResourceId == relation.ResourceId && r.PersonId == keepId);

            if (!alreadyHasPerson)
                db.ResourceRelatedPersonRelations.Add(new ResourceRelatedPersonRelation { ResourceId = relation.ResourceId, PersonId = keepId, Role = relation.Role });
        }

        // Transfer Person-Person target relationships
        List<PersonRelationship> targetPersonRelations = await db.PersonRelationships.Where(r => r.TargetPersonId == removeId).ToListAsync();

        foreach (PersonRelationship relation in targetPersonRelations)
        {
            if (relation.SourcePersonId == keepId) continue;

            bool exists = await db.PersonRelationships.AnyAsync(r => r.SourcePersonId == relation.SourcePersonId && r.TargetPersonId == keepId);

            if (!exists)
                db.PersonRelationships.Add(new PersonRelationship { SourcePersonId = relation.SourcePersonId, TargetPersonId = keepId, Relation = relation.Relation });
        }

        // Transfer Person-Person source relationships
        List<PersonRelationship> sourcePersonRelations = await db.PersonRelationships.Where(r => r.SourcePersonId == removeId).ToListAsync();

        foreach (PersonRelationship relation in sourcePersonRelations)
        {
            if (relation.TargetPersonId == keepId) continue;

            bool exists = await db.PersonRelationships.AnyAsync(r => r.TargetPersonId == relation.TargetPersonId && r.SourcePersonId == keepId);

            if (!exists)
                db.PersonRelationships.Add(new PersonRelationship { TargetPersonId = relation.TargetPersonId, SourcePersonId = keepId, Relation = relation.Relation });
        }

        Person removePerson = await db.Persons.FindAsync(removeId) ?? throw new InvalidOperationException($"Person {removeId} not found during merge.");

        db.Persons.Remove(removePerson);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    // Authored resources
    public async Task AddAuthoredResourceAsync(Guid personId, Guid resourceId)
    {
        db.ResourceAuthorRelations.Add(new ResourceAuthorRelation { AuthorId = personId, ResourceId = resourceId });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveAuthoredResourceAsync(Guid personId, Guid resourceId)
    {
        ResourceAuthorRelation? relation = await db.ResourceAuthorRelations.FindAsync(resourceId, personId);
        if (relation == null) return false;

        db.ResourceAuthorRelations.Remove(relation);
        await db.SaveChangesAsync();
        return true;
    }

    // Related resources
    public async Task AddRelatedResourceAsync(Guid personId, Guid resourceId, string? role)
    {
        db.ResourceRelatedPersonRelations.Add(new ResourceRelatedPersonRelation { PersonId = personId, ResourceId = resourceId, Role = role });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveRelatedResourceAsync(Guid personId, Guid resourceId)
    {
        ResourceRelatedPersonRelation? relation = await db.ResourceRelatedPersonRelations.FindAsync(resourceId, personId);
        if (relation == null) return false;

        db.ResourceRelatedPersonRelations.Remove(relation);

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRelatedResourceRoleAsync(Guid personId, Guid resourceId, string newRole)
    {
        ResourceRelatedPersonRelation? relation = await db.ResourceRelatedPersonRelations.FindAsync(resourceId, personId);
        if (relation == null) return false;

        relation.Role = newRole;

        await db.SaveChangesAsync();
        return true;
    }

    // Person relationships
    public async Task AddPersonRelationshipAsync(Guid personId, Guid targetPersonId, string? relation)
    {
        db.PersonRelationships.Add(new PersonRelationship { SourcePersonId = personId, TargetPersonId = targetPersonId, Relation = relation });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemovePersonRelationshipAsync(Guid personId, Guid targetPersonId)
    {
        PersonRelationship? rel = await db.PersonRelationships.FindAsync(personId, targetPersonId);
        if (rel == null) return false;

        db.PersonRelationships.Remove(rel);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdatePersonRelationshipRoleAsync(Guid personId, Guid targetPersonId, string newRole)
    {
        PersonRelationship? rel = await db.PersonRelationships.FindAsync(personId, targetPersonId);
        if (rel == null) return false;

        rel.Relation = newRole;
        await db.SaveChangesAsync();
        return true;
    }

    // Organisation relations
    public async Task AddOrganisationRelationAsync(Guid personId, Guid orgId, string? role)
    {
        db.PersonOrganisationRelations.Add(new PersonOrganisationRelation { PersonId = personId, OrganisationId = orgId, Role = role });
        await db.SaveChangesAsync();
    }

    public async Task<bool> RemoveOrganisationRelationAsync(Guid personId, Guid orgId)
    {
        PersonOrganisationRelation? rel = await db.PersonOrganisationRelations.FindAsync(personId, orgId);
        if (rel == null) return false;

        db.PersonOrganisationRelations.Remove(rel);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateOrganisationRelationRoleAsync(Guid personId, Guid orgId, string newRole)
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
            FROM persons a
            JOIN persons b ON a.id < b.id
            WHERE similarity(a.name, b.name) > {threshold}
            ORDER BY "Score" DESC
            LIMIT {limit}
        """).ToListAsync();
    }

    #endregion
}