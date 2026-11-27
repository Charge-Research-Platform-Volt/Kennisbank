using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Services.Vector;

/// <summary>A chunk to embed. StartChar/EndChar locate content chunks in the resource's stored text</summary>
public record ResourceChunkInput(string Text, ChunkType Type, int Part, int? StartChar = null, int? EndChar = null);

public interface IVectorStore
{
    /// <summary>
    /// Embed the given chunks, then replace all existing chunks of the resource with them, and its stored text with
    /// <paramref name="text"/> in the same transaction (chunk positions point into it; null removes it).
    /// Existing chunks are left untouched if embedding fails.
    /// </summary>
    Task ReplaceResourcePointsAsync(Guid resourceId, List<ResourceChunkInput> chunks, ResourceText? text);

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
    /// Get the stored text of a resource's content chunks, in document order. Only a fallback for resources indexed
    /// before full texts were stored; can go once every resource has one
    /// </summary>
    Task<List<string>> GetContentChunkTextsAsync(Guid resourceId);

    /// <summary>The resource's stored full text, or null when it has none</summary>
    Task<ResourceText?> GetResourceTextAsync(Guid resourceId);

    /// <summary>
    /// Replaces a document's chunks with those of the given version, but only while that version is still the
    /// document's latest. Returns false (and changes nothing) when a newer version exists or the document is gone.
    /// </summary>
    Task<bool> ReplaceProjectDocumentPointsAsync(Guid documentId, Guid versionId, List<(string Text, int Part)> chunks);

    /// <summary>
    /// Delete all chunks for a project document
    /// </summary>
    Task DeletePointsByProjectDocumentIdAsync(Guid documentId);
}
