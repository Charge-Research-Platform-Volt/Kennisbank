using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Data;

/// <summary>
/// Class to hold data for a search result on the grid
/// </summary>
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
    /// <summary>
    /// Searches all resources, persons and organisations using the given parameters
    /// </summary>
    /// <param name="pageIndex">The index of the page to retrieve</param>
    /// <param name="pageSize">The size of a page</param>
    /// <param name="search">The search query</param>
    /// <param name="sortBy">The attribute to sort by</param>
    /// <param name="sortDirection">The search direction (asc or desc)</param>
    /// <param name="filter_name">Filter to apply to the names</param>
    /// <param name="filter_type">Filter to apply to the type</param>
    /// <param name="filter_pubdate_max">The maximum publication date</param>
    /// <param name="filter_pubdate_min">The minimum publication date</param>
    /// <returns>The result of the search</returns>
    public async Task<GridSearchResult> SearchResourceGridAsync(
        int pageIndex,
        int pageSize,
        string? search = null,
        string? sortBy = null,
        string? sortDirection = null,
        string? filter_name = null,
        string? filter_type = null,
        string? filter_pubdate_min = null,
        string? filter_pubdate_max = null
    ) 
    {
        // Parse date filters
        DateTime? minDate = null;
        DateTime? maxDate = null;

        if (!string.IsNullOrEmpty(filter_pubdate_min) && DateTime.TryParse(filter_pubdate_min, out var parsedMin))
            minDate = DateTime.SpecifyKind(parsedMin, DateTimeKind.Utc);

        if (!string.IsNullOrEmpty(filter_pubdate_max) && DateTime.TryParse(filter_pubdate_max, out var parsedMax))
            maxDate = DateTime.SpecifyKind(parsedMax, DateTimeKind.Utc);
    
        // Store filters in dictionary
        Dictionary<string, object?> filters = new Dictionary<string, object?>
        {
            { "name", filter_name },
            { "type", filter_type },
            { "pubdate_min", minDate },
            { "pubdate_max", maxDate },
        };

        // If there is a search query, execute search
        if (!string.IsNullOrEmpty(search))
            return await ExecuteSearchQuery(pageIndex, pageSize, search, sortBy, sortDirection, filters);

        // If not, execute regular query
        else
            return await ExecuteRegularQuery(pageIndex, pageSize, sortBy, sortDirection, filters);
    }
    
    /// <summary>
    /// This function executes a search query.
    /// </summary>
    /// <param name="pageIndex">The index of the current page</param>
    /// <param name="pageSize">The size of the page</param>
    /// <param name="search">The search query</param>
    /// <param name="sortBy">The attribute to sort by</param>
    /// <param name="sortDirection">The direction to sort in (asc or desc)</param>
    /// <param name="filters">A dictionary of filters to apply</param>
    /// <returns>The search result</returns>
    private async Task<GridSearchResult> ExecuteSearchQuery(int pageIndex, int pageSize, string search, string? sortBy, string? sortDirection, Dictionary<string, object?> filters) 
    {
        // Build the query
        SearchQueryBuilder queryBuilder = new SearchQueryBuilder().AddSearchCondition(search).AddFilters(filters);

        // Get sql and parameters
        var (sql, parameters) = queryBuilder.BuildSearchQuery(sortBy, sortDirection, (pageIndex - 1) * pageSize, pageSize);
        var (countSql, countParameters) = queryBuilder.BuildCountQuery();

        // Execute the sql
        ResourceGridSearchResult[] searchResults = await database.ResourceGridSearchResults.FromSqlRaw(sql, parameters).ToArrayAsync();

        // Retrieve the total count
        int totalCount = await database.Database.SqlQueryRaw<int>(countSql, countParameters).SingleAsync();
        
        // Return result
        return CreateGridResult(searchResults.Select(x => x.ToResourceGridItem()).ToArray(), totalCount, pageIndex, pageSize, search, true);
    }
    
    /// <summary>
    /// This function executes a regular query, without search.
    /// </summary>
    /// <param name="pageIndex">The index of the current page</param>
    /// <param name="pageSize">The size of the page</param>
    /// <param name="sortBy">The attribute to sort by</param>
    /// <param name="sortDirection">The direction to sort in (asc or desc)</param>
    /// <param name="filters">A dictionary of filters to apply</param>
    /// <returns>The query result</returns>
    private async Task<GridSearchResult> ExecuteRegularQuery(int pageIndex, int pageSize, string? sortBy, string? sortDirection, Dictionary<string, object?> filters) 
    {
        IQueryable<ResourceGridItem> query = database.ResourceGridItems.AsQueryable();

        query = ApplyFilters(query, filters);
        query = ApplySorting(query, sortBy, sortDirection, hasSearch: false);

        int totalCount = await query.CountAsync();
        var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToArrayAsync();

        return CreateGridResult(items, totalCount, pageIndex, pageSize, null, false);
    }
    
    /// <summary>
    /// This function applies filters to a query
    /// </summary>
    /// <param name="query">The base query</param>
    /// <param name="filters">A dictionary of filters to apply</param>
    /// <returns>A new query with the filters applied</returns>
    private IQueryable<ResourceGridItem> ApplyFilters(IQueryable<ResourceGridItem> query, Dictionary<string, object?> filters) 
    {
        // Apply name filter
        if (filters.TryGetValue("name", out var nameFilter) && !string.IsNullOrEmpty(nameFilter?.ToString()))
            query = query.Where(x => EF.Functions.Like(x.Name, $"%{nameFilter}%"));

        // Apply type filter
        if (filters.TryGetValue("type", out var typeFilter) && typeFilter != null && !string.IsNullOrEmpty(typeFilter.ToString())) 
        {
            string[] allowedTypes = typeFilter.ToString().Split(',').Select(x => x.Trim()).ToArray();
        
            query = query.Where(x => allowedTypes.Contains(x.Type));
        }

        // Apply minimum publication date filter
        if (filters.TryGetValue("pubdate_min", out var minDateFilter) && minDateFilter is DateTime minDate)
            query = query.Where(x => x.PublicationDate >= minDate);

        // Apply maximum publication date filter
        if (filters.TryGetValue("pubdate_max", out var maxDateFilter) && maxDateFilter is DateTime maxDate)
            query = query.Where(x => x.PublicationDate <= maxDate);

        return query;
    }
    
    /// <summary>
    /// This function applies sorting to a query
    /// </summary>
    /// <param name="query">The base query</param>
    /// <param name="sortBy">The attribute to sort by</param>
    /// <param name="sortDirection">The direction to sort in (asc or desc)</param>
    /// <param name="hasSearch">Determines if it has a search query</param>
    /// <returns>A new query with the sorting applied</returns>
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
    
    /// <summary>
    /// This function constructs the search result
    /// </summary>
    /// <param name="items">The found items</param>
    /// <param name="totalCount">The total count of items available</param>
    /// <param name="pageIndex">The index of the current page</param>
    /// <param name="pageSize">The size of the page</param>
    /// <param name="searchTerm">The search query</param>
    /// <param name="isSearchResult">Determines if it was a search result</param>
    /// <returns>A GridSearchResult instance</returns>
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
    
    /// <summary>
    /// Helper class to build a search query
    /// </summary>
    private class SearchQueryBuilder 
    {
        private readonly List<string> whereConditions = new();
        private readonly List<object> parameters = new();
        private int paramIndex = 1;
        
        /// <summary>
        /// Adds a search condition to the query
        /// </summary>
        /// <param name="searchTerm">The search query</param>
        /// <returns>Itself with the search condition added</returns>
        public SearchQueryBuilder AddSearchCondition(string searchTerm) 
        {
            whereConditions.Add(@"""SearchVector"" @@ plainto_tsquery('english', {0})");
            parameters.Add(searchTerm);
            return this;
        }
        
        /// <summary>
        /// Adds filters to the query
        /// </summary>
        /// <param name="filters">A dictionary of filters to apply</param>
        /// <returns>Itself with the filters added</returns>
        public SearchQueryBuilder AddFilters(Dictionary<string, object?> filters) 
        {
            foreach ((string key, object? value) in filters) 
            {
                if (value == null) continue;
                
                switch (key.ToLower()) 
                {
                    case "name":
                        if (!string.IsNullOrEmpty(value.ToString())) 
                        {
                            whereConditions.Add($@"""Name"" ILIKE {{{paramIndex}}}");
                            parameters.Add($"%{value}%");
                            paramIndex++;
                        }
                        break;
                    
                    case "type":
                        if (!string.IsNullOrEmpty(value.ToString())) 
                        {
                            string[] allowedTypes = value.ToString().Split(',').Select(t => t.Trim()).ToArray();

                            if (allowedTypes.Length == 1) 
                            {
                                whereConditions.Add($@"""Type"" = {{{paramIndex}}}");
                                parameters.Add(value.ToString());
                                paramIndex++;
                            }
                            else 
                            {
                                List<string> typeParams = new List<string>();
                                foreach (string type in allowedTypes) 
                                {
                                    typeParams.Add($"{{{paramIndex}}}");
                                    parameters.Add(type);
                                    paramIndex++;
                                }

                                whereConditions.Add($@"""Type"" IN ({string.Join(", ", typeParams)})");
                            }
                        }
                        
                        break;
                        
                    case "pubdate_min":
                        if (value is DateTime minDate) 
                        {
                            whereConditions.Add($@"""PublicationDate"" >= {{{paramIndex}}}");
                            parameters.Add(minDate);
                            paramIndex++;
                        }
                        break;
                        
                    case "pubdate_max":
                        if (value is DateTime maxDate) 
                        {
                            whereConditions.Add($@"""PublicationDate"" <= {{{paramIndex}}}");
                            parameters.Add(maxDate);
                            paramIndex++;
                        }
                        break;
                }
            }

            return this;
        }
        
        /// <summary>
        /// Build a search query
        /// </summary>
        /// <param name="sortBy">The attribute to sort by</param>
        /// <param name="sortDirection">The direction to sort in (asc or desc)</param>
        /// <param name="offset">The offset (how many items to skip based on page index and size)</param>
        /// <param name="pageSize">The size of the page</param>
        /// <returns>A tuple with the sql and its parameters</returns>
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
        
        /// <summary>
        /// Builds a query to count the total amount of items
        /// </summary>
        /// <returns>A tuple with the sql and its parameters</returns>
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
        
        /// <summary>
        /// Builds a order by clause
        /// </summary>
        /// <param name="sortBy">The attribute to sort by</param>
        /// <param name="sortDirection">The direction to sort in (asc or desc)</param>
        /// <returns>Part of a SQL query containing the Order By clause</returns>
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