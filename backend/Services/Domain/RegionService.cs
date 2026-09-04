using System.Linq.Expressions;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.Search;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class RegionService(DatabaseContext db, TaxonomySearchIndexService taxonomySearchIndexService)
{
    public const string TypeTag = "region";

    #region Queries

    public async Task<Region?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        IQueryable<Region> query = db.Regions;

        if (includeRelations)
            query = query.Include(r => r.ResourceRegionRelations);

        return await query.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Region[]> GetAllAsync(Expression<Func<Region, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<Region> query = db.Regions;

        if (includeRelations)
            query = query.Include(r => r.ResourceRegionRelations);

        if (predicate != null)
            query = query.Where(predicate);

        return await query.OrderBy(r => r.Name).ToArrayAsync();
    }

    public async Task<(Region[] Items, int TotalCount)> GetPageAsync(int page, int pageSize, Expression<Func<Region, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<Region> query = db.Regions;

        if (includeRelations)
            query = query.Include(r => r.ResourceRegionRelations);

        if (predicate != null)
            query = query.Where(predicate);

        int totalCount = await query.CountAsync();

        Region[] items = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        return (items, totalCount);
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await db.Regions.AnyAsync(r => r.Id == id);

    public async Task<bool> ExistsAsync(Expression<Func<Region, bool>> predicate)
        => await db.Regions.AnyAsync(predicate);

    public async Task<int> CountAsync(Expression<Func<Region, bool>>? predicate = null)
    {
        IQueryable<Region> query = db.Regions;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public async Task<Guid?> FindIdByNameAsync(string name)
        => await db.Regions.Where(r => r.Name == name).Select(r => (Guid?)r.Id).FirstOrDefaultAsync();

    public async Task<(Region[] Items, int TotalCount)> SearchAsync(string query, int page, int pageSize)
    {
        var (ids, totalCount) = await taxonomySearchIndexService.SearchAsync(query, TypeTag, page, pageSize);
        Region[] regions = await GetAllAsync(predicate: r => ids.Contains(r.Id));
        Dictionary<Guid, Region> lookup = regions.ToDictionary(r => r.Id);
        Region[] items = ids.Where(lookup.ContainsKey).Select(id => lookup[id]).ToArray();
        return (items, totalCount);
    }

    #endregion

    #region Commands

    public async Task<Guid> CreateAsync(string name, Guid createdBy)
    {
        Region region = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };

        db.Regions.Add(region);
        await db.SaveChangesAsync();
        await taxonomySearchIndexService.SyncAsync(region.Id, region.Name, TypeTag);
        return region.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, Action<Region> update)
    {
        Region? region = await db.Regions.FindAsync(id);
        if (region == null) return false;

        update(region);
        await db.SaveChangesAsync();
        await taxonomySearchIndexService.SyncAsync(region.Id, region.Name, TypeTag);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Region? region = await db.Regions.FindAsync(id);
        if (region == null) return false;

        db.Regions.Remove(region);
        await db.SaveChangesAsync();
        await taxonomySearchIndexService.DeleteAsync(id);
        return true;
    }

    public async Task MergeAsync(Guid keepId, Guid removeId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        List<ResourceRegionRelation> resourceRelations = await db.ResourceRegionRelations.Where(r => r.RegionId == removeId).ToListAsync();

        foreach (ResourceRegionRelation relation in resourceRelations)
        {
            bool alreadyHasRegion = await db.ResourceRegionRelations.AnyAsync(r => r.ResourceId == relation.ResourceId && r.RegionId == keepId);

            if (!alreadyHasRegion)
                db.ResourceRegionRelations.Add(new ResourceRegionRelation { ResourceId = relation.ResourceId, RegionId = keepId });
        }

        var removeRegion = await db.Regions.FindAsync(removeId) ?? throw new InvalidOperationException($"Region {removeId} not found during merge.");

        db.Regions.Remove(removeRegion);
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
            FROM regions a
            JOIN regions b ON a.id < b.id
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