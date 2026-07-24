using Meilisearch;
using Meilisearch.QueryParameters;

namespace KnowledgeBank.Services.Search;

public class TaxonomySearchIndexService(MeilisearchClient client)
{
    private const string IndexName = "taxonomy";

    public async Task EnsureIndexConfiguredAsync()
    {
        await client.CreateIndexAsync(IndexName, "id");
        Meilisearch.Index index = client.Index(IndexName);
        await index.UpdateSearchableAttributesAsync(["name"]);
        await index.UpdateFilterableAttributesAsync(["type"]);
    }

    public async Task SyncAsync(Guid id, string name, string type)
        => await client.Index(IndexName).AddDocumentsAsync([new TaxonomySearchDocument { Id = id.ToString(), Name = name, Type = type }]);

    public async Task DeleteAsync(Guid id)
        => await client.Index(IndexName).DeleteOneDocumentAsync(id.ToString());

    public async Task<int> DeleteOrphanedAsync(HashSet<Guid> validIds)
    {
        List<TaxonomyIdRef> all = [];
        int offset = 0;
        const int pageSize = 1000;

        while (true)
        {
            var page = await client.Index(IndexName).GetDocumentsAsync<TaxonomyIdRef>(new DocumentsQuery
            {
                Limit = pageSize,
                Offset = offset,
                Fields = ["id"]
            });

            List<TaxonomyIdRef> results = page.Results.ToList();
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

    public async Task<(Guid[] Ids, int TotalCount)> SearchAsync(string query, string type, int page, int pageSize)
    {
        var result = (SearchResult<TaxonomySearchDocument>)await client.Index(IndexName).SearchAsync<TaxonomySearchDocument>(query, new SearchQuery
        {
            Filter = $"type = \"{type}\"",
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        });
        return ([.. result.Hits.Select(h => Guid.Parse(h.Id))], result.EstimatedTotalHits);
    }
}

public class TaxonomySearchDocument
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
}

public class TaxonomyIdRef
{
    public required string Id { get; set; }
}