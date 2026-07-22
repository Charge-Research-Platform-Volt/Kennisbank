using KnowledgeBank.Models;

namespace KnowledgeBank.Services.Search.Models;

/// <summary>
/// Extended library item that includes search relevance information.
/// </summary>
public class LibraryItemWithScore : LibraryItem
{
    /// <summary>
    /// Final combined relevance score (0-1 range).
    /// Higher scores indicate better matches to the search query.
    /// </summary>
    public float RelevanceScore { get; set; }

    /// <summary>
    /// Information about which search methods matched this result.
    /// </summary>
    public SearchProvenance Provenance { get; set; } = new();

    /// <summary>
    /// Text chunks that matched the search query (from vector search).
    /// </summary>
    public List<string> MatchedChunks { get; set; } = new();
}
