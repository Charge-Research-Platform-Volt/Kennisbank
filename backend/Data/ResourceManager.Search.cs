using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace KnowledgeBank.Data;

public class GridSearchResult 
{
    public required ResourceGridItem[] Items { get; set; }
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public string? SearchTerm { get; set; }
    public bool IsSearchResult { get; set; }
}

public partial class ResourceManager 
{
    public async Task<GridSearchResult> SearchResourceGridAsync(
        int pageIndex,
        int pageSize,
        string? search = null,
        string? sortBy = null,
        string? sortDirection = null,
        string? filter_name = null,
        string? filter_type = null
    ) 
    {
        // Store filters in dictionary
        Dictionary<string, string?> filters = new Dictionary<string, string?>
        {
            { "name", filter_name },
            { "type", filter_type }
        };

        // If there is a search query, execute search
        if (!string.IsNullOrEmpty(search))
            return await ExecuteSearchQuery(pageIndex, pageSize, search, sortBy, sortDirection, filters);

        // If not, execute regular query
        else
            return await ExecuteRegularQuery(pageIndex, pageSize, sortBy, sortDirection, filters);
    }
    
    private async Task<GridSearchResult> ExecuteSearchQuery(int pageIndex, int pageSize, string search, string? sortBy, string? sortDirection, Dictionary<string, string?> filters) 
    {
        SearchQueryBuilder queryBuilder = new SearchQueryBuilder().AddSearchCondition(search).AddFilters(filters);

        var (sql, parameters) = queryBuilder.BuildSearchQuery(sortBy, sortDirection, (pageIndex - 1) * pageSize, pageSize);
        var (countSql, countParameters) = queryBuilder.BuildCountQuery();

        ResourceGridSearchResult[] searchResults = await database.ResourceGridSearchResults.FromSqlRaw(sql, parameters).ToArrayAsync();

        int totalCount = await database.Database.SqlQueryRaw<int>(countSql, countParameters).SingleAsync();

        return CreateGridResult(searchResults.Select(x => x.ToResourceGridItem()).ToArray(), totalCount, pageIndex, pageSize, search, true);
    }
    
    private async Task<GridSearchResult> ExecuteRegularQuery(int pageIndex, int pageSize, string? sortBy, string? sortDirection, Dictionary<string, string?> filters) 
    {
        IQueryable<ResourceGridItem> query = database.ResourceGridItems.AsQueryable();

        query = ApplyFilters(query, filters);
        query = ApplySorting(query, sortBy, sortDirection, hasSearch: false);

        int totalCount = await query.CountAsync();
        var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToArrayAsync();

        return CreateGridResult(items, totalCount, pageIndex, pageSize, null, false);
    }
    
    private IQueryable<ResourceGridItem> ApplyFilters(IQueryable<ResourceGridItem> query, Dictionary<string, string?> filters) 
    {
        if (filters.TryGetValue("name", out var nameFilter) && !string.IsNullOrEmpty(nameFilter))
            query = query.Where(x => EF.Functions.Like(x.Name, $"%{nameFilter}%"));

        if (filters.TryGetValue("type", out var typeFilter) && !string.IsNullOrEmpty(typeFilter))
            query = query.Where(x => x.Type == typeFilter.ToString());

        return query;
    }
    
    private IQueryable<ResourceGridItem> ApplySorting(IQueryable<ResourceGridItem> query, string? sortBy, string? sortDirection, bool hasSearch) 
    {
        if (hasSearch && string.IsNullOrEmpty(sortBy))
            return query;

        return (sortBy?.ToLower(), sortDirection) switch
        {   
            // Name sorting
            ("name", "desc") => query.OrderByDescending(x => x.Name),
            ("name", _) => query.OrderBy(x => x.Name),
            
            // Type sorting
            ("type", "desc") => query.OrderByDescending(x => x.Type),
            ("type", _) => query.OrderBy(x => x.Type),

            // Publication date sorting
            ("publicationdate", "desc") => query.OrderByDescending(x => x.PublicationDate),
            ("publicationdate", _) => query.OrderBy(x => x.PublicationDate),

            // Creation date sorting
            ("creationdate", "desc") => query.OrderByDescending(x => x.CreationDate),
            ("creationdate", _) => query.OrderBy(x => x.CreationDate),

            // Default
            _ => query.OrderBy(x => x.CreationDate)
        };
    }
    
    private static GridSearchResult CreateGridResult(ResourceGridItem[] items, int totalCount, int pageIndex, int pageSize, string? searchTerm, bool isSearchResult) 
    {
        return new GridSearchResult
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            IsSearchResult = isSearchResult
        };
    }
    
    private class SearchQueryBuilder 
    {
        private readonly List<string> whereConditions = new();
        private readonly List<object> parameters = new();
        private int paramIndex = 1;
        
        public SearchQueryBuilder AddSearchCondition(string searchTerm) 
        {
            whereConditions.Add(@"""SearchVector"" @@ plainto_tsquery('english', {0})");
            parameters.Add(searchTerm);
            return this;
        }
        
        public SearchQueryBuilder AddFilters(Dictionary<string, string?> filters) 
        {
            foreach ((string key, string? value) in filters) 
            {
                if (string.IsNullOrEmpty(value)) continue;
                
                switch (key.ToLower()) 
                {
                    case "name":
                        whereConditions.Add($@"""Name"" ILIKE {{{paramIndex}}}");
                        parameters.Add($"%{value}%");
                        break;
                    
                    case "type":
                        whereConditions.Add($@"""Type"" = {{{paramIndex}}}");
                        parameters.Add(value.ToString());
                        break;
                }

                paramIndex++;
            }

            return this;
        }
        
        public (string sql, object[] parameters) BuildSearchQuery(string? sortBy, string? sortDirection, int offset, int pageSize) 
        {
            string whereClause = string.Join(" AND ", whereConditions);
            string orderByClause = BuildOrderByClause(sortBy, sortDirection);

            object[] allParams = parameters.Concat(new object[] { offset, pageSize }).ToArray();

            string sql = $@"
                SELECT ""Id"", ""Name"", ""PublicationDate"", ""Type"", ""FileType"", ""CreationDate"",
                    ts_rank(""SearchVector"", plainto_tsquery('english', {{0}})) as ""Relevance""
                FROM ResourceGridView
                WHERE {whereClause}
                ORDER BY {orderByClause}
                OFFSET {{{paramIndex}}} ROWS
                FETCH NEXT {{{paramIndex + 1}}} ROWS ONLY
            ";

            return (sql, allParams);
        }
        
        public (string sql, object[] parameters) BuildCountQuery() 
        {
            string whereClause = string.Join(" AND ", whereConditions);

            string sql = $@"
                SELECT COUNT(*) as ""Value""
                FROM ResourceGridView
                WHERE {whereClause}
            ";

            return (sql, parameters.ToArray());
        }
        
        private string BuildOrderByClause(string? sortBy, string? sortDirection) 
        {
            string direction = sortDirection?.ToLower() == "desc" ? "DESC" : "ASC";

            return sortBy?.ToLower() switch
            {
                "name" => $@"""Name"" {direction}",
                "type" => $@"""Type"" {direction}",
                "publicationDate" => $@"""PublicationDate"" {direction}",
                "creationDate" => $@"""CreationDate"" {direction}",
                _ => $@"ts_rank(""SearchVector"", plainto_tsquery('english', {{0}})) DESC, ""CreationDate"" DESC"
            };
        }
    }
}