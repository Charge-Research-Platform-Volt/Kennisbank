using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.Search;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class LibraryService(DatabaseContext db, HybridSearchService hybridSearch)
{
    public async Task<GridResult> GetGridAsync(GridRequest request)
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

        if (!string.IsNullOrEmpty(request.Search))
        {
            var result = await hybridSearch.SearchAsync(request.Search, request.Page, request.PageSize, filters);

            return new GridResult
            {
                Items = result.Items.Select(item => new ResourceGridItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    PublicationDate = item.PublicationDate,
                    PublicationDatePrecision = item.PublicationDatePrecision,
                    Type = item.Type,
                    FileType = item.FileType,
                    CreatedOn = item.CreatedOn,
                    Chunks = item.MatchedChunks
                }).ToArray(),
                TotalCount = result.TotalCount,
                DurationInMs = (int)result.DurationInMs,
                Page = result.Page,
                PageSize = result.PageSize,
                SearchTerm = result.SearchTerm,
                IsSearchResult = result.IsSearchResult
            };
        }

        IQueryable<ResourceGridItem> query = db.ResourceGridItems.AsQueryable();
        query = ApplyFilters(query, filters);
        query = ApplySorting(query, request.SortBy, request.SortDirection);

        int totalCount = await query.CountAsync();
        ResourceGridItem[] items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArrayAsync();

        return new GridResult
        {
            Items = items,
            TotalCount = totalCount,
            Page =request.Page,
            PageSize = request.PageSize,
            IsSearchResult = false
        };
    }

    public async Task<ResourceGridItem[]> GetGridItemsByIdsAsync(IEnumerable<Guid> ids)
        => await db.ResourceGridItems.Where(i => ids.Contains(i.Id)).ToArrayAsync();

    public async Task<bool> ItemExistsAsync(Guid id)
        => await db.ResourceGridItems.AnyAsync(i => i.Id == id);

    public async Task<ResourceTrashItem[]> GetTrashItemsAsync()
        => await db.ResourceTrashItems.OrderByDescending(x => x.TrashDate).ToArrayAsync();

    private IQueryable<ResourceGridItem> ApplyFilters(IQueryable<ResourceGridItem> query, Dictionary<string, object?> filters)
    {
        if (filters.TryGetValue("type", out var typeFilter) && typeFilter is string[] types && types.Length > 0)
            query = query.Where(x => types.Contains(x.Type));

        if (filters.TryGetValue("pubdate_min", out var minDateFilter) && minDateFilter is DateTime minDate)
            query = query.Where(x => x.PublicationDate == null || x.PublicationDate >= minDate);

        if (filters.TryGetValue("pubdate_max", out var maxDateFilter) && maxDateFilter is DateTime maxDate)
            query = query.Where(x => x.PublicationDate == null || x.PublicationDate <= maxDate);

        query = ApplyRelationFilter(query, filters, "tag_ids", "tag_filter_mode", "tag");
        query = ApplyRelationFilter(query, filters, "region_ids", "region_filter_mode", "region");

        if (filters.TryGetValue("resource_type_ids", out var rtFilter) && rtFilter is Guid[] typeIds && typeIds.Length > 0)
            query = query.Where(x => x.Type != "resource" || (x.TypeId != null && typeIds.Contains(x.TypeId.Value)));

        if (filters.TryGetValue("journal_ids", out var journalFilter) && journalFilter is Guid[] journalIds && journalIds.Length > 0)
            query = query.Where(x => x.Type != "resource" || (x.JournalId != null && journalIds.Contains(x.JournalId.Value)));

        return query;
    }

    private IQueryable<ResourceGridItem> ApplyRelationFilter(IQueryable<ResourceGridItem> query, Dictionary<string, object?> filters, string idsKey, string modeKey, string relationType)
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

    private static IQueryable<ResourceGridItem> ApplySorting(IQueryable<ResourceGridItem> query, string? sortBy, string? sortDirection)
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