using System.Text.Json.Serialization;
using Meilisearch;
using Meilisearch.QueryParameters;

namespace KnowledgeBank.Services.Search;

public class AttachmentChunkSearchIndexService(MeilisearchClient client)
{
    private const string IndexName = "attachment-chunks";

    public async Task EnsureIndexConfiguredAsync()
    {
        await client.CreateIndexAsync(IndexName, "id");
        Meilisearch.Index index = client.Index(IndexName);

        await index.UpdateSearchableAttributesAsync(["chunkText"]);
        await index.UpdateFilterableAttributesAsync(["attachmentId", "chatId"]);
        await index.UpdateEmbeddersAsync(new Dictionary<string, Embedder>
        {
            ["default"] = new Embedder { Source = EmbedderSource.UserProvided, Dimensions = 1024 }
        });
    }

    public async Task IndexChunksAsync(IEnumerable<AttachmentChunkDocument> documents)
    {
        var docs = documents.ToList();
        if (docs.Count == 0) return;
        await client.Index(IndexName).AddDocumentsInBatchesAsync(docs, batchSize: 500);
    }

    public async Task DeleteByAttachmentIdAsync(Guid attachmentId)
    {
        await client.Index(IndexName).DeleteDocumentsAsync(new DeleteDocumentsQuery
        {
            Filter = $"attachmentId = \"{attachmentId}\""
        });
    }

    public async Task<List<string>> SearchAsync(string query, float[] queryEmbedding, Guid attachmentId, int limit = 5) {
        SearchQuery searchQuery = new()
        {
            Vector = Array.ConvertAll(queryEmbedding, v => (double)v),
            Hybrid = new HybridSearch { Embedder = "default", SemanticRatio = SearchTuning.GetSemanticRatio(query) },
            Filter = $"attachmentId = \"{attachmentId}\"",
            ShowRankingScore = true,
            Limit = limit
        };

        var result = (SearchResult<AttachmentChunkDocument>)await client.Index(IndexName).SearchAsync<AttachmentChunkDocument>(query, searchQuery);

        return result.Hits
                     .OrderByDescending(h => h.RankingScore)
                     .Select(h => h.ChunkText)
                     .ToList();
    }
}

public class AttachmentChunkDocument
{
    public required string Id { get; set; }
    public required string AttachmentId { get; set; }
    public required string ChatId { get; set; }
    public required int ChunkPart { get; set; }
    public required string ChunkText { get; set; }

    [JsonPropertyName("_vectors")]
    public Dictionary<string, float[]> Vectors { get; set; } = [];

    [JsonPropertyName("_rankingScore")]
    public double? RankingScore { get; set; }
}