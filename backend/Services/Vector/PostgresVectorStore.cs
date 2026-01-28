using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using PgVector = Pgvector.Vector;

namespace KnowledgeBank.Services.Vector;

public class PostgresVectorStore(IDbContextFactory<DatabaseContext> dbFactory, RAGSystem ragSystem) : IVectorStore
{
    /// <inherit/>
    public async Task CreatePointsAsync(Guid resourceId, List<(string Text, ChunkType Type, int Part)> chunks)
    {
        await using var database = await dbFactory.CreateDbContextAsync();

        foreach (var chunk in chunks)
        {
            float[] embeddingArray = await ragSystem.GenerateEmbedding(chunk.Text);
            ResourceChunk resourceChunk = new ResourceChunk
            {
                Id = Guid.NewGuid(),
                ResourceId = resourceId,
                ChunkType = chunk.Type,
                ChunkText = chunk.Text,
                ChunkPart = chunk.Part,
                Embedding = new PgVector(embeddingArray),
                CreatedAt = DateTime.UtcNow
            };

            database.ResourceChunks.Add(resourceChunk);
        }

        await database.SaveChangesAsync();
    }

    /// <inherit/>
    public async Task<bool> DeletePointsByResourceIdAsync(Guid resourceId)
    {
        await using var database = await dbFactory.CreateDbContextAsync();

        int deleted = await database.ResourceChunks
            .Where(c => c.ResourceId == resourceId)
            .ExecuteDeleteAsync();
        return deleted > 0;
    }

    /// <inherit/>
    public async Task<bool> UpdateMetadataPointAsync(Guid resourceId, string newChunkText)
    {
        await using var database = await dbFactory.CreateDbContextAsync();

        var metadataChunk = await database.ResourceChunks
            .FirstOrDefaultAsync(c => c.ResourceId == resourceId && c.ChunkType == ChunkType.MetaData);

        if (metadataChunk == null)
            return false;

        metadataChunk.ChunkText = newChunkText;
        float[] embeddingArray = await ragSystem.GenerateEmbedding(newChunkText);
        metadataChunk.Embedding = new PgVector(embeddingArray);

        await database.SaveChangesAsync();
        return true;
    }

    /// <inherit/>
    public async Task<List<VectorSearchResult>> SemanticSearchAsync(float[] queryEmbedding, int limit, float scoreThreshold)
    {
        await using var database = await dbFactory.CreateDbContextAsync();

        var queryVector = new PgVector(queryEmbedding);

        // Use cosine distance - score is 1 - distance (higher is better)
        var results = await database.ResourceChunks
            .Where(c => c.Embedding != null)
            .Select(c => new
            {
                c.Id,
                c.ResourceId,
                c.ChunkText,
                ChunkType = c.ChunkType.ToString(),
                c.ChunkPart,
                Distance = c.Embedding!.CosineDistance(queryVector)
            })
            .Where(c => 1 - c.Distance >= scoreThreshold)
            .OrderBy(c => c.Distance)
            .Take(limit)
            .ToListAsync();

        return results.Select(r => new VectorSearchResult
        {
            Id = r.Id,
            ResourceId = r.ResourceId,
            ChunkText = r.ChunkText,
            ChunkType = r.ChunkType,
            ChunkPart = r.ChunkPart,
            Score = 1 - (float)r.Distance
        }).ToList();
    }

    /// <inherit/>
    public async Task<List<VectorSearchResult>> TextSearchAsync(string query, int limit)
    {
        await using var database = await dbFactory.CreateDbContextAsync();

        // Use raw SQL for trigram search since EF Core doesn't have built-in support
        return await database.Database.SqlQuery<VectorSearchResult>($@"
            SELECT
                id as ""Id"",
                ""resource-id"" as ""ResourceId"",
                ""chunk-text"" as ""ChunkText"",
                ""chunk-type"" as ""ChunkType"",
                ""chunk-part"" as ""ChunkPart"",
                CAST(similarity(""chunk-text"", {query}) AS real) as ""Score""
            FROM ""resource-chunks""
            WHERE ""chunk-text"" % {query} OR ""chunk-text"" ILIKE '%' || {query} || '%'
            ORDER BY ""Score"" DESC
            LIMIT {limit}
        ").ToListAsync();
    }

    /// <inherit/>
    public async Task<List<VectorSearchResult>> GetChunksByResourceIdAsync(Guid resourceId)
    {
        await using var database = await dbFactory.CreateDbContextAsync();

        return await database.ResourceChunks
            .Where(c => c.ResourceId == resourceId)
            .OrderBy(c => c.ChunkPart)
            .Select(c => new VectorSearchResult
            {
                Id = c.Id,
                ResourceId = c.ResourceId,
                ChunkText = c.ChunkText,
                ChunkType = c.ChunkType.ToString(),
                ChunkPart = c.ChunkPart,
                Score = 1.0f
            }).ToListAsync();
    }

    /// <inherit/>
    public async Task<List<VectorSearchResult>> RecommendSimilarAsync(Guid resourceId, int limit, float scoreThreshold)
    {
        await using var database = await dbFactory.CreateDbContextAsync();

        // Get the average embedding for the resource's chunks using raw SQL
        // since EF Core doesn't support AVG on vector types directly
        var avgEmbeddingResult = await database.Database.SqlQuery<VectorResult>($@"
            SELECT AVG(embedding)::vector(1536) as ""Value""
            FROM ""resource-chunks""
            WHERE ""resource-id"" = {resourceId}
                AND embedding IS NOT NULL
        ").FirstOrDefaultAsync();

        if (avgEmbeddingResult?.Value == null) return [];

        var avgVector = avgEmbeddingResult.Value;

        // Search for similar chunks excluding the source resource
        var results = await database.ResourceChunks
            .Where(c => c.Embedding != null && c.ResourceId != resourceId)
            .Select(c => new
            {
                c.Id,
                c.ResourceId,
                c.ChunkText,
                ChunkType = c.ChunkType.ToString(),
                c.ChunkPart,
                Distance = c.Embedding!.CosineDistance(avgVector)
            })
            .Where(c => 1 - c.Distance >= scoreThreshold)
            .OrderBy(c => c.Distance)
            .Take(limit)
            .ToListAsync();

        return results.Select(r => new VectorSearchResult
        {
            Id = r.Id,
            ResourceId = r.ResourceId,
            ChunkText = r.ChunkText,
            ChunkType = r.ChunkType,
            ChunkPart = r.ChunkPart,
            Score = 1 - (float)r.Distance
        }).ToList();
    }
}

/// <summary>
/// Helper class for SQL query results that return a vector
/// </summary>
internal class VectorResult
{
    public PgVector? Value { get; set; }
}
