using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Meilisearch;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Search;

public class LibrarySearchIndexService(MeilisearchClient client, DatabaseContext db)
{
    private const string IndexName = "library";

    public async Task EnsureIndexConfiguredAsync()
    {
        await client.CreateIndexAsync(IndexName, "id");
        Meilisearch.Index index = client.Index(IndexName);

        await index.UpdateSearchableAttributesAsync(["name", "aliases", "description", "occupation", "website", "emailAddress"]);
        await index.UpdateFilterableAttributesAsync(["type", "typeId", "journalId", "tagIds", "regionIds", "publicationDateTimestamp"]);
        await index.UpdateSortableAttributesAsync(["createdOnTimestamp", "name", "publicationDateTimestamp"]);
    }

    public async Task SyncResourceAsync(Guid id)
    {
        Resource? resource = await db.Resources
            .Include(r => r.ResourceTagRelations!)
            .Include(r => r.ResourceRegionRelations!)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource == null || resource.Trashed)
        {
            await DeleteAsync(id);
            return;
        }

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
            CreatedOnTimestamp = new DateTimeOffset(resource.CreatedOn).ToUnixTimeSeconds()
        };

        await client.Index(IndexName).AddDocumentsAsync([doc]);
    }

    public async Task SyncPersonAsync(Guid id)
    {
        Person? person = await db.Persons.FirstOrDefaultAsync(p => p.Id == id);

        if (person == null || person.Trashed)
        {
            await DeleteAsync(id);
            return;
        }

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
            CreatedOnTimestamp = new DateTimeOffset(person.CreatedOn).ToUnixTimeSeconds()
        };

        await client.Index(IndexName).AddDocumentsAsync([doc]);
    }

    public async Task SyncOrganisationAsync(Guid id)
    {
        Organisation? org = await db.Organisations.FirstOrDefaultAsync(o => o.Id == id);

        if (org == null || org.Trashed)
        {
            await DeleteAsync(id);
            return;
        }

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
            CreatedOnTimestamp = new DateTimeOffset(org.CreatedOn).ToUnixTimeSeconds()
        };

        await client.Index(IndexName).AddDocumentsAsync([doc]);
    }

    public async Task DeleteAsync(Guid id)
        => await client.Index(IndexName).DeleteOneDocumentAsync(id.ToString());

    public async Task<(Guid[] Ids, int TotalCount)> SearchAsync(string query, LibraryFilterOptions? filterOptions, int page, int pageSize)
    {
        var result = (SearchResult<LibrarySearchDocument>)await client.Index(IndexName).SearchAsync<LibrarySearchDocument>(query, new SearchQuery
        {
            Filter = BuildFilterExpressions(filterOptions),
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        });

        Guid[] ids = [.. result.Hits.Select(h => Guid.Parse(h.Id))];
        return (ids, result.EstimatedTotalHits);
    }

    private static string? BuildFilterExpressions(LibraryFilterOptions? options)
    {
        if (options == null) return null;

        List<string> clauses = [];

        if (options.TypeFilter?.Length > 0)
            clauses.Add($"type IN [{string.Join(", ", options.TypeFilter.Select(t => $"\"{t}\""))}]");

        if (!string.IsNullOrEmpty(options.PubdateMin) && DateTime.TryParse(options.PubdateMin, out var minDate))
            clauses.Add($"(publicationDateTimestamp IS NULL OR publicationDateTimestamp >= {new DateTimeOffset(DateTime.SpecifyKind(minDate, DateTimeKind.Utc)).ToUnixTimeSeconds()})");

        if (!string.IsNullOrEmpty(options.PubdateMax) && DateTime.TryParse(options.PubdateMax, out var maxDate))
            clauses.Add($"(publicationDateTimestamp IS NULL OR publicationDateTimestamp <= {new DateTimeOffset(DateTime.SpecifyKind(maxDate, DateTimeKind.Utc)).ToUnixTimeSeconds()})");

        if (options.TagFilter?.Length > 0)
            clauses.Add(BuildRelationClause("tagIds", options.TagFilter, options.TagFilterMode));

        if (options.RegionFilter?.Length > 0)
            clauses.Add(BuildRelationClause("regionIds", options.RegionFilter, options.RegionFilterMode));

        if (options.ResourceTypeFilter?.Length > 0)
            clauses.Add($"(type != \"resource\" OR typeId IN [{string.Join(", ", options.ResourceTypeFilter.Select(t => $"\"{t}\""))}])");

        if (options.JournalFilter?.Length > 0)
            clauses.Add($"(type != \"resource\" OR journalId IN [{string.Join(", ", options.JournalFilter.Select(t => $"\"{t}\""))}])");

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
}