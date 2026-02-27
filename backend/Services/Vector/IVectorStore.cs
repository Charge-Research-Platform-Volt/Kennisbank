namespace KnowledgeBank.Services.Vector;

public class VectorSearchResult 
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public string ChunkType { get; set; } = string.Empty;
    public int ChunkPart { get; set; }
    public float Score { get; set; }
}

public interface IVectorStore
{
    /// <summary>
    /// Store chunks with embeddings for a resource
    /// </summary>
    Task CreateResourcePointsAsync(Guid resourceId, List<(string Text, ChunkType Type, int Part)> chunks);

    /// <summary>
    /// Store chunks with embeddings for an entity
    /// </summary>
    Task CreateEntityPointsAsync(Guid entityId, List<(string Text, ChunkType Type, int Part)> chunks);

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
    /// Semantic search using vector similarity
    /// </summary>
    Task<List<VectorSearchResult>> SemanticSearchAsync(float[] queryEmbedding, int limit, float scoreThreshold);

    /// <summary>
    /// Text-based search using trigrams
    /// </summary>
    Task<List<VectorSearchResult>> TextSearchAsync(string query, int limit);

    /// <summary>
    /// Get all chunks for a resource
    /// </summary>
    Task<List<VectorSearchResult>> GetChunksByResourceIdAsync(Guid resourceId);

    /// <summary>
    /// Find similar resources based on a resource's embeddings
    /// </summary>
    Task<List<VectorSearchResult>> RecommendSimilarAsync(Guid resourceId, int limit, float scoreThreshold);
}