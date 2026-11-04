using System.Text.Json.Serialization;
using Meilisearch;
using Meilisearch.QueryParameters;

namespace KnowledgeBank.Services.Search;

public class ProjectDocumentChunkSearchIndexService(MeilisearchClient client)
{
    private const string IndexName = "project-doc-chunks";

    public async Task EnsureIndexConfiguredAsync()
    {
        await client.CreateIndexAsync(IndexName, "id");
        Meilisearch.Index index = client.Index(IndexName);

        await Task.WhenAll(
            index.UpdateSearchableAttributesAsync(["chunkText"]),
            index.UpdateFilterableAttributesAsync(["documentId", "projectId"]),
            index.UpdateEmbeddersAsync(new Dictionary<string, Embedder>
            {
                ["default"] = new Embedder { Source = EmbedderSource.UserProvided, Dimensions = 1024 }
            })
        );
    }

    public async Task IndexChunksAsync(IEnumerable<ProjectDocumentChunkDocument> documents)
    {
        var docs = documents.ToList();
        if (docs.Count == 0) return;
        await client.Index(IndexName).AddDocumentsInBatchesAsync(docs, batchSize: 500);
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId)
    {
        await client.Index(IndexName).DeleteDocumentsAsync(new DeleteDocumentsQuery
        {
            Filter = $"documentId = \"{documentId}\""
        });
    }

    /// <summary>
    /// Hybrid search within the given documents only. The caller passes the documents the user may view;
    /// an empty list returns nothing rather than searching every project's documents.
    /// </summary>
    public async Task<List<ProjectDocumentChunkDocument>> SearchAsync(string query, float[] queryEmbedding, IReadOnlyCollection<Guid> documentIds, int limit = 8)
    {
        if (documentIds.Count == 0) return [];

        SearchQuery searchQuery = new()
        {
            Vector = Array.ConvertAll(queryEmbedding, v => (double)v),
            Hybrid = new HybridSearch { Embedder = "default", SemanticRatio = SearchTuning.GetSemanticRatio(query) },
            Filter = $"documentId IN [{string.Join(", ", documentIds.Select(id => $"\"{id}\""))}]",
            ShowRankingScore = true,
            Limit = limit
        };

        var result = (SearchResult<ProjectDocumentChunkDocument>)await client.Index(IndexName).SearchAsync<ProjectDocumentChunkDocument>(query, searchQuery);

        return [.. result.Hits.OrderByDescending(h => h.RankingScore)];
    }
}

public class ProjectDocumentChunkDocument
{
    public required string Id { get; set; }
    public required string DocumentId { get; set; }
    public required string ProjectId { get; set; }
    public required string VersionId { get; set; }
    public required int ChunkPart { get; set; }
    public required string ChunkText { get; set; }

    [JsonPropertyName("_vectors")]
    public Dictionary<string, float[]> Vectors { get; set; } = [];

    [JsonPropertyName("_rankingScore")]
    public double? RankingScore { get; set; }
}