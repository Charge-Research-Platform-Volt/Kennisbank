using KnowledgeBank.Models;
using KnowledgeBank.Services;
using Microsoft.EntityFrameworkCore;
using Qdrant.Client.Grpc;
using static Qdrant.Client.Grpc.Conditions;

namespace KnowledgeBank.Data;

/// <summary>
/// DTO for making a grid request
/// </summary>
public class GridRequest
{
    public required int PageIndex { get; set; }
    public required int PageSize { get; set; }
    public string? SearchQuery { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public GridFilterOptions? FilterOptions { get; set; }
}

/// <summary>
/// DTO for setting filters in grid request
/// </summary>
public class GridFilterOptions
{
    public string[]? TypeFilter { get; set; }
    public string? PubdateMin { get; set; }
    public string? PubdateMax { get; set; }
    public string[]? TagFilter { get; set; }
    public string? TagFilterMode { get; set; } = "any";
    public string[]? RegionFilter { get; set; }
    public string? RegionFilterMode { get; set; } = "any";
}


public class GridSearchTemplate
{
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public string? SearchTerm { get; set; }
    public bool IsSearchResult { get; set; }
}

/// <summary>
/// Class to hold data for a search result on the grid
/// </summary>
public class GridSearchResult : GridSearchTemplate
{
    public required ResourceGridItem[] Items { get; set; }
}

public class GridSearchResultWithChunks : GridSearchTemplate
{
    public required ResourceGridItemWithChunks[] Items { get; set; }
}


public partial class ResourceManager
{
    /// <summary>
    /// Searches all resources, persons and organisations using the given parameters
    /// </summary>
    /// <param name="request">The request DTO</param>
    /// <returns>The result of the search</returns>
    public async Task<GridSearchTemplate> SearchResourceGridAsync(GridRequest request)
    {
        // Parse date filters
        DateTime? minDate = null;
        DateTime? maxDate = null;

        if (!string.IsNullOrEmpty(request.FilterOptions?.PubdateMin) && DateTime.TryParse(request.FilterOptions?.PubdateMin, out var parsedMin))
            minDate = DateTime.SpecifyKind(parsedMin, DateTimeKind.Utc);

        if (!string.IsNullOrEmpty(request.FilterOptions?.PubdateMax) && DateTime.TryParse(request.FilterOptions?.PubdateMax, out var parsedMax))
            maxDate = DateTime.SpecifyKind(parsedMax, DateTimeKind.Utc);

        // Parse GUID filters
        Guid[] tagGuids = StringToGuidArray(request.FilterOptions?.TagFilter);
        Guid[] regionGuids = StringToGuidArray(request.FilterOptions?.RegionFilter);

        // Store filters in dictionary
        // Add the filter here and add functionality both in ApplyFilters (EF Core) and AddFilters (Raw SQL)
        Dictionary<string, object?> filters = new Dictionary<string, object?>
            {
                { "type", request.FilterOptions?.TypeFilter },
                { "pubdate_min", minDate },
                { "pubdate_max", maxDate },
                { "tag_ids", tagGuids },
                { "tag_filter_mode", request.FilterOptions?.TagFilterMode },
                { "region_ids", regionGuids },
                { "region_filter_mode", request.FilterOptions?.RegionFilterMode },
            };

        // If there is a search query, execute search
        if (!string.IsNullOrEmpty(request.SearchQuery))
            return await ExecuteSearchQuery(request.PageIndex, request.PageSize, request.SearchQuery, request.SortBy, request.SortDirection, filters);

        // If not, execute regular query
        else
            return await ExecuteRegularQuery(request.PageIndex, request.PageSize, request.SortBy, request.SortDirection, filters);
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
    private async Task<GridSearchTemplate> ExecuteSearchQuery(int pageIndex, int pageSize, string search, string? sortBy, string? sortDirection, Dictionary<string, object?> filters)
    {
        try
        {
            _logger.Information("Executing search using Qdrant");
            return await ExecuteSearchQdrant(pageIndex, pageSize, search, filters);
        }
        catch (Exception)
        {
            _logger.Warning("Qdrant search failed, falling back to SQL search");

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
    }




    /// Executes a semantic search using Qdrant vector database with fallback to text matching.
    /// <param name="pageIndex">The current page index for pagination (1-based).</param>
    /// <param name="pageSize">The number of items to return per page.</param>
    /// <param name="search">The search query string to find relevant resources.</param>
    /// <param name="filters">Additional filters to apply to the search results as key-value pairs.</param>
    /// <returns>
    /// A <see cref="GridSearchResult"/> containing the paginated search results with resource items,
    /// total count, and pagination metadata. Returns empty result if no matches are found.
    /// </returns>
    /// <remarks>
    /// The method first attempts to perform semantic search by generating embeddings from the search query.
    /// If embedding generation fails, it falls back to text-based matching using the search term.
    /// Results are grouped by resourceId.
    /// </remarks>
    private async Task<GridSearchResultWithChunks> ExecuteSearchQdrant(int pageIndex, int pageSize, string search, Dictionary<string, object?> filters)
    {
        IReadOnlyList<PointGroup> searchresults;

        try
        {
            _logger.Information("Executing semantic search using Qdrant");

            // Generate embedding for the search query
            float[] embeddingData = await _ragSystem.GenerateEmbedding(search);

            // Perform vector search in Qdrant
            searchresults = await _ragSystem.QdrantClient.QueryGroupsAsync(
                _ragSystem.CollectionName,
                groupBy: "resourceId",
                query: embeddingData,
                limit: 1000,
                groupSize: 4
            );

            Console.WriteLine($"Qdrant search results: {searchresults}");
        }
        catch (Exception)
        {
            _logger.Error("Qdrant embedding generation failed, falling back to Qdrant text-based search");

            // If embedding generation fails, fallback to text-based search
            searchresults = await _ragSystem.QdrantClient.QueryGroupsAsync(
                _ragSystem.CollectionName,
                groupBy: "resourceId",
                filter: MatchText("chunkText", search),
                limit: 1000,
                groupSize: 4
            );
        }

        try
        {
            _logger.Information("Processing Qdrant search results");

            // Extract resource IDs and chunks from search results in a single pass
            var resourceData = new Dictionary<Guid, List<string>>(searchresults.Count);
            foreach (var result in searchresults)
            {
                var resourceId = Guid.Parse(result.Id.StringValue);
                var chunks = new List<string>(result.Hits.Count);
                foreach (var hit in result.Hits)
                {
                    chunks.Add(CustomPayload.FromPayload(hit.Payload).ChunkText);
                }
                resourceData[resourceId] = chunks;
            }

            // Early return for empty results
            if (resourceData.Count == 0)
                return new GridSearchResultWithChunks
                {
                    Items = [],
                    TotalCount = 0,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    SearchTerm = search,
                    IsSearchResult = true
                };

            // Get resource IDs as array
            var resourceIds = resourceData.Keys.ToArray();

            // Query database for resources matching the IDs from vector search results.
            var query = database.ResourceGridItems.Where(x => resourceIds.Contains(x.Id));

            // Apply filters to the query
            query = ApplyFilters(query, filters);

            // Execute the query to get all filtered results, Set total count to the number of filtered results
            ResourceGridItem[] allItems = await query.ToArrayAsync();
            int totalCount = allItems.Length;

            // Create a lookup for resource positions based on the original search results
            var positionLookup = new Dictionary<Guid, int>(resourceData.Count);
            int position = 0;
            foreach (var kvp in resourceData)
                positionLookup[kvp.Key] = position++;

            // Sort by semantic relevance, then apply pagination and add chunks
            var pagedItems = allItems
                .OrderBy(x => positionLookup[x.Id])
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new ResourceGridItemWithChunks
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    PublicationDate = item.PublicationDate,
                    Type = item.Type,
                    FileType = item.FileType,
                    CreationDate = item.CreationDate,
                    Chunks = resourceData[item.Id]
                }).ToArray();

            return new GridSearchResultWithChunks
            {
                Items = pagedItems,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                SearchTerm = search,
                IsSearchResult = true
            };
        }
        catch (Exception error)
        {
            _logger.Error("Error while processing Qdrant search results: {ErrorMessage}. StackTrace: {StackTrace}", error.Message, error.StackTrace);
            throw new InvalidOperationException($"An error occurred while processing the search results: {error.Message}", error);
        }
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
    public IQueryable<ResourceGridItem> ApplyFilters(IQueryable<ResourceGridItem> query, Dictionary<string, object?> filters)
    {
        // Apply type filter
        if (filters.TryGetValue("type", out var typeFilter) && typeFilter != null && ((string[])typeFilter).Length > 0)
            query = query.Where(x => ((string[])typeFilter).Contains(x.Type));

        // Apply minimum publication date filter
        if (filters.TryGetValue("pubdate_min", out var minDateFilter) && minDateFilter is DateTime minDate)
            query = query.Where(x => x.PublicationDate >= minDate);

        // Apply maximum publication date filter
        if (filters.TryGetValue("pubdate_max", out var maxDateFilter) && maxDateFilter is DateTime maxDate)
            query = query.Where(x => x.PublicationDate <= maxDate);

        // Apply tag filter
        query = ApplyRelationFilter(query, filters, "tag_ids", "tag_filter_mode", "tag");

        // Apply region filter
        query = ApplyRelationFilter(query, filters, "region_ids", "region_filter_mode", "region");

        return query;
    }

    private IQueryable<ResourceGridItem> ApplyRelationFilter(IQueryable<ResourceGridItem> query, Dictionary<string, object?> filters, string idsKey, string modeKey, string relationType)
    {
        if (!filters.TryGetValue(idsKey, out var idsFilter) || idsFilter is not Guid[] ids || ids.Length == 0)
            return query;

        string filterMode = filters.TryGetValue(modeKey, out var mode) && mode is string modeStr ? modeStr : "any";

        // Resource must have ALL specified IDs
        if (filterMode.ToLower() == "all")
        {
            foreach (Guid id in ids)
            {
                // Tag filter
                if (relationType == "tag")
                    query = query.Where(x => x.Type != "resource" || database.ResourceTagRelations.Any(rt => rt.TagId == id && rt.ResourceId == x.Id));

                // Region filter
                if (relationType == "region")
                    query = query.Where(x => x.Type != "resource" || database.ResourceRegionRelations.Any(rr => rr.RegionId == id && rr.ResourceId == x.Id));
            }
        }

        // Resource must have ANY of the specified IDs
        else
        {
            // Tag filter
            if (relationType == "tag")
                query = query.Where(x => x.Type != "resource" || database.ResourceTagRelations.Any(rt => ids.Contains(rt.TagId) && rt.ResourceId == x.Id));

            // Region filter
            if (relationType == "region")
                query = query.Where(x => x.Type != "resource" || database.ResourceRegionRelations.Any(rr => ids.Contains(rr.RegionId) && rr.ResourceId == x.Id));
        }

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
            _ => query.OrderByDescending(x => x.CreationDate)
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
        /// Adds a search condition to the query. This is fuzzy and will handle typos and partial matches
        /// </summary>
        /// <param name="searchTerm">The search query</param>
        /// <returns>Itself with the search condition added</returns>
        public SearchQueryBuilder AddSearchCondition(string searchTerm)
        {
            List<string> searchConditions =
            [
                // Strategy 1: Exact phrase match (highest priority)
                @"""SearchVector"" @@ phraseto_tsquery('english', {0})",
                
                // Strategy 2: All words must be present (websearch_to_tsquery handles quotes, AND, OR, etc.)
                @"""SearchVector"" @@ websearch_to_tsquery('english', {0})",
            ];

            // Strategy 3: Prefix matching for partial words
            string[] words = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0)
            {
                // Create prefix queries for each word
                string prefixQuery = string.Join(" & ", words.Select(w => $"{w}:*"));
                searchConditions.Add(@"""SearchVector"" @@ to_tsquery('english', {1})");
                parameters.Add(prefixQuery);
                paramIndex++;
            }

            // Strategy 4: Fuzzy matching using similarity
            searchConditions.Add(@"(
                similarity(""Name"", {" + paramIndex + @"}) > 0.3 OR
                similarity(coalesce(""Description"", ''), {" + paramIndex + @"}) > 0.2
            )");
            parameters.Add(searchTerm);
            paramIndex++;

            // Strategy 5: ILIKE for substring matching
            searchConditions.Add(@"(
                ""Name"" ILIKE {" + paramIndex + @"} OR
                coalesce(""Description"", '') ILIKE {" + paramIndex + @"}
            )");
            parameters.Add($"%{searchTerm}%");
            paramIndex++;

            parameters.Add($"{searchTerm}%");
            paramIndex++;

            whereConditions.Add($"({string.Join(" OR ", searchConditions)})");
            parameters.Insert(0, searchTerm);
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
                    case "type":
                        if (value != null && ((string[])value).Length > 0)
                        {
                            string[] allowedTypes = (string[])value;

                            if (allowedTypes.Length == 1)
                            {
                                whereConditions.Add($@"""Type"" = {{{paramIndex}}}");
                                parameters.Add(allowedTypes[0]);
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

                    case "tag_ids":
                        AddRelationFilter(value, filters, "tag_filter_mode", "resource-tag", "tag-id");
                        break;

                    case "region_ids":
                        AddRelationFilter(value, filters, "region_filter_mode", "resource-region", "region-id");
                        break;
                }
            }

            return this;
        }

        private void AddRelationFilter(object? value, Dictionary<string, object?> filters, string modeKey, string tableName, string columnName)
        {
            if (value is not Guid[] ids || ids.Length == 0) return;

            string filterMode = "any";
            if (filters.TryGetValue(modeKey, out var mode) && mode is string modeStr)
                filterMode = modeStr.ToLower();

            // Resource must have ALL specified IDs
            if (filterMode == "all")
            {
                List<string> idParams = new List<string>();
                foreach (Guid id in ids)
                {
                    idParams.Add($"{{{paramIndex}}}");
                    parameters.Add(id);
                    paramIndex++;
                }

                whereConditions.Add($@"(
                    ""Type"" != 'resource' OR
                    (SELECT COUNT(*) FROM ""{tableName}""
                    WHERE ""resource-id"" = ""Id"" AND ""{columnName}"" IN ({string.Join(", ", idParams)})) = {{{paramIndex}}}
                )");
                parameters.Add(ids.Length);
                paramIndex++;
            }

            // Resource must have ANY of the specified IDs
            else
            {
                List<string> idParams = new List<string>();
                foreach (Guid id in ids)
                {
                    idParams.Add($"{{{paramIndex}}}");
                    parameters.Add(id);
                    paramIndex++;
                }

                whereConditions.Add($@"(
                    ""Type"" != 'resource' OR
                    EXISTS (SELECT 1 FROM ""{tableName}""
                        WHERE ""resource-id"" = ""Id"" AND ""{columnName}"" IN ({string.Join(", ", idParams)}))
                )");
            }
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
                SELECT ""Id"", ""Name"", ""Description"", ""PublicationDate"", ""Type"", ""FileType"", ""CreationDate"",
                    (
                        -- Exact phrase match gets highest score
                        CASE WHEN ""SearchVector"" @@ phraseto_tsquery('english', {{0}}) THEN 10.0 ELSE 0.0 END +
                        
                        -- Full-text search relevance
                        CASE WHEN ""SearchVector"" @@ websearch_to_tsquery('english', {{0}})
                            THEN ts_rank(""SearchVector"", websearch_to_tsquery('english', {{0}})) * 5.0
                            ELSE 0.0 END +
                        
                        -- Prefix matching
                        CASE WHEN ""SearchVector"" @@ to_tsquery('english', {{1}})
                            THEN ts_rank(""SearchVector"", to_tsquery('english', {{1}}))
                            ELSE 0.0 END +
                        
                        -- Name similarity bonus
                        similarity(""Name"", {{2}}) * 2.0 +
                        
                        -- Name starts with search term bonus
                        CASE WHEN ""Name"" ILIKE {{4}} THEN 1.0 ELSE 0.0 END
                    ) as ""Relevance""
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
                _ => @"""Relevance"" DESC, ""CreationDate"" DESC"
            };
        }
    }
}