using System.Text.Json.Serialization;
using Meilisearch;
using Meilisearch.QueryParameters;

namespace KnowledgeBank.Services.Search;

public class ChunkSearchIndexService(MeilisearchClient client)
{
    private const string IndexName = "chunks";

    public async Task EnsureIndexConfiguredAsync()
    {
        await client.CreateIndexAsync(IndexName, "id");
        Meilisearch.Index index = client.Index(IndexName);

        await index.UpdateSearchableAttributesAsync(["chunkText"]);
        await index.UpdateFilterableAttributesAsync(["parentId", "parentType", "chunkType"]);
        await index.UpdateEmbeddersAsync(new Dictionary<string, Embedder>
        {
            ["default"] = new Embedder { Source = EmbedderSource.UserProvided, Dimensions = 1024 }
        });
    }

    public async Task IndexChunksAsync(IEnumerable<ChunkSearchDocument> documents)
    {
        var docs = documents.ToList();
        if (docs.Count == 0) return;
        await client.Index(IndexName).AddDocumentsInBatchesAsync(docs, batchSize: 500);
    }

    public async Task DeleteByParentIdAsync(Guid parentId)
    {
        await client.Index(IndexName).DeleteDocumentsAsync(new DeleteDocumentsQuery
        {
            Filter = $"parentId = \"{parentId}\""
        });
    }

    public async Task<int> DeleteOrphanedAsync(HashSet<Guid> validResourceIds, HashSet<Guid> validEntityIds)
    {
        List<ChunkParentRef> all = [];
        int offset = 0;
        const int pageSize = 1000;

        while (true)
        {
            var page = await client.Index(IndexName).GetDocumentsAsync<ChunkParentRef>(new DocumentsQuery
            {
                Limit = pageSize,
                Offset = offset,
                Fields = ["id", "parentId", "parentType"]
            });

            List<ChunkParentRef> results = page.Results.ToList();
            all.AddRange(results);

            if (results.Count < pageSize) break;
            offset += pageSize;
        }

        List<string> orphanIds = all
            .Where(d => !(d.ParentType == "resource" ? validResourceIds.Contains(Guid.Parse(d.ParentId)) : validEntityIds.Contains(Guid.Parse(d.ParentId))))
            .Select(d => d.Id)
            .ToList();

        if (orphanIds.Count == 0) return 0;

        await client.Index(IndexName).DeleteDocumentsAsync(orphanIds);
        return orphanIds.Count;
    }

    public async Task<List<(Guid ResourceId, float Score)>> RecommendSimilarAsync(Guid resourceId, int limit, float scoreThreshold)
    {
        var ownResult = (SearchResult<ChunkVectorDoc>)await client.Index(IndexName).SearchAsync<ChunkVectorDoc>("", new SearchQuery
        {
            Filter = $"parentId = \"{resourceId}\"",
            RetrieveVectors = true,
            Limit = 1000
        });

        List<float[]> ownVectors = ownResult.Hits
            .Select(h => h.Vectors?.GetValueOrDefault("default")?.Embeddings?.FirstOrDefault())
            .Where(v => v != null)
            .Select(v => v!)
            .ToList();

        if (ownVectors.Count == 0) return [];

        float[] avgVector = new float[ownVectors[0].Length];
        foreach (float[] v in ownVectors)
            for (int i = 0; i < avgVector.Length; i++)
                avgVector[i] += v[i];
        for (int i = 0; i < avgVector.Length; i++)
            avgVector[i] /= ownVectors.Count;

        var result = (SearchResult<ChunkSearchDocument>)await client.Index(IndexName).SearchAsync<ChunkSearchDocument>("", new SearchQuery
        {
            Vector = Array.ConvertAll(avgVector, v => (double)v),
            Hybrid = new HybridSearch { Embedder = "default", SemanticRatio = 1.0 },
            Filter = $"parentType = \"resource\" AND NOT (parentId = \"{resourceId}\")",
            Distinct = "parentId",
            ShowRankingScore = true,
            Limit = limit
        });

        return result.Hits
            .Where(h => (h.RankingScore ?? 0) >= scoreThreshold)
            .Select(h => (Guid.Parse(h.ParentId), (float)(h.RankingScore ?? 0)))
            .ToList();
    }

    public async Task<List<ChunkSearchResult>> SearchAsync(string query, float[] queryEmbedding, int limit, Guid[]? parentIdFilter = null, string? typeFilter = null, Guid[]? excludeParentIds = null, int chunksPerParent = 3)
    {
        List<string> clauses = [];

        if (parentIdFilter?.Length > 0)
            clauses.Add($"parentId IN [{string.Join(", ", parentIdFilter.Select(id => $"\"{id}\""))}]");

        if (!string.IsNullOrEmpty(typeFilter))
            clauses.Add($"parentType = \"{typeFilter}\"");

        if (excludeParentIds?.Length > 0)
            clauses.Add($"NOT (parentId IN [{string.Join(", ", excludeParentIds.Select(id => $"\"{id}\""))}])");

        string? filter = clauses.Count > 0 ? string.Join(" AND ", clauses) : null;
        double semanticRatio = SearchTuning.GetSemanticRatio(query);

        var rankQuery = new SearchQuery
        {
            Vector = Array.ConvertAll(queryEmbedding, v => (double)v),
            Hybrid = new HybridSearch { Embedder = "default", SemanticRatio = semanticRatio },
            Filter = filter,
            Distinct = "parentId",
            ShowRankingScore = true,
            Limit = limit
        };

        var rankResult = (SearchResult<ChunkSearchDocument>)await client.Index(IndexName).SearchAsync<ChunkSearchDocument>(query, rankQuery);
        var rankedParents = rankResult.Hits
            .Select(h => (h.ParentId, h.ParentType, Score: h.RankingScore ?? 0))
            .ToList();

        if (rankedParents.Count == 0) return [];

        string parentsClause = $"parentId IN [{string.Join(", ", rankedParents.Select(p => $"\"{p.ParentId}\""))}]";

        var snippetQuery = new SearchQuery
        {
            Vector = Array.ConvertAll(queryEmbedding, v => (double)v),
            Hybrid = new HybridSearch { Embedder = "default", SemanticRatio = semanticRatio },
            Filter = parentsClause,
            ShowRankingScore = true,
            AttributesToCrop = ["chunkText"],
            CropLength = 30,
            AttributesToHighlight = ["chunkText"],
            HighlightPreTag = "\u0001",
            HighlightPostTag = "\u0002",
            Limit = rankedParents.Count * chunksPerParent
        };

        var snippetResult = (SearchResult<ChunkSearchDocument>)await client.Index(IndexName).SearchAsync<ChunkSearchDocument>(query, snippetQuery);

        Dictionary<string, int> order = rankedParents.Select((p, i) => (p.ParentId, i)).ToDictionary(x => x.ParentId, x => x.i);
        Dictionary<string, float> scoreByParent = rankedParents.ToDictionary(p => p.ParentId, p => (float)p.Score);

        return snippetResult.Hits
            .GroupBy(h => (h.ParentId, h.ParentType))
            .Select(g => new ChunkSearchResult
            {
                ParentId = Guid.Parse(g.Key.ParentId),
                ParentType = g.Key.ParentType,
                MatchedChunks = g.OrderByDescending(c => c.RankingScore).Take(chunksPerParent).Select(c => c.Formatted?.ChunkText ?? c.ChunkText).ToList(),
                Score = scoreByParent[g.Key.ParentId]
            })
            .OrderBy(r => order[r.ParentId.ToString()])
            .ToList();
    }
}

public class ChunkSearchDocument
{
    public required string Id { get; set; }
    public required string ParentId { get; set; }
    public required string ParentType { get; set; }
    public required string ChunkType { get; set; }
    public required int ChunkPart { get; set; }
    public required string ChunkText { get; set; }

    [JsonPropertyName("_vectors")]
    public Dictionary<string, float[]> Vectors { get; set; } = [];

    [JsonPropertyName("_rankingScore")]
    public double? RankingScore { get; set; }

    [JsonPropertyName("_formatted")]
    public FormattedChunk? Formatted { get; set; }
}

public class FormattedChunk
{
    public string? ChunkText { get; set; }
}

public class ChunkSearchResult
{
    public required Guid ParentId { get; set; }
    public required string ParentType { get; set; }
    public required List<string> MatchedChunks { get; set; }
    public required float Score { get; set; }
}

public class ChunkParentRef
{
    public required string Id { get; set; }
    public required string ParentId { get; set; }
    public required string ParentType { get; set; }
}

public class ChunkVectorDoc
{
    [JsonPropertyName("_vectors")]
    public Dictionary<string, VectorDetail>? Vectors { get; set; }
}

public class VectorDetail
{
    public required float[][] Embeddings { get; set; }
}