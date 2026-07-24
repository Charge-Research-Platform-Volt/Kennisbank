using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Services.Vector;

public interface IVectorStore
{
    /// <summary>
    /// Store chunks with embeddings for a resource
    /// </summary>
    Task CreateResourcePointsAsync(Guid resourceId, List<(string Text, ChunkType Type, int Part)> chunks);

    /// <summary>
    /// Store chunks with embeddings for an entity
    /// </summary>
    Task CreateEntityPointsAsync(Guid entityId, string entityType, List<(string Text, ChunkType Type, int Part)> chunks);

    /// <summary>
    /// Delete all chunks for a resource
    /// </summary>
    Task<bool> DeletePointsByResourceIdAsync(Guid resourceId);

    /// <summary>
    /// Delete all chunks for an entity
    /// </summary>
    /// <param name="entityId"></param>
    Task<bool> DeletePointsByEntityIdAsync(Guid entityId);

    /// <summary>
    /// Update the metadata chunk text and embedding for a resource
    /// </summary>
    Task<bool> UpdateMetadataPointAsync(Guid resourceId, string newChunkText);
}
