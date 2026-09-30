using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Services.Vector;

public interface IVectorStore
{
    /// <summary>
    /// Embed the given chunks, then replace all existing chunks of the resource with them.
    /// Existing chunks are left untouched if embedding fails.
    /// </summary>
    Task ReplaceResourcePointsAsync(Guid resourceId, List<(string Text, ChunkType Type, int Part)> chunks);

    /// <summary>
    /// Embed the given chunks, then replace all existing chunks of the entity with them.
    /// Existing chunks are left untouched if embedding fails.
    /// </summary>
    Task ReplaceEntityPointsAsync(Guid entityId, string entityType, List<(string Text, ChunkType Type, int Part)> chunks);

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

    /// <summary>
    /// Get the stored text of a resource's content chunks, in document order
    /// </summary>
    Task<List<string>> GetContentChunkTextsAsync(Guid resourceId);
}
