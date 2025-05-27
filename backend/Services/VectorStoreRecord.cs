using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace KnowledgeBank.Services;

// IsIndexed: Indicates whether the property should be indexed for filtering in cases where a database requires opting in to indexing per property. Default is false.
// IsFullTextIndexed: Indicates whether the property should be indexed for full text search for databases that support full text search. Default is false.

// enum for resource types
public enum ChunkType { ContentText, MetaData }


// Represents a record in the vector store.
public class ResourceVectorStoreRecord
{
    [VectorStoreKey]
    [TextSearchResultName]
    public required Guid Id { get; set; }

    [VectorStoreData(IsIndexed = true)]
    [TextSearchResultLink]
    public required string ResourceId { get; set; }

    // ----

    [VectorStoreData(IsIndexed = false)]
    public required string ChunkType { get; set; }

    [VectorStoreData(IsFullTextIndexed = true)]
    [TextSearchResultValue]
    public required string ChunkText { get; set; }

    [VectorStoreVector(Dimensions: RAGSystem.EMBEDDING_DIMENSIONS, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public required ReadOnlyMemory<float> ChunkEmbedding { get; set; }


    // [VectorStoreRecordData(IsFullTextIndexed = true)]
    // public string? Description { get; set; }

    // [VectorStoreRecordData(IsIndexed = true)]
    // public string[]? Tags { get; set; }


    // [VectorStoreRecordData(IsIndexed = true)]
    // public string? Text { get; set; }

    // [VectorStoreRecordData(IsFullTextIndexed = true)]
    // public string? Text2 { get; set; }
}


public class MetadataVectorStoreRecord
{
    [VectorStoreKey]
    public required Guid Id { get; set; }

    [VectorStoreData]
    public required string ResourceId { get; set; }

    // ----

    [VectorStoreVector(Dimensions: 768, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public required ReadOnlyMemory<float> Embedding { get; set; }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)