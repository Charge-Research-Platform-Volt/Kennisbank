using System.Linq.Expressions;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.Search;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class TagService(DatabaseContext db, TaxonomySearchIndexService taxonomySearchIndexService)
{
    public const string TypeTag = "tag";

    #region Queries

    public async Task<Tag?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        IQueryable<Tag> query = db.Tags;

        if (includeRelations)
            query = query.Include(t => t.ResourceTagRelations).Include(t => t.ProjectTagRelations);

        return await query.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tag[]> GetAllAsync(Expression<Func<Tag, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<Tag> query = db.Tags;

        if (includeRelations)
            query = query.Include(t => t.ResourceTagRelations).Include(t => t.ProjectTagRelations);

        if (predicate != null)
            query = query.Where(predicate);

        return await query.OrderBy(t => t.Name).ToArrayAsync();
    }

    public async Task<(Tag[] Items, int TotalCount)> GetPageAsync(int page, int pageSize, Expression<Func<Tag, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<Tag> query = db.Tags;

        if (includeRelations)
            query = query.Include(t => t.ResourceTagRelations).Include(t => t.ProjectTagRelations);

        if (predicate != null)
            query = query.Where(predicate);

        int totalCount = await query.CountAsync();

        Tag[] items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        return (items, totalCount);
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await db.Tags.AnyAsync(t => t.Id == id);

    public async Task<bool> ExistsAsync(Expression<Func<Tag, bool>> predicate)
        => await db.Tags.AnyAsync(predicate);

    public async Task<int> CountAsync(Expression<Func<Tag, bool>>? predicate = null)
    {
        IQueryable<Tag> query = db.Tags;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public async Task<Guid?> FindIdByNameAsync(string name)
        => await db.Tags.Where(t => t.Name == name).Select(t => (Guid?)t.Id).FirstOrDefaultAsync();

    public async Task<(Tag[] Items, int TotalCount)> SearchAsync(string query, int page, int pageSize)
    {
        var (ids, totalCount) = await taxonomySearchIndexService.SearchAsync(query, TypeTag, page, pageSize);
        Tag[] tags = await GetAllAsync(predicate: t => ids.Contains(t.Id));
        Dictionary<Guid, Tag> lookup = tags.ToDictionary(t => t.Id);
        Tag[] items = ids.Where(lookup.ContainsKey).Select(id => lookup[id]).ToArray();
        return (items, totalCount);
    }

    #endregion

    #region Commands

    public async Task<Guid> CreateAsync(string name, Guid createdBy)
    {
        Tag tag = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };

        db.Tags.Add(tag);
        await db.SaveChangesAsync();
        await taxonomySearchIndexService.SyncAsync(tag.Id, tag.Name, TypeTag);
        return tag.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, Action<Tag> update)
    {
        Tag? tag = await db.Tags.FindAsync(id);
        if (tag == null) return false;

        update(tag);
        await db.SaveChangesAsync();
        await taxonomySearchIndexService.SyncAsync(tag.Id, tag.Name, TypeTag);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Tag? tag = await db.Tags.FindAsync(id);
        if (tag == null) return false;

        db.Tags.Remove(tag);
        await db.SaveChangesAsync();
        await taxonomySearchIndexService.DeleteAsync(id);
        return true;
    }

    public async Task MergeAsync(Guid keepId, Guid removeId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        List<ResourceTagRelation> resourceRelations = await db.ResourceTagRelations.Where(r => r.TagId == removeId).ToListAsync();

        foreach (ResourceTagRelation relation in resourceRelations)
        {
            bool alreadyHasTag = await db.ResourceTagRelations.AnyAsync(r => r.ResourceId == relation.ResourceId && r.TagId == keepId);

            if (!alreadyHasTag)
                db.ResourceTagRelations.Add(new ResourceTagRelation { ResourceId = relation.ResourceId, TagId = keepId });
        }

        List<ProjectTagRelation> projectRelations = await db.ProjectTagRelations.Where(r => r.TagId == removeId).ToListAsync();

        foreach (ProjectTagRelation relation in projectRelations)
        {
            bool alreadyHasTag = await db.ProjectTagRelations.AnyAsync(r => r.ProjectId == relation.ProjectId && r.TagId == keepId);

            if (!alreadyHasTag)
                db.ProjectTagRelations.Add(new ProjectTagRelation { ProjectId = relation.ProjectId, TagId = keepId });
        }

        var removeTag = await db.Tags.FindAsync(removeId) ?? throw new InvalidOperationException($"Tag {removeId} not found during merge.");

        db.Tags.Remove(removeTag);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await taxonomySearchIndexService.DeleteAsync(removeId);
    }

    #endregion

    #region Suggestions

    public async Task<List<MergeSuggestion>> GetMergeSuggestionsAsync(float threshold = 0.5f, int limit = 20)
    {
        return await db.Database.SqlQuery<MergeSuggestion>($"""
            SELECT
                a.id        AS "Id1",
                a.name      AS "Name1",
                b.id        AS "Id2",
                b.name      AS "Name2",
                similarity(a.name, b.name) AS "Score"
            FROM tags a
            JOIN tags b ON a.id < b.id
            WHERE similarity(a.name, b.name) > {threshold}
                AND NOT EXISTS (
                    SELECT 1 FROM "dismissed-merge-suggestions" d
                    WHERE d."entity-type" = {TypeTag} AND d.id1 = a.id AND d.id2 = b.id
                )
            ORDER BY "Score" DESC
            LIMIT {limit}
        """).ToListAsync();
    }

    #endregion
}