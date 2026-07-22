namespace KnowledgeBank.Services.Search.Models;

/// <summary>
/// Result of a hybrid search operation, including items and metadata.
/// </summary>
public class HybridSearchResult
{
    /// <summary>
    /// The search result items with relevance scores.
    /// </summary>
    public LibraryItemWithScore[] Items { get; set; } = Array.Empty<LibraryItemWithScore>();

    /// <summary>
    /// Total count of matching items (before pagination).
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Duration of the search in ms
    /// </summary>
    public long DurationInMs { get; set; }

    /// <summary>
    /// Current page index (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The search query string.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Indicates this is a search result (vs regular query).
    /// </summary>
    public bool IsSearchResult { get; set; }

    /// <summary>
    /// Metadata about the search operation.
    /// </summary>
    public SearchMetadata Metadata { get; set; } = new();
}
