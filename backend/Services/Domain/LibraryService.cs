using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Search;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class LibraryService(DatabaseContext db, LibrarySearchIndexService librarySearchIndexService, ChunkSearchIndexService chunkSearchIndexService, EmbeddingService embeddingService)
{
    private readonly Serilog.ILogger logger = Serilog.Log.ForContext<LibraryService>();

    public async Task<List<LibraryItemWithChunks>> SearchContentAsync(string query, int limit, Guid[]? idsFilter = null, string? typeFilter = null, Guid[]? excludeIds = null, int chunksPerParent = 3)
    {
        float[] queryEmbedding = await embeddingService.GenerateEmbedding(query);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        List<ChunkSearchResult> chunkResults = await chunkSearchIndexService.SearchAsync(query, queryEmbedding, limit, idsFilter, typeFilter, excludeIds, chunksPerParent);
        stopwatch.Stop();
        logger.Debug("Meilisearch chunk search took {ElapsedMs}ms for query: {Query}", stopwatch.ElapsedMilliseconds, query);

        Guid[] parentIds = chunkResults.Select(r => r.ParentId).ToArray();
        Dictionary<Guid, LibraryItem> itemLookup = await db.LibraryItems
            .Where(i => parentIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id);

        return chunkResults
            .Where(r => itemLookup.ContainsKey(r.ParentId))
            .Select(r => new LibraryItemWithChunks
            {
                Item = itemLookup[r.ParentId],
                MatchedChunks = r.MatchedChunks,
                Score = r.Score
            })
            .ToList();
    }

    public async Task<LibraryResult> GetLibraryAsync(LibraryRequest request)
    {
        DateTime? minDate = null;
        DateTime? maxDate = null;

        if (!string.IsNullOrEmpty(request.FilterOptions?.PubdateMin) && DateTime.TryParse(request.FilterOptions.PubdateMin, out var parsedMin))
            minDate = DateTime.SpecifyKind(parsedMin, DateTimeKind.Utc);

        if (!string.IsNullOrEmpty(request.FilterOptions?.PubdateMax) && DateTime.TryParse(request.FilterOptions.PubdateMax, out var parsedMax))
            maxDate = DateTime.SpecifyKind(parsedMax, DateTimeKind.Utc);

        Dictionary<string, object?> filters = new()
        {
            { "type", request.FilterOptions?.TypeFilter },
            { "pubdate_min", minDate },
            { "pubdate_max", maxDate },
            { "tag_ids", ParseGuids(request.FilterOptions?.TagFilter) },
            { "tag_filter_mode", request.FilterOptions?.TagFilterMode },
            { "region_ids", ParseGuids(request.FilterOptions?.RegionFilter) },
            { "region_filter_mode", request.FilterOptions?.RegionFilterMode },
            { "resource_type_ids", ParseGuids(request.FilterOptions?.ResourceTypeFilter) },
            { "journal_ids", ParseGuids(request.FilterOptions?.JournalFilter) },
        };

        Dictionary<string, int> typeCounts = await librarySearchIndexService.GetTypeFacetsAsync(request.Search ?? "", request.FilterOptions);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            float[] queryEmbedding = await embeddingService.GenerateEmbedding(request.Search);
            var (ids, searchTotalCount) = await librarySearchIndexService.SearchAsync(request.Search, request.FilterOptions, request.Page, request.PageSize, queryEmbedding);

            Dictionary<Guid, LibraryItem> itemLookup = await db.LibraryItems.Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id);

            return new LibraryResult
            {
                Items = [.. ids.Where(itemLookup.ContainsKey).Select(id => itemLookup[id])],
                TotalCount = searchTotalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                SearchTerm = request.Search,
                TypeCounts = typeCounts
            };
        }

        IQueryable<LibraryItem> query = db.LibraryItems.AsQueryable();
        query = ApplyFilters(query, filters);
        query = ApplySorting(query, request.SortBy, request.SortDirection);

        int totalCount = await query.CountAsync();
        LibraryItem[] items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArrayAsync();

        return new LibraryResult
        {
            Items = items,
            TotalCount = totalCount,
            Page =request.Page,
            PageSize = request.PageSize,
            TypeCounts = typeCounts
        };
    }

    public async Task<LibraryItem[]> GetLibraryItemsByIdsAsync(IEnumerable<Guid> ids)
        => await db.LibraryItems.Where(i => ids.Contains(i.Id)).ToArrayAsync();

    public async Task<Dictionary<Guid, List<RelationItemDto>>> GetAuthorNamesForResourcesAsync(IEnumerable<Guid> resourceIds)
    {
        Guid[] ids = [.. resourceIds];
        if (ids.Length == 0) return [];

        var rows = await db.ResourceAuthorRelations
            .Where(r => ids.Contains(r.ResourceId) && r.Author != null)
            .Select(r => new
            {
                r.ResourceId,
                r.AuthorId,
                AuthorName = r.Author!.Name,
                AuthorType = r.Author is Person ? "person" : "organisation"
            })
            .ToListAsync();

        return rows
            .GroupBy(r => r.ResourceId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new RelationItemDto { Id = r.AuthorId, Name = r.AuthorName, FileType = r.AuthorType }).ToList()
            );
    }

    public async Task<bool> ItemExistsAsync(Guid id)
        => await db.LibraryItems.AnyAsync(i => i.Id == id);

    public async Task<TrashItem[]> GetTrashItemsAsync()
        => await db.TrashItems.OrderByDescending(x => x.TrashDate).ToArrayAsync();

    private IQueryable<LibraryItem> ApplyFilters(IQueryable<LibraryItem> query, Dictionary<string, object?> filters)
    {
        if (filters.TryGetValue("type", out var typeFilter) && typeFilter is string[] types && types.Length > 0)
            query = query.Where(x => types.Contains(x.Type));

        if (filters.TryGetValue("pubdate_min", out var minDateFilter) && minDateFilter is DateTime minDate)
            query = query.Where(x => x.PublicationDate == null || x.PublicationDate >= minDate);

        if (filters.TryGetValue("pubdate_max", out var maxDateFilter) && maxDateFilter is DateTime maxDate)
            query = query.Where(x => x.PublicationDate == null || x.PublicationDate < maxDate.AddDays(1));

        query = ApplyRelationFilter(query, filters, "tag_ids", "tag_filter_mode", "tag");
        query = ApplyRelationFilter(query, filters, "region_ids", "region_filter_mode", "region");

        if (filters.TryGetValue("resource_type_ids", out var rtFilter) && rtFilter is Guid[] typeIds && typeIds.Length > 0)
            query = query.Where(x => x.Type != "resource" || (x.TypeId != null && typeIds.Contains(x.TypeId.Value)));

        if (filters.TryGetValue("journal_ids", out var journalFilter) && journalFilter is Guid[] journalIds && journalIds.Length > 0)
            query = query.Where(x => x.Type != "resource" || (x.JournalId != null && journalIds.Contains(x.JournalId.Value)));

        return query;
    }

    private IQueryable<LibraryItem> ApplyRelationFilter(IQueryable<LibraryItem> query, Dictionary<string, object?> filters, string idsKey, string modeKey, string relationType)
    {
        if (!filters.TryGetValue(idsKey, out var idsFilter) || idsFilter is not Guid[] ids || ids.Length == 0)
            return query;

        string filterMode = filters.TryGetValue(modeKey, out var mode) && mode is string modeStr ? modeStr : "any";

        if (filterMode.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            foreach (Guid id in ids)
            {
                if (relationType == "tag")
                    query = query.Where(x => x.Type != "resource" || db.ResourceTagRelations.Any(rt => rt.TagId == id && rt.ResourceId == x.Id));
                else if (relationType == "region")
                    query = query.Where(x => x.Type != "resource" || db.ResourceRegionRelations.Any(rr => rr.RegionId == id && rr.ResourceId == x.Id));
            }
        }
        else
        {
            if (relationType == "tag")
                query = query.Where(x => x.Type != "resource" || db.ResourceTagRelations.Any(rt => ids.Contains(rt.TagId) && rt.ResourceId == x.Id));
            else if (relationType == "region")
                query = query.Where(x => x.Type != "resource" || db.ResourceRegionRelations.Any(rr => ids.Contains(rr.RegionId) && rr.ResourceId == x.Id));
        }

        return query;
    }

    private static IQueryable<LibraryItem> ApplySorting(IQueryable<LibraryItem> query, string? sortBy, string? sortDirection)
    {
        return (sortBy?.ToLower(), sortDirection?.ToLower()) switch
        {
            ("name", "desc") => query.OrderByDescending(x => x.Name),
            ("name", _) => query.OrderBy(x => x.Name),
            ("type", "desc") => query.OrderByDescending(x => x.Type),
            ("type", _) => query.OrderBy(x => x.Type),
            ("publicationdate", "desc") => query.OrderByDescending(x => x.PublicationDate),
            ("publicationdate", _) => query.OrderBy(x => x.PublicationDate),
            ("createdon", "desc") => query.OrderByDescending(x => x.CreatedOn),
            ("createdon", _) => query.OrderBy(x => x.CreatedOn),
            _ => query.OrderByDescending(x => x.CreatedOn)
        };
    }

    private static Guid[] ParseGuids(string[]? strings)
    {
        if (strings == null || strings.Length == 0) return [];
        return strings
            .Where(s => Guid.TryParse(s, out _))
            .Select(Guid.Parse)
            .ToArray();
    }
}