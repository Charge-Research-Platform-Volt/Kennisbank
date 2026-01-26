using System.Diagnostics;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.Search.Models;
using KnowledgeBank.Services.Vector;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace KnowledgeBank.Services.Search;

/// <summary>
/// Main orchestrator for hybrid search operations.
/// Combines semantic, text-based, and Postgres full-text search with intelligent fusion.
/// </summary>
public class HybridSearchService
{
    private readonly Serilog.ILogger _logger;
    private readonly RAGSystem _ragSystem;
    private readonly IDbContextFactory<DatabaseContext> _dbFactory;
    private readonly HybridSearchConfig _config;
    private readonly SearchFusionEngine _fusionEngine;
    private readonly SearchRanker _ranker;
    private readonly IVectorStore _vectorStore;

    public HybridSearchService(
        RAGSystem ragSystem,
        IDbContextFactory<DatabaseContext> dbFactory,
        HybridSearchConfig config,
        IVectorStore vectorStore)
    {
        _ragSystem = ragSystem;
        _dbFactory = dbFactory;
        _config = config;
        _fusionEngine = new SearchFusionEngine(config);
        _ranker = new SearchRanker(config);
        _logger = Log.ForContext<HybridSearchService>();
        _vectorStore = vectorStore;

        // Validate configuration
        _config.Validate();
    }

    /// <summary>
    /// Executes a hybrid search across multiple search sources.
    /// </summary>
    /// <param name="searchQuery">The search query string</param>
    /// <param name="pageIndex">Page index (1-based)</param>
    /// <param name="pageSize">Number of results per page</param>
    /// <param name="filters">Dictionary of filters to apply</param>
    /// <returns>Hybrid search result with ranked items and metadata</returns>
    public async Task<HybridSearchResult> SearchAsync(
        string searchQuery,
        int pageIndex,
        int pageSize,
        Dictionary<string, object?> filters)
    {
        var stopwatch = Stopwatch.StartNew();
        var metadata = new SearchMetadata();

        _logger.Information("Starting hybrid search for query: '{Query}'", searchQuery);

        try
        {
            // Calculate how many candidates to fetch from each source
            int candidateLimit = Math.Min(
                pageSize * _config.CandidateMultiplier,
                _config.MaxCandidatesPerSource);

            // Execute parallel searches
            var candidates = await ExecuteParallelSearches(searchQuery, candidateLimit, metadata);

            if (candidates.Count == 0)
            {
                _logger.Warning("No candidates found from any search source");
                return CreateEmptyResult(searchQuery, pageIndex, pageSize, metadata, stopwatch);
            }

            _logger.Information("Retrieved {Count} total candidates before fusion", candidates.Count);

            // Fuse results from different sources
            var fusedCandidates = _fusionEngine.FuseResults(candidates);
            metadata.FusedResultCount = fusedCandidates.Count;

            // Load full item data BEFORE re-ranking so name boosting works
            var candidatesWithItems = await LoadItemDataAsync(fusedCandidates);

            // Re-rank results (now Item is populated so name boosting works!)
            var rerankedCandidates = _ranker.ReRank(candidatesWithItems, searchQuery);

            // Filter by relevance threshold
            var filteredCandidates = _ranker.FilterByRelevance(rerankedCandidates);
            metadata.FilteredResultCount = filteredCandidates.Count;

            // Apply additional filters (type, date, tags, etc.)
            var finalCandidates = await ApplyFiltersAsync(filteredCandidates, filters);
            int totalCount = finalCandidates.Count;

            // Paginate results
            var pagedCandidates = finalCandidates
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Convert to result items
            var resultItems = pagedCandidates
                .Select(c => new ResourceGridItemWithScore
                {
                    Id = c.Item!.Id,
                    Name = c.Item.Name,
                    Description = c.Item.Description,
                    PublicationDate = c.Item.PublicationDate,
                    Type = c.Item.Type,
                    FileType = c.Item.FileType,
                    CreationDate = c.Item.CreationDate,
                    RelevanceScore = c.FinalScore,
                    Provenance = c.Provenance,
                    MatchedChunks = c.MatchedChunks
                })
                .ToArray();

            // Calculate average score
            metadata.AverageScore = resultItems.Any()
                ? resultItems.Average(i => i.RelevanceScore)
                : 0f;

            stopwatch.Stop();
            metadata.SearchDuration = stopwatch.Elapsed;

            _logger.Information(
                "Hybrid search complete. Results: {ResultCount}/{TotalCount}, Duration: {Duration}ms, Avg Score: {AvgScore:F3}",
                resultItems.Length, totalCount, stopwatch.ElapsedMilliseconds, metadata.AverageScore);

            return new HybridSearchResult
            {
                Items = resultItems,
                TotalCount = totalCount,
                DurationInMs = stopwatch.ElapsedMilliseconds,
                PageIndex = pageIndex,
                PageSize = pageSize,
                SearchTerm = searchQuery,
                IsSearchResult = true,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "Hybrid search failed for query: '{Query}'", searchQuery);
            metadata.Warnings.Add($"Search failed: {ex.Message}");
            metadata.SearchDuration = stopwatch.Elapsed;

            return CreateEmptyResult(searchQuery, pageIndex, pageSize, metadata, stopwatch);
        }
    }

    /// <summary>
    /// Executes searches across multiple sources in parallel.
    /// </summary>
    private async Task<List<SearchCandidate>> ExecuteParallelSearches(
        string searchQuery,
        int candidateLimit,
        SearchMetadata metadata)
    {
        var allCandidates = new List<SearchCandidate>();

        // Create tasks for parallel execution
        var tasks = new List<Task<List<SearchCandidate>>>();

        // Semantic search
        tasks.Add(ExecuteSemanticSearchAsync(searchQuery, candidateLimit, metadata));

        // Text-based search (pgvector trigram)
        tasks.Add(ExecuteTextSearchAsync(searchQuery, candidateLimit, metadata));

        // Postgres full-text search (on metadata fields)
        if (_config.UsePostgresFullTextSearch)
        {
            tasks.Add(ExecutePostgresSearchAsync(searchQuery, candidateLimit, metadata));
        }

        // Wait for all searches to complete
        var results = await Task.WhenAll(tasks);

        // Combine all results
        foreach (var result in results)
        {
            allCandidates.AddRange(result);
        }

        return allCandidates;
    }

    /// <summary>
    /// Executes semantic (vector) search using pgvector.
    /// </summary>
    private async Task<List<SearchCandidate>> ExecuteSemanticSearchAsync(
        string searchQuery,
        int limit,
        SearchMetadata metadata)
    {
        var candidates = new List<SearchCandidate>();

        try
        {
            _logger.Debug("Executing semantic search");

            // Generate embedding for the search query
            float[] embeddingData = await _ragSystem.GenerateEmbedding(searchQuery);

            // Perform vector search using pgvector
            var searchResults = await _vectorStore.SemanticSearchAsync(embeddingData, limit, _config.SemanticScoreThreshold);

            // Group by resource ID and take top chunks per resource
            var grouped = searchResults.GroupBy(r => r.ResourceId).Take(limit);

            int rank = 1;
            foreach (var group in grouped)
            {
                var chunks = group.OrderByDescending(r => r.Score).Take(_config.MaxChunksPerResource).Select(r => r.ChunkText).ToList();

                float score = group.Max(r => r.Score);

                candidates.Add(new SearchCandidate
                {
                    ResourceId = group.Key,
                    Provenance = new SearchProvenance
                    {
                        SemanticScore = score
                    },
                    MatchedChunks = chunks,
                    SemanticRank = rank++
                });
            }

            metadata.SemanticResultCount = candidates.Count;
            metadata.UsedSemanticSearch = true;

            _logger.Information("Semantic search returned {Count} results", candidates.Count);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Semantic search failed");
            metadata.Warnings.Add($"Semantic search failed: {ex.Message}");
        }

        return candidates;
    }

    /// <summary>
    /// Executes text-based keyword search using pg_trgm trigram matching.
    /// </summary>
    private async Task<List<SearchCandidate>> ExecuteTextSearchAsync(
        string searchQuery,
        int limit,
        SearchMetadata metadata)
    {
        var candidates = new List<SearchCandidate>();

        try
        {
            _logger.Debug("Executing text-based search");

            var searchResults = await _vectorStore.TextSearchAsync(searchQuery, limit * _config.MaxChunksPerResource);

            // Group by resource ID
            var grouped = searchResults.GroupBy(r => r.ResourceId).Take(limit);

            int rank = 1;
            foreach (var group in grouped)
            {
                var chunks = group.OrderByDescending(r => r.Score).Take(_config.MaxChunksPerResource).Select(r => r.ChunkText).ToList();

                float score = group.Max(r => r.Score);

                candidates.Add(new SearchCandidate
                {
                    ResourceId = group.Key,
                    Provenance = new SearchProvenance
                    {
                        TextScore = score
                    },
                    MatchedChunks = chunks,
                    TextRank = rank++
                });
            }

            metadata.TextResultCount = candidates.Count;
            metadata.UsedTextSearch = true;

            _logger.Information("Text search returned {Count} results", candidates.Count);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Text-based search failed");
            metadata.Warnings.Add($"Text search failed: {ex.Message}");
        }

        return candidates;
    }

    /// <summary>
    /// Executes full-text search using Postgres.
    /// </summary>
    private async Task<List<SearchCandidate>> ExecutePostgresSearchAsync(
        string searchQuery,
        int limit,
        SearchMetadata metadata)
    {
        var candidates = new List<SearchCandidate>();

        try
        {
            _logger.Debug("Executing Postgres full-text search");

            // Build SQL query for full-text search
            string sql = BuildPostgresSearchQuery(searchQuery, limit);

            // Format query for tsquery prefix search - join words with " & " and add :* to each word
            string[] words = searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string prefixQuery = string.Join(" & ", words.Select(w => $"{w}:*"));

            await using var db = await _dbFactory.CreateDbContextAsync();
            var searchResults = await db.ResourceGridSearchResults
                .FromSqlRaw(sql, searchQuery, prefixQuery, searchQuery, $"%{searchQuery}%", searchQuery + "%")
                .ToListAsync();

            int rank = 1;
            foreach (var result in searchResults)
            {
                candidates.Add(new SearchCandidate
                {
                    ResourceId = result.Id,
                    Item = result.ToResourceGridItem(),
                    Provenance = new SearchProvenance
                    {
                        PostgresScore = result.Relevance
                    },
                    PostgresRank = rank++
                });
            }

            metadata.PostgresResultCount = candidates.Count;
            metadata.UsedPostgresSearch = true;

            _logger.Information("Postgres search returned {Count} results", candidates.Count);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Postgres search failed");
            metadata.Warnings.Add($"Postgres search failed: {ex.Message}");
        }

        return candidates;
    }

    /// <summary>
    /// Builds the Postgres full-text search SQL query.
    /// </summary>
    private string BuildPostgresSearchQuery(string searchQuery, int limit)
    {
        return $@"
            SELECT ""Id"", ""Name"", ""Description"", ""PublicationDate"", ""PublicationDatePrecision"", ""Type"", ""FileType"", ""CreationDate"",
                (
                    CASE WHEN ""SearchVector"" @@ phraseto_tsquery('english', {{0}}) THEN 10.0 ELSE 0.0 END +
                    CASE WHEN ""SearchVector"" @@ websearch_to_tsquery('english', {{0}})
                        THEN ts_rank(""SearchVector"", websearch_to_tsquery('english', {{0}})) * 5.0
                        ELSE 0.0 END +
                    CASE WHEN ""SearchVector"" @@ to_tsquery('english', {{1}})
                        THEN ts_rank(""SearchVector"", to_tsquery('english', {{1}}))
                        ELSE 0.0 END +
                    CASE WHEN ""Name"" ILIKE {{4}} THEN 1.0 ELSE 0.0 END
                ) as ""Relevance""
            FROM ResourceGridView
            WHERE (
                ""SearchVector"" @@ phraseto_tsquery('english', {{2}}) OR
                ""SearchVector"" @@ websearch_to_tsquery('english', {{2}}) OR
                ""SearchVector"" @@ to_tsquery('english', {{1}}) OR
                ""Name"" ILIKE {{3}} OR
                coalesce(""Description"", '') ILIKE {{3}}
            )
            ORDER BY ""Relevance"" DESC
            LIMIT {limit}
        ";
    }

    /// <summary>
    /// Loads full item data for search candidates from the database.
    /// </summary>
    private async Task<List<SearchCandidate>> LoadItemDataAsync(List<SearchCandidate> candidates)
    {
        if (candidates.Count == 0)
            return candidates;

        // Extract resource IDs that don't already have Item data
        var idsToLoad = candidates
            .Where(c => c.Item == null)
            .Select(c => c.ResourceId)
            .ToArray();

        if (idsToLoad.Length == 0)
            return candidates; // All items already loaded

        _logger.Debug("Loading item data for {Count} candidates", idsToLoad.Length);

        // Query database for full resource data
        await using var db = await _dbFactory.CreateDbContextAsync();
        var resourceItems = await db.ResourceGridItems
            .Where(x => idsToLoad.Contains(x.Id))
            .ToListAsync();

        // Create lookup for efficient matching
        var itemLookup = resourceItems.ToDictionary(item => item.Id);

        // Populate Item property for candidates
        foreach (var candidate in candidates)
        {
            if (candidate.Item == null && itemLookup.TryGetValue(candidate.ResourceId, out var item))
            {
                candidate.Item = item;
            }
        }

        return candidates;
    }

    /// <summary>
    /// Applies filters to search candidates and loads full resource data.
    /// </summary>
    private async Task<List<SearchCandidate>> ApplyFiltersAsync(
        List<SearchCandidate> candidates,
        Dictionary<string, object?> filters)
    {
        if (candidates.Count == 0)
            return candidates;

        // Extract resource IDs
        var resourceIds = candidates.Select(c => c.ResourceId).ToArray();

        // Query database for full resource data
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.ResourceGridItems.Where(x => resourceIds.Contains(x.Id));

        // Apply filters (same logic as ResourceManager)
        query = ApplyFilters(query, db, filters);

        // Execute query
        var resourceItems = await query.ToListAsync();

        // Create lookup for efficient matching
        var itemLookup = resourceItems.ToDictionary(item => item.Id);

        // Update candidates with full item data, filter out items that don't match filters
        // IMPORTANT: Maintain the original order from candidates list (which is already sorted by relevance)
        var filteredCandidates = candidates
            .Where(c => itemLookup.ContainsKey(c.ResourceId))
            .Select(c =>
            {
                c.Item = itemLookup[c.ResourceId];
                return c;
            })
            .OrderByDescending(c => c.FinalScore)  // Ensure descending order by score
            .ToList();

        _logger.Information("Applied filters: {Original} -> {Filtered} candidates",
            candidates.Count, filteredCandidates.Count);

        return filteredCandidates;
    }

    /// <summary>
    /// Applies standard filters to the query (copied from ResourceManager).
    /// </summary>
    private IQueryable<ResourceGridItem> ApplyFilters(
        IQueryable<ResourceGridItem> query,
        DatabaseContext db,
        Dictionary<string, object?> filters)
    {
        // Type filter
        if (filters.TryGetValue("type", out var typeFilter) && typeFilter != null && ((string[])typeFilter).Length > 0)
            query = query.Where(x => ((string[])typeFilter).Contains(x.Type));

        // Publication date filters (include resources with unknown dates)
        if (filters.TryGetValue("pubdate_min", out var minDateFilter) && minDateFilter is DateTime minDate)
            query = query.Where(x => x.PublicationDate == null || x.PublicationDate >= minDate);

        if (filters.TryGetValue("pubdate_max", out var maxDateFilter) && maxDateFilter is DateTime maxDate)
            query = query.Where(x => x.PublicationDate == null || x.PublicationDate <= maxDate);

        // Tag filter
        query = ApplyRelationFilter(query, db, filters, "tag_ids", "tag_filter_mode", "tag");

        // Region filter
        query = ApplyRelationFilter(query, db, filters, "region_ids", "region_filter_mode", "region");

        return query;
    }

    /// <summary>
    /// Applies relation filters (tags, regions) to the query.
    /// </summary>
    private IQueryable<ResourceGridItem> ApplyRelationFilter(
        IQueryable<ResourceGridItem> query,
        DatabaseContext db,
        Dictionary<string, object?> filters,
        string idsKey,
        string modeKey,
        string relationType)
    {
        if (!filters.TryGetValue(idsKey, out var idsFilter) || idsFilter is not Guid[] ids || ids.Length == 0)
            return query;

        string filterMode = filters.TryGetValue(modeKey, out var mode) && mode is string modeStr ? modeStr : "any";

        if (filterMode.ToLower() == "all")
        {
            foreach (Guid id in ids)
            {
                if (relationType == "tag")
                    query = query.Where(x => x.Type != "resource" || db.ResourceTagRelations.Any(rt => rt.TagId == id && rt.ResourceId == x.Id));

                if (relationType == "region")
                    query = query.Where(x => x.Type != "resource" || db.ResourceRegionRelations.Any(rr => rr.RegionId == id && rr.ResourceId == x.Id));
            }
        }
        else
        {
            if (relationType == "tag")
                query = query.Where(x => x.Type != "resource" || db.ResourceTagRelations.Any(rt => ids.Contains(rt.TagId) && rt.ResourceId == x.Id));

            if (relationType == "region")
                query = query.Where(x => x.Type != "resource" || db.ResourceRegionRelations.Any(rr => ids.Contains(rr.RegionId) && rr.ResourceId == x.Id));
        }

        return query;
    }

    /// <summary>
    /// Creates an empty search result.
    /// </summary>
    private HybridSearchResult CreateEmptyResult(
        string searchQuery,
        int pageIndex,
        int pageSize,
        SearchMetadata metadata,
        Stopwatch stopwatch)
    {
        metadata.SearchDuration = stopwatch.Elapsed;

        return new HybridSearchResult
        {
            Items = Array.Empty<ResourceGridItemWithScore>(),
            TotalCount = 0,
            PageIndex = pageIndex,
            PageSize = pageSize,
            SearchTerm = searchQuery,
            IsSearchResult = true,
            Metadata = metadata
        };
    }
}
