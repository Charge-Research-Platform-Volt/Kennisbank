using System.Text.Json.Serialization;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using Meilisearch;
using Meilisearch.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Search;

public class LibrarySearchIndexService(MeilisearchClient client, IDbContextFactory<DatabaseContext> dbFactory)
{
    private const string IndexName = "library";

    public async Task EnsureIndexConfiguredAsync()
    {
        await client.CreateIndexAsync(IndexName, "id");
        Meilisearch.Index index = client.Index(IndexName);


        await Task.WhenAll(
            index.UpdateSearchableAttributesAsync(["name", "aliases", "description", "occupation", "website", "emailAddress"]),
            index.UpdateFilterableAttributesAsync(["type", "typeId", "journalId", "tagIds", "regionIds", "publicationDateTimestamp"]),
            index.UpdateSortableAttributesAsync(["createdOnTimestamp", "name", "publicationDateTimestamp"]),
            index.UpdateEmbeddersAsync(new Dictionary<string, Embedder>
            {
                ["default"] = new Embedder { Source = EmbedderSource.UserProvided, Dimensions = 1024 }
            })
        );
    }

    public async Task UpdateVectorAsync(Guid id, float[] vector)
    {
        await client.Index(IndexName).UpdateDocumentsAsync(new[]
        {
            new { id = id.ToString(), _vectors = new Dictionary<string, float[]> { ["default"] = vector }}
        });
    }

    public async Task SyncResourceAsync(Guid id)
    {
        await using DatabaseContext db = await dbFactory.CreateDbContextAsync();

        Resource? resource = await db.Resources
            .Include(r => r.ResourceTagRelations!)
            .Include(r => r.ResourceRegionRelations!)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource == null || resource.Trashed)
        {
            await DeleteAsync(id);
            return;
        }

        float[]? vector = await db.ResourceChunks
            .Where(c => c.ResourceId == id && c.ChunkType == ChunkType.MetaData && c.Embedding != null)
            .Select(c => c.Embedding!.ToArray())
            .FirstOrDefaultAsync();

        LibrarySearchDocument doc = new()
        {
            Id = resource.Id.ToString(),
            Name = resource.Title,
            Description = resource.Description,
            Type = "resource",
            TypeId = resource.TypeId?.ToString(),
            JournalId = resource.JournalId?.ToString(),
            TagIds = resource.ResourceTagRelations?.Select(r => r.TagId.ToString()).ToArray() ?? [],
            RegionIds = resource.ResourceRegionRelations?.Select(r => r.RegionId.ToString()).ToArray() ?? [],
            PublicationDateTimestamp = resource.PublicationDate.HasValue ? new DateTimeOffset(resource.PublicationDate.Value).ToUnixTimeSeconds() : null,
            CreatedOnTimestamp = new DateTimeOffset(resource.CreatedOn).ToUnixTimeSeconds(),
            Vectors = new() { ["default"] = vector }
        };

        await client.Index(IndexName).AddDocumentsAsync([doc]);
    }

    public async Task SyncPersonAsync(Guid id)
    {
        await using DatabaseContext db = await dbFactory.CreateDbContextAsync();

        Person? person = await db.Persons.FirstOrDefaultAsync(p => p.Id == id);

        if (person == null || person.Trashed)
        {
            await DeleteAsync(id);
            return;
        }

        float[]? vector = await db.EntityChunks
            .Where(c => c.EntityId == id && c.ChunkType == ChunkType.MetaData && c.Embedding != null)
            .Select(c => c.Embedding!.ToArray())
            .FirstOrDefaultAsync();

        LibrarySearchDocument doc = new()
        {
            Id = person.Id.ToString(),
            Name = person.Name,
            Description = person.Description,
            Aliases = person.Aliases,
            Occupation = person.Occupation,
            EmailAddress = person.EmailAddress,
            Type = "person",
            TagIds = [],
            RegionIds = [],
            CreatedOnTimestamp = new DateTimeOffset(person.CreatedOn).ToUnixTimeSeconds(),
            Vectors = new() { ["default"] = vector }
        };

        await client.Index(IndexName).AddDocumentsAsync([doc]);
    }

    public async Task SyncOrganisationAsync(Guid id)
    {
        await using DatabaseContext db = await dbFactory.CreateDbContextAsync();

        Organisation? org = await db.Organisations.FirstOrDefaultAsync(o => o.Id == id);

        if (org == null || org.Trashed)
        {
            await DeleteAsync(id);
            return;
        }

        float[]? vector = await db.EntityChunks
            .Where(c => c.EntityId == id && c.ChunkType == ChunkType.MetaData && c.Embedding != null)
            .Select(c => c.Embedding!.ToArray())
            .FirstOrDefaultAsync();

        LibrarySearchDocument doc = new()
        {
            Id = org.Id.ToString(),
            Name = org.Name,
            Description = org.Description,
            Aliases = org.Aliases,
            Website = org.Website,
            EmailAddress = org.EmailAddress,
            Type = "organisation",
            TagIds = [],
            RegionIds = [],
            CreatedOnTimestamp = new DateTimeOffset(org.CreatedOn).ToUnixTimeSeconds(),
            Vectors = new() { ["default"] = vector }
        };

        await client.Index(IndexName).AddDocumentsAsync([doc]);
    }

    public async Task DeleteAsync(Guid id)
        => await client.Index(IndexName).DeleteOneDocumentAsync(id.ToString());

    public async Task<int> DeleteOrphanedAsync(HashSet<Guid> validIds)
    {
        List<LibraryIdRef> all = [];
        int offset = 0;
        const int pageSize = 1000;

        while (true)
        {
            var page = await client.Index(IndexName).GetDocumentsAsync<LibraryIdRef>(new DocumentsQuery
            {
                Limit = pageSize,
                Offset = offset,
                Fields = ["id"]
            });

            List<LibraryIdRef> results = page.Results.ToList();
            all.AddRange(results);

            if (results.Count < pageSize) break;
            offset += pageSize;
        }

        List<string> orphanIds = all
            .Where(d => !validIds.Contains(Guid.Parse(d.Id)))
            .Select(d => d.Id)
            .ToList();

        if (orphanIds.Count == 0) return 0;

        await client.Index(IndexName).DeleteDocumentsAsync(orphanIds);
        return orphanIds.Count;
    }

    public async Task<(Guid[] Ids, int TotalCount)> SearchAsync(string query, LibraryFilterOptions? filterOptions, int page, int pageSize, float[]? queryEmbedding = null)
    {
        var searchQuery = new SearchQuery
        {
            Filter = BuildFilterExpressions(filterOptions),
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        };

        if (queryEmbedding != null)
        {
            searchQuery.Vector = Array.ConvertAll(queryEmbedding, v => (double)v);
            searchQuery.Hybrid = new HybridSearch { Embedder = "default", SemanticRatio = SearchTuning.GetSemanticRatio(query) };
        }

        var result = (SearchResult<LibrarySearchDocument>)await client.Index(IndexName).SearchAsync<LibrarySearchDocument>(query, searchQuery);

        Guid[] ids = [.. result.Hits.Select(h => Guid.Parse(h.Id))];
        return (ids, result.EstimatedTotalHits);
    }

    public async Task<List<(Guid Id, string Type, float Score)>> SearchWithScoresAsync(string query, LibraryFilterOptions? filterOptions, int limit, float[]? queryEmbedding = null)
    {
        var searchQuery = new SearchQuery
        {
            Filter = BuildFilterExpressions(filterOptions),
            ShowRankingScore = true,
            Limit = limit
        };

        if (queryEmbedding != null)
        {
            searchQuery.Vector = Array.ConvertAll(queryEmbedding, v => (double)v);
            searchQuery.Hybrid = new HybridSearch { Embedder = "default", SemanticRatio = SearchTuning.GetSemanticRatio(query) };
        }

        var result = (SearchResult<LibrarySearchDocument>)await client.Index(IndexName).SearchAsync<LibrarySearchDocument>(query, searchQuery);

        return result.Hits
            .Select(h => (Guid.Parse(h.Id), h.Type, (float)(h.RankingScore ?? 0)))
            .ToList();
    }

    public async Task<Dictionary<string, int>> GetTypeFacetsAsync(string query, LibraryFilterOptions? filterOptions)
    {
        LibraryFilterOptions? facetFilterOptions = filterOptions == null ? null : new LibraryFilterOptions
        {
            PubdateMin = filterOptions.PubdateMin,
            PubdateMax = filterOptions.PubdateMax,
            TagFilter = filterOptions.TagFilter,
            TagFilterMode = filterOptions.TagFilterMode,
            RegionFilter = filterOptions.RegionFilter,
            RegionFilterMode = filterOptions.RegionFilterMode,
            ResourceTypeFilter = filterOptions.ResourceTypeFilter,
            JournalFilter = filterOptions.JournalFilter
        };

        var result = (SearchResult<LibrarySearchDocument>)await client.Index(IndexName).SearchAsync<LibrarySearchDocument>(query, new SearchQuery
        {
            Filter = BuildFilterExpressions(facetFilterOptions),
            Facets = ["type"],
            Limit = 0
        });

        return result.FacetDistribution != null && result.FacetDistribution.TryGetValue("type", out var distribution)
            ? distribution.ToDictionary(kv => kv.Key, kv => kv.Value)
            : [];
    }

    // Filter values come straight from the request and are inserted into the filter string, so only
    // known types and well-formed GUIDs are allowed through
    private static readonly HashSet<string> KnownTypes = ["resource", "person", "organisation"];

    private static string[] ValidIds(string[]? ids) =>
        ids?.Select(id => Guid.TryParse(id, out Guid guid) ? guid.ToString() : null).OfType<string>().ToArray() ?? [];

    internal static string? BuildFilterExpressions(LibraryFilterOptions? options)
    {
        if (options == null) return null;

        List<string> clauses = [];

        string[] types = options.TypeFilter?.Where(KnownTypes.Contains).ToArray() ?? [];
        string[] tagIds = ValidIds(options.TagFilter);
        string[] regionIds = ValidIds(options.RegionFilter);
        string[] resourceTypeIds = ValidIds(options.ResourceTypeFilter);
        string[] journalIds = ValidIds(options.JournalFilter);

        if (types.Length > 0)
            clauses.Add($"type IN [{string.Join(", ", types.Select(t => $"\"{t}\""))}]");

        if (!string.IsNullOrEmpty(options.PubdateMin) && DateTime.TryParse(options.PubdateMin, out var minDate))
            clauses.Add($"(publicationDateTimestamp IS NULL OR publicationDateTimestamp >= {new DateTimeOffset(DateTime.SpecifyKind(minDate, DateTimeKind.Utc)).ToUnixTimeSeconds()})");

        if (!string.IsNullOrEmpty(options.PubdateMax) && DateTime.TryParse(options.PubdateMax, out var maxDate))
            clauses.Add($"(publicationDateTimestamp IS NULL OR publicationDateTimestamp <= {new DateTimeOffset(DateTime.SpecifyKind(maxDate, DateTimeKind.Utc)).ToUnixTimeSeconds()})");

        if (tagIds.Length > 0)
            clauses.Add(BuildRelationClause("tagIds", tagIds, options.TagFilterMode));

        if (regionIds.Length > 0)
            clauses.Add(BuildRelationClause("regionIds", regionIds, options.RegionFilterMode));

        if (resourceTypeIds.Length > 0)
            clauses.Add($"(type != \"resource\" OR typeId IN [{string.Join(", ", resourceTypeIds.Select(t => $"\"{t}\""))}])");

        if (journalIds.Length > 0)
            clauses.Add($"(type != \"resource\" OR journalId IN [{string.Join(", ", journalIds.Select(t => $"\"{t}\""))}])");

        return clauses.Count > 0 ? string.Join(" AND ", clauses) : null;
    }
    
    private static string BuildRelationClause(string field, string[] ids, string? mode)
    {
        string relation = mode?.Equals("all", StringComparison.OrdinalIgnoreCase) == true
            ? string.Join(" AND ", ids.Select(id => $"{field} = \"{id}\""))
            : $"{field} IN [{string.Join(", ", ids.Select(id => $"\"{id}\""))}]";

        return $"(type != \"resource\" OR {relation})";
    }
}

public class LibrarySearchDocument
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string> Aliases { get; set; } = [];
    public string? Occupation { get; set; }
    public string? Website { get; set; }
    public string? EmailAddress { get; set; }
    public required string Type { get; set; }
    public string? TypeId { get; set; }
    public string? JournalId { get; set; }
    public string[] TagIds { get; set; } = [];
    public string[] RegionIds { get; set; } = [];
    public long? PublicationDateTimestamp { get; set; }
    public long CreatedOnTimestamp { get; set; }

    [JsonPropertyName("_vectors")]
    public Dictionary<string, float[]?> Vectors { get; set; } = [];

    [JsonPropertyName("_rankingScore")]
    public double? RankingScore { get; set; }
}

public class LibraryIdRef
{
    public required string Id { get; set; }
}