using System.Linq.Expressions;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class ResourceTypeService(DatabaseContext db)
{
    #region Queries

    public async Task<ResourceType?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        IQueryable<ResourceType> query = db.ResourceTypes;

        if (includeRelations)
            query = query.Include(r => r.Resources);

        return await query.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ResourceType[]> GetAllAsync(Expression<Func<ResourceType, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<ResourceType> query = db.ResourceTypes;

        if (includeRelations)
            query = query.Include(r => r.Resources);

        if (predicate != null)
            query = query.Where(predicate);

        return await query.OrderBy(r => r.Name).ToArrayAsync();
    }

    public async Task<(ResourceType[] Items, int TotalCount)> GetPageAsync(int page, int pageSize, Expression<Func<ResourceType, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<ResourceType> query = db.ResourceTypes;

        if (includeRelations)
            query = query.Include(r => r.Resources);

        if (predicate != null)
            query = query.Where(predicate);

        int totalCount = await query.CountAsync();

        ResourceType[] items = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        return (items, totalCount);
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await db.ResourceTypes.AnyAsync(r => r.Id == id);

    public async Task<bool> ExistsAsync(Expression<Func<ResourceType, bool>> predicate)
        => await db.ResourceTypes.AnyAsync(predicate);

    public async Task<int> CountAsync(Expression<Func<ResourceType, bool>>? predicate = null)
    {
        IQueryable<ResourceType> query = db.ResourceTypes;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public async Task<Guid?> FindIdByNameAsync(string name)
        => await db.ResourceTypes.Where(r => r.Name == name).Select(r => (Guid?)r.Id).FirstOrDefaultAsync();

    #endregion

    #region Commands

    public async Task<Guid> CreateAsync(string name, Guid createdBy)
    {
        ResourceType resourceType = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };

        db.ResourceTypes.Add(resourceType);
        await db.SaveChangesAsync();
        return resourceType.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, Action<ResourceType> update)
    {
        ResourceType? resourceType = await db.ResourceTypes.FindAsync(id);
        if (resourceType == null) return false;

        update(resourceType);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        ResourceType? resourceType = await db.ResourceTypes.FindAsync(id);
        if (resourceType == null) return false;

        db.ResourceTypes.Remove(resourceType);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task MergeAsync(Guid keepId, Guid removeId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.Resources.Where(r => r.TypeId == removeId).ExecuteUpdateAsync(s => s.SetProperty(r => r.TypeId, keepId));

        ResourceType removeType = await db.ResourceTypes.FindAsync(removeId) ?? throw new InvalidOperationException($"Resource type {removeId} not found during merge.");

        db.ResourceTypes.Remove(removeType);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
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
            FROM resource-types a
            JOIN resource-types b ON a.id < b.id
            WHERE similarity(a.name, b.name) > {threshold}
            ORDER BY "Score" DESC
            LIMIT {limit}
        """).ToListAsync();
    }

    #endregion
}