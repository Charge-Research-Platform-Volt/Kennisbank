namespace KnowledgeBank.Services.Search.Models;

/// <summary>
/// Metadata about a search operation, useful for debugging and analytics.
/// </summary>
public class SearchMetadata
{
    /// <summary>
    /// Number of results returned by semantic search.
    /// </summary>
    public int SemanticResultCount { get; set; }

    /// <summary>
    /// Number of results returned by text-based search.
    /// </summary>
    public int TextResultCount { get; set; }

    /// <summary>
    /// Number of results returned by Postgres full-text search.
    /// </summary>
    public int PostgresResultCount { get; set; }

    /// <summary>
    /// Number of unique results after fusion and deduplication.
    /// </summary>
    public int FusedResultCount { get; set; }

    /// <summary>
    /// Number of results remaining after applying relevance threshold.
    /// </summary>
    public int FilteredResultCount { get; set; }

    /// <summary>
    /// Average relevance score of the final results.
    /// </summary>
    public float AverageScore { get; set; }

    /// <summary>
    /// Total time taken for the search operation.
    /// </summary>
    public TimeSpan SearchDuration { get; set; }

    /// <summary>
    /// Whether semantic search was used.
    /// </summary>
    public bool UsedSemanticSearch { get; set; }

    /// <summary>
    /// Whether text-based search was used.
    /// </summary>
    public bool UsedTextSearch { get; set; }

    /// <summary>
    /// Whether Postgres search was used.
    /// </summary>
    public bool UsedPostgresSearch { get; set; }

    /// <summary>
    /// Any errors or warnings that occurred during search.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
