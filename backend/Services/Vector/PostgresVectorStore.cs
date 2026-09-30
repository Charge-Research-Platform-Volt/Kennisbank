using KnowledgeBank.Data;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using PgVector = Pgvector.Vector;
using KnowledgeBank.Services.Search;

namespace KnowledgeBank.Services.Vector;

public class PostgresVectorStore(IDbContextFactory<DatabaseContext> dbFactory, EmbeddingService aiClientProvider, ChunkSearchIndexService chunkSearchIndexService, LibrarySearchIndexService librarySearchIndexService) : IVectorStore
{
    /// <inheritdoc />
    public async Task CreateResourcePointsAsync(Guid resourceId, List<(string Text, ChunkType Type, int Part)> chunks)
    {
        await using var database = await dbFactory.CreateDbContextAsync();
        List<ChunkSearchDocument> searchDocs = [];

        float[][] embeddings = await aiClientProvider.GenerateEmbeddings(chunks.Select(c => c.Text).ToList());

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            float[] embeddingArray = embeddings[i];

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
            searchDocs.Add(new ChunkSearchDocument
            {
                Id = resourceChunk.Id.ToString(),
                ParentId = resourceId.ToString(),
                ParentType = "resource",
                ChunkType = chunk.Type.ToString(),
                ChunkPart = chunk.Part,
                ChunkText = chunk.Text,
                Vectors = new() { ["default"] = embeddingArray }
            });

            if (chunk.Type == ChunkType.MetaData)
                await librarySearchIndexService.UpdateVectorAsync(resourceId, embeddingArray);
        }

        await database.SaveChangesAsync();
        await chunkSearchIndexService.IndexChunksAsync(searchDocs);
    }

    /// <inheritdoc />
    public async Task CreateEntityPointsAsync(Guid entityId, string entityType, List<(string Text, ChunkType Type, int Part)> chunks)
    {
        await using var database = await dbFactory.CreateDbContextAsync();
        List<ChunkSearchDocument> searchDocs = [];

        float[][] embeddings = await aiClientProvider.GenerateEmbeddings(chunks.Select(c => c.Text).ToList());

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            float[] embeddingArray = embeddings[i];

            EntityChunk entityChunk = new EntityChunk
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                ChunkType = chunk.Type,
                ChunkText = chunk.Text,
                ChunkPart = chunk.Part,
                Embedding = new PgVector(embeddingArray),
                CreatedAt = DateTime.UtcNow
            };

            database.EntityChunks.Add(entityChunk);
            searchDocs.Add(new ChunkSearchDocument
            {
                Id = entityChunk.Id.ToString(),
                ParentId = entityId.ToString(),
                ParentType = entityType,
                ChunkType = chunk.Type.ToString(),
                ChunkPart = chunk.Part,
                ChunkText = chunk.Text,
                Vectors = new() { ["default"] = embeddingArray }
            });

            if (chunk.Type == ChunkType.MetaData)
                await librarySearchIndexService.UpdateVectorAsync(entityId, embeddingArray);
        }

        await database.SaveChangesAsync();
        await chunkSearchIndexService.IndexChunksAsync(searchDocs);
    }

    /// <inheritdoc />
    public async Task<bool> DeletePointsByResourceIdAsync(Guid resourceId)
    {
        await using var database = await dbFactory.CreateDbContextAsync();

        int deleted = await database.ResourceChunks
            .Where(c => c.ResourceId == resourceId)
            .ExecuteDeleteAsync();

        await chunkSearchIndexService.DeleteByParentIdAsync(resourceId);
        return deleted > 0;
    }
    
    /// <inheritdoc />
    public async Task<bool> DeletePointsByEntityIdAsync(Guid entityId) 
    {
        await using var database = await dbFactory.CreateDbContextAsync();

        int deleted = await database.EntityChunks
            .Where(c => c.EntityId == entityId)
            .ExecuteDeleteAsync();

        await chunkSearchIndexService.DeleteByParentIdAsync(entityId);
        return deleted > 0;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateMetadataPointAsync(Guid resourceId, string newChunkText)
    {
        await using var database = await dbFactory.CreateDbContextAsync();

        var metadataChunk = await database.ResourceChunks
            .FirstOrDefaultAsync(c => c.ResourceId == resourceId && c.ChunkType == ChunkType.MetaData);

        if (metadataChunk == null)
            return false;

        metadataChunk.ChunkText = newChunkText;
        float[] embeddingArray = await aiClientProvider.GenerateEmbedding(newChunkText);
        metadataChunk.Embedding = new PgVector(embeddingArray);

        await database.SaveChangesAsync();

        await chunkSearchIndexService.IndexChunksAsync([new ChunkSearchDocument {
            Id = metadataChunk.Id.ToString(),
            ParentId = resourceId.ToString(),
            ParentType = "resource",
            ChunkType = metadataChunk.ChunkType.ToString(),
            ChunkPart = metadataChunk.ChunkPart,
            ChunkText = newChunkText,
            Vectors = new() { ["default"] = embeddingArray }
        }]);

        await librarySearchIndexService.UpdateVectorAsync(resourceId, embeddingArray);

        return true;
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetContentChunkTextsAsync(Guid resourceId)
    {
        await using var database = await dbFactory.CreateDbContextAsync();

        return await database.ResourceChunks
            .Where(c => c.ResourceId == resourceId && c.ChunkType == ChunkType.ContentText)
            .OrderBy(c => c.ChunkPart)
            .Select(c => c.ChunkText)
            .ToListAsync();
    }
}
