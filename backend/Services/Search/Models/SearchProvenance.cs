namespace KnowledgeBank.Services.Search.Models;

/// <summary>
/// Tracks the origin and scores of a search result across different search methods.
/// Provides transparency into how and why a result was matched.
/// </summary>
public class SearchProvenance
{
    /// <summary>
    /// Score from semantic (vector) search, if applicable.
    /// Range: 0-1, where 1 is most similar.
    /// </summary>
    public float? SemanticScore { get; set; }

    /// <summary>
    /// Score from text-based keyword search, if applicable.
    /// Range: 0-1, normalized from original search scores.
    /// </summary>
    public float? TextScore { get; set; }

    /// <summary>
    /// Score from Postgres full-text search, if applicable.
    /// Range: 0-1, normalized from ts_rank scores.
    /// </summary>
    public float? PostgresScore { get; set; }

    /// <summary>
    /// The primary source that contributed most to the final result.
    /// Values: "semantic", "text", "postgres", "hybrid"
    /// </summary>
    public string PrimarySource { get; set; } = "hybrid";

    /// <summary>
    /// Indicates whether this result appeared in multiple search sources.
    /// </summary>
    public bool IsHybridResult =>
        (SemanticScore.HasValue ? 1 : 0) +
        (TextScore.HasValue ? 1 : 0) +
        (PostgresScore.HasValue ? 1 : 0) > 1;

    /// <summary>
    /// Determines the primary source based on which score contributed most.
    /// </summary>
    public void DeterminePrimarySource()
    {
        float semantic = SemanticScore ?? 0;
        float text = TextScore ?? 0;
        float postgres = PostgresScore ?? 0;

        if (IsHybridResult)
        {
            PrimarySource = "hybrid";
        }
        else if (semantic > text && semantic > postgres)
        {
            PrimarySource = "semantic";
        }
        else if (text > semantic && text > postgres)
        {
            PrimarySource = "text";
        }
        else if (postgres > semantic && postgres > text)
        {
            PrimarySource = "postgres";
        }
        else
        {
            PrimarySource = semantic > 0 ? "semantic" : (text > 0 ? "text" : "postgres");
        }
    }
}
