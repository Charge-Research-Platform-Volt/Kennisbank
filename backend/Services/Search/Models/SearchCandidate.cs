using KnowledgeBank.Models;

namespace KnowledgeBank.Services.Search.Models;

/// <summary>
/// Represents a search result candidate before final ranking and filtering.
/// Used internally during the search fusion process.
/// </summary>
public class SearchCandidate
{
    /// <summary>
    /// The resource ID of this candidate.
    /// </summary>
    public Guid ResourceId { get; set; }

    /// <summary>
    /// The full resource grid item data.
    /// </summary>
    public ResourceGridItem? Item { get; set; }

    /// <summary>
    /// Combined normalized score after fusion (0-1 range).
    /// </summary>
    public float FinalScore { get; set; }

    /// <summary>
    /// Score contributions from different search sources.
    /// </summary>
    public SearchProvenance Provenance { get; set; } = new();

    /// <summary>
    /// Matched text chunks from vector search (if applicable).
    /// </summary>
    public List<string> MatchedChunks { get; set; } = new();

    /// <summary>
    /// Rank position in semantic search results (1-based, null if not present).
    /// </summary>
    public int? SemanticRank { get; set; }

    /// <summary>
    /// Rank position in text search results (1-based, null if not present).
    /// </summary>
    public int? TextRank { get; set; }

    /// <summary>
    /// Rank position in Postgres search results (1-based, null if not present).
    /// </summary>
    public int? PostgresRank { get; set; }

    /// <summary>
    /// Reciprocal Rank Fusion score calculated from rank positions.
    /// </summary>
    public float RRFScore { get; set; }

    /// <summary>
    /// Additional boost applied based on contextual factors.
    /// </summary>
    public float BoostScore { get; set; }
}
