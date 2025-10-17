namespace KnowledgeBank.Services.Search.Models;

/// <summary>
/// Configuration settings for the hybrid search system.
/// Controls search weights, thresholds, and behavior parameters.
/// </summary>
public class HybridSearchConfig
{
    /// <summary>
    /// Weight applied to semantic (vector) search results in the final score.
    /// Default: 0.6 (60% weight)
    /// </summary>
    public float SemanticWeight { get; set; } = 0.6f;

    /// <summary>
    /// Weight applied to text-based (keyword) search results in the final score.
    /// Default: 0.3 (30% weight)
    /// </summary>
    public float TextWeight { get; set; } = 0.3f;

    /// <summary>
    /// Weight applied to Postgres full-text search results in the final score.
    /// Default: 0.1 (10% weight)
    /// </summary>
    public float PostgresWeight { get; set; } = 0.1f;

    /// <summary>
    /// Minimum relevance score (0-1) required for a result to be included.
    /// Results below this threshold are filtered out.
    /// Default: 0.15
    /// </summary>
    public float MinimumRelevanceScore { get; set; } = 0.15f;

    /// <summary>
    /// Minimum similarity score for semantic search results from Qdrant.
    /// Default: 0.3
    /// </summary>
    public float SemanticScoreThreshold { get; set; } = 0.3f;

    /// <summary>
    /// Multiplier for candidate results to fetch before fusion.
    /// If pageSize is 20, we fetch (20 * CandidateMultiplier) from each source.
    /// Default: 3
    /// </summary>
    public int CandidateMultiplier { get; set; } = 3;

    /// <summary>
    /// Maximum number of candidates to fetch from each search source.
    /// Default: 500
    /// </summary>
    public int MaxCandidatesPerSource { get; set; } = 500;

    /// <summary>
    /// Enable re-ranking of results after fusion.
    /// Default: true
    /// </summary>
    public bool EnableReranking { get; set; } = true;

    /// <summary>
    /// Enable contextual boosting (e.g., exact matches, recent dates).
    /// Default: true
    /// </summary>
    public bool EnableBoosting { get; set; } = true;

    /// <summary>
    /// Fall back to Postgres search when Qdrant is unavailable.
    /// Default: true
    /// </summary>
    public bool UsePostgresWhenQdrantFails { get; set; } = true;

    /// <summary>
    /// Boost factor for exact phrase matches in resource name.
    /// Default: 0.2
    /// </summary>
    public float ExactNameMatchBoost { get; set; } = 0.2f;

    /// <summary>
    /// Boost factor for resources with recent publication dates.
    /// Default: 0.1
    /// </summary>
    public float RecentPublicationBoost { get; set; } = 0.1f;

    /// <summary>
    /// Number of days to consider a publication "recent" for boosting.
    /// Default: 365 (1 year)
    /// </summary>
    public int RecentPublicationDays { get; set; } = 365;

    /// <summary>
    /// The constant k used in Reciprocal Rank Fusion (RRF).
    /// Default: 60 (standard RRF value)
    /// </summary>
    public int RRFConstant { get; set; } = 60;

    /// <summary>
    /// Maximum number of chunks to include per resource in search results.
    /// Default: 4
    /// </summary>
    public int MaxChunksPerResource { get; set; } = 4;

    /// <summary>
    /// Validates that all configuration values are within acceptable ranges.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration values are invalid.</exception>
    public void Validate()
    {
        if (SemanticWeight < 0 || SemanticWeight > 1)
            throw new ArgumentException("SemanticWeight must be between 0 and 1", nameof(SemanticWeight));

        if (TextWeight < 0 || TextWeight > 1)
            throw new ArgumentException("TextWeight must be between 0 and 1", nameof(TextWeight));

        if (PostgresWeight < 0 || PostgresWeight > 1)
            throw new ArgumentException("PostgresWeight must be between 0 and 1", nameof(PostgresWeight));

        if (MinimumRelevanceScore < 0 || MinimumRelevanceScore > 1)
            throw new ArgumentException("MinimumRelevanceScore must be between 0 and 1", nameof(MinimumRelevanceScore));

        if (SemanticScoreThreshold < 0 || SemanticScoreThreshold > 1)
            throw new ArgumentException("SemanticScoreThreshold must be between 0 and 1", nameof(SemanticScoreThreshold));

        if (CandidateMultiplier < 1)
            throw new ArgumentException("CandidateMultiplier must be at least 1", nameof(CandidateMultiplier));

        if (RRFConstant < 1)
            throw new ArgumentException("RRFConstant must be at least 1", nameof(RRFConstant));
    }
}
