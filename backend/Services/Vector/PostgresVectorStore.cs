using KnowledgeBank.Data;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using PgVector = Pgvector.Vector;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Models.ProjectDocuments;

namespace KnowledgeBank.Services.Vector;

public class PostgresVectorStore(IDbContextFactory<DatabaseContext> dbFactory, EmbeddingService aiClientProvider, ChunkSearchIndexService chunkSearchIndexService, LibrarySearchIndexService librarySearchIndexService, ProjectDocumentChunkSearchIndexService projectDocumentChunkSearchIndexService) : IVectorStore
{
    /// <inheritdoc/>
    public async Task ReplaceResourcePointsAsync(Guid resourceId, List<(string Text, ChunkType Type, int Part)> chunks)
    {
        // Embed first
        float[][] embeddings = await aiClientProvider.GenerateEmbeddings(chunks.Select(c => c.Text).ToList());

        await using var database = await dbFactory.CreateDbContextAsync();
        List<ChunkSearchDocument> searchDocs = [];
        float[]? metadataEmbedding = null;

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            float[] embeddingArray = embeddings[i];

            ResourceChunk resourceChunk = new()
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
                metadataEmbedding = embeddingArray;
        }

        // Swap old for new in one transaction
        await using (var transaction = await database.Database.BeginTransactionAsync())
        {
            await database.ResourceChunks.Where(c => c.ResourceId == resourceId).ExecuteDeleteAsync();
            await database.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // Send chunk embeddings to Meilisearch
        await chunkSearchIndexService.DeleteByParentIdAsync(resourceId);
        await chunkSearchIndexService.IndexChunksAsync(searchDocs);

        // Send metadata embeddings to Meilisearch
        if (metadataEmbedding != null)
            await librarySearchIndexService.UpdateVectorAsync(resourceId, metadataEmbedding);
    }

    /// <inheritdoc />
    public async Task ReplaceEntityPointsAsync(Guid entityId, string entityType, List<(string Text, ChunkType Type, int Part)> chunks)
    {
        float[][] embeddings = await aiClientProvider.GenerateEmbeddings(chunks.Select(c => c.Text).ToList());

        await using var database = await dbFactory.CreateDbContextAsync();
        List<ChunkSearchDocument> searchDocs = [];
        float[]? metadataEmbedding = null;

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            float[] embeddingArray = embeddings[i];

            EntityChunk entityChunk = new()
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
                metadataEmbedding = embeddingArray;
        }

        await using (var transaction = await database.Database.BeginTransactionAsync())
        {
            await database.EntityChunks.Where(c => c.EntityId == entityId).ExecuteDeleteAsync();
            await database.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // Store chunk embeddings in Meilisearch
        await chunkSearchIndexService.DeleteByParentIdAsync(entityId);
        await chunkSearchIndexService.IndexChunksAsync(searchDocs);

        // Store metadata embedding
        if (metadataEmbedding != null)
            await librarySearchIndexService.UpdateVectorAsync(entityId, metadataEmbedding);
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

        /// <inheritdoc />
    public async Task<bool> ReplaceProjectDocumentPointsAsync(Guid documentId, Guid versionId, List<(string Text, int Part)> chunks)
    {
        // Embed outside transaction
        float[][] embeddings = await aiClientProvider.GenerateEmbeddings(chunks.Select(c => c.Text).ToList());

        await using var database = await dbFactory.CreateDbContextAsync();
        await using var transaction = await database.Database.BeginTransactionAsync();

        // The row lock serialises swaps per document, so a slow older version can't overwrite a newer one's chunks.
        // Adding a version also updates this row, so it waits for us (or we see its commited version below).
        ProjectDocument? document = await database.ProjectDocuments
            .FromSql($"SELECT * FROM \"project-documents\" WHERE id = {documentId} FOR UPDATE")
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (document == null) return false;

        Guid latestId = await database.ProjectDocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => v.Id)
            .FirstOrDefaultAsync();

        if (latestId != versionId) return false;

        List<ProjectDocumentChunkDocument> searchDocs = [];

        for (int i = 0; i < chunks.Count; i++)
        {
            var (Text, Part) = chunks[i];

            ProjectDocumentChunk documentChunk = new()
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                VersionId = versionId,
                ChunkPart = Part,
                ChunkText = Text,
                Embedding = new PgVector(embeddings[i]),
                CreatedAt = DateTime.UtcNow
            };

            database.ProjectDocumentChunks.Add(documentChunk);
            searchDocs.Add(new ProjectDocumentChunkDocument
            {
                Id = documentChunk.Id.ToString(),
                DocumentId = documentId.ToString(),
                ProjectId = document.ProjectId.ToString(),
                VersionId = versionId.ToString(),
                ChunkPart = Part,
                ChunkText = Text,
                Vectors = new() { ["default"] = embeddings[i] }
            });
        }

        await database.ProjectDocumentChunks.Where(c => c.DocumentId == documentId).ExecuteDeleteAsync();
        await database.SaveChangesAsync();

        // Queued while holding the lock, so Meilisearch receives the swaps in the same order as Postgres
        // (it processes one index's tasks in order). If this throws, the transaction rolls back.
        await projectDocumentChunkSearchIndexService.DeleteByDocumentIdAsync(documentId);
        await projectDocumentChunkSearchIndexService.IndexChunksAsync(searchDocs);

        await transaction.CommitAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task DeletePointsByProjectDocumentIdAsync(Guid documentId)
    {
        await using var database = await dbFactory.CreateDbContextAsync();
        await database.ProjectDocumentChunks.Where(c => c.DocumentId == documentId).ExecuteDeleteAsync();
        await projectDocumentChunkSearchIndexService.DeleteByDocumentIdAsync(documentId);
    }
}
