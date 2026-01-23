using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Vector;

public class PostgresVectorStore(DatabaseContext database, RAGSystem ragSystem) : IVectorStore 
{
    /// <inherit/>
    public async Task CreatePointsAsync(Guid resourceId, List<(string Text, ChunkType Type, int Part)> chunks) 
    {
        foreach (var chunk in chunks) 
        {
            float[] embedding = await ragSystem.GenerateEmbedding(chunk.Text);
            ResourceChunk resourceChunk = new ResourceChunk
            {
                Id = Guid.NewGuid(),
                ResourceId = resourceId,
                ChunkType = chunk.Type,
                ChunkText = chunk.Text,
                ChunkPart = chunk.Part,
                Embedding = embedding,
                CreatedAt = DateTime.UtcNow
            };

            database.ResourceChunks.Add(resourceChunk);
        }

        await database.SaveChangesAsync();
    }

    /// <inherit/>
    public async Task<bool> DeletePointsByResourceIdAsync(Guid resourceId)
    {
        int deleted = await database.ResourceChunks
            .Where(c => c.ResourceId == resourceId)
            .ExecuteDeleteAsync();
        return deleted > 0;
    }

    /// <inherit/>
    public async Task<bool> UpdateMetadataPointAsync(Guid resourceId, string newChunkText) 
    {
        var metadataChunk = await database.ResourceChunks
            .FirstOrDefaultAsync(c => c.ResourceId == resourceId && c.ChunkType == ChunkType.MetaData);

        if (metadataChunk == null)
            return false;

        metadataChunk.ChunkText = newChunkText;
        metadataChunk.Embedding = await ragSystem.GenerateEmbedding(newChunkText);

        await database.SaveChangesAsync();
        return true;
    }

    /// <inherit/>
    public async Task<List<VectorSearchResult>> SemanticSearchAsync(float[] queryEmbedding, int limit, float scoreThreshold) 
    {
        // Convert float[] to PostgreSQL vector format
        string vectorString = $"[{string.Join(",", queryEmbedding)}]";

        return await database.Database.SqlQuery<VectorSearchResult>($@"
            SELECT
                id as ""Id"",
                ""resource-id"" as ""ResourceId"",
                ""chunk-text"" as ""ChunkText"",
                ""chunk-type"" as ""ChunkType"",
                ""chunk-part"" as ""ChunkPart"",
                CAST(1 - (embedding <=> '{vectorString}'::vector) AS real) as ""Score""
            FROM ""resource-chunks""
            WHERE embedding IS NOT NULL
                AND 1 - (embedding <=> '{vectorString}'::vector) >= {scoreThreshold}
            ORDER BY embedding <=> '{vectorString}'::vector
            LIMIT {limit} 
        ").ToListAsync();
    }

    /// <inherit/>
    public async Task<List<VectorSearchResult>> TextSearchAsync(string query, int limit) 
    {
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
        // Get average embedding for the resource's chunks
        var avgEmbedding = await database.Database.SqlQuery<float[]>($@"
            SELECT AVG(embedding)::vector as ""Value""
            FROM ""resource-chunks""
            WHERE ""resource-id"" = {resourceId}
                AND embedding IS NOT NULL
        ").FirstOrDefaultAsync();

        if (avgEmbedding == null) return [];

        // Convert to vector string and search excluding the source resource
        string vectorString = $"[{string.Join(",", avgEmbedding)}]";

        return await database.Database.SqlQuery<VectorSearchResult>($@"
            SELECT
                id as ""Id"",
                ""resource-id"" as ""ResourceId"",
                ""chunk-text"" as ""ChunkText"",
                ""chunk-type"" as ""ChunkType"",
                ""chunk-part"" as ""ChunkPart"",
                CAST(1 - (embedding <=> '{vectorString}'::vector) AS real) as ""Score""
            FROM ""resource-chunks""
            WHERE embedding IS NOT NULL
                AND ""resource-id"" != {resourceId}
                AND 1 - (embedding <=> '{vectorString}'::vector) >= {scoreThreshold}
            ORDER BY embedding <=> '{vectorString}'::vector
            LIMIT {limit}
        ").ToListAsync();
    }
}