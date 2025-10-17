using KnowledgeBank.Services.Search.Models;
using Serilog;

namespace KnowledgeBank.Services.Search;

/// <summary>
/// Handles fusion of search results from multiple sources using various algorithms.
/// Implements Reciprocal Rank Fusion (RRF) and score normalization.
/// </summary>
public class SearchFusionEngine
{
    private readonly Serilog.ILogger _logger;
    private readonly HybridSearchConfig _config;

    public SearchFusionEngine(HybridSearchConfig config)
    {
        _config = config;
        _logger = Log.ForContext<SearchFusionEngine>();
    }

    /// <summary>
    /// Fuses search candidates from multiple sources into a unified ranked list.
    /// Uses Reciprocal Rank Fusion (RRF) combined with weighted score normalization.
    /// </summary>
    /// <param name="candidates">List of search candidates from all sources</param>
    /// <returns>Deduplicated and fused list of candidates with combined scores</returns>
    public List<SearchCandidate> FuseResults(List<SearchCandidate> candidates)
    {
        if (candidates.Count == 0)
            return candidates;

        _logger.Debug("Fusing {Count} search candidates", candidates.Count);

        // Group candidates by resource ID for deduplication
        var groupedCandidates = candidates
            .GroupBy(c => c.ResourceId)
            .Select(g => MergeCandidates(g.ToList()))
            .ToList();

        _logger.Debug("After deduplication: {Count} unique candidates", groupedCandidates.Count);

        // Normalize scores within each source
        NormalizeScores(groupedCandidates);

        // Calculate RRF scores
        CalculateRRFScores(groupedCandidates);

        // Calculate final combined scores
        CalculateFinalScores(groupedCandidates);

        // Sort by final score
        var fusedResults = groupedCandidates
            .OrderByDescending(c => c.FinalScore)
            .ToList();

        _logger.Information("Fusion complete. Top score: {TopScore:F3}, Bottom score: {BottomScore:F3}",
            fusedResults.FirstOrDefault()?.FinalScore ?? 0,
            fusedResults.LastOrDefault()?.FinalScore ?? 0);

        return fusedResults;
    }

    /// <summary>
    /// Merges multiple candidates for the same resource into a single candidate.
    /// Combines scores, chunks, and rank positions from all sources.
    /// </summary>
    private SearchCandidate MergeCandidates(List<SearchCandidate> candidates)
    {
        var merged = candidates.First();

        // Merge provenance scores
        foreach (var candidate in candidates)
        {
            if (candidate.Provenance.SemanticScore.HasValue)
                merged.Provenance.SemanticScore = Math.Max(
                    merged.Provenance.SemanticScore ?? 0,
                    candidate.Provenance.SemanticScore.Value);

            if (candidate.Provenance.TextScore.HasValue)
                merged.Provenance.TextScore = Math.Max(
                    merged.Provenance.TextScore ?? 0,
                    candidate.Provenance.TextScore.Value);

            if (candidate.Provenance.PostgresScore.HasValue)
                merged.Provenance.PostgresScore = Math.Max(
                    merged.Provenance.PostgresScore ?? 0,
                    candidate.Provenance.PostgresScore.Value);

            // Take the best rank from each source
            if (candidate.SemanticRank.HasValue)
                merged.SemanticRank = merged.SemanticRank.HasValue
                    ? Math.Min(merged.SemanticRank.Value, candidate.SemanticRank.Value)
                    : candidate.SemanticRank;

            if (candidate.TextRank.HasValue)
                merged.TextRank = merged.TextRank.HasValue
                    ? Math.Min(merged.TextRank.Value, candidate.TextRank.Value)
                    : candidate.TextRank;

            if (candidate.PostgresRank.HasValue)
                merged.PostgresRank = merged.PostgresRank.HasValue
                    ? Math.Min(merged.PostgresRank.Value, candidate.PostgresRank.Value)
                    : candidate.PostgresRank;

            // Merge chunks (deduplicate)
            foreach (var chunk in candidate.MatchedChunks)
            {
                if (!merged.MatchedChunks.Contains(chunk))
                    merged.MatchedChunks.Add(chunk);
            }
        }

        // Limit chunks to configured maximum
        if (merged.MatchedChunks.Count > _config.MaxChunksPerResource)
        {
            merged.MatchedChunks = merged.MatchedChunks
                .Take(_config.MaxChunksPerResource)
                .ToList();
        }

        return merged;
    }

    /// <summary>
    /// Normalizes scores from each search source to 0-1 range using min-max normalization.
    /// </summary>
    private void NormalizeScores(List<SearchCandidate> candidates)
    {
        // Get min/max for each score type
        var semanticScores = candidates
            .Where(c => c.Provenance.SemanticScore.HasValue)
            .Select(c => c.Provenance.SemanticScore!.Value)
            .ToList();

        var textScores = candidates
            .Where(c => c.Provenance.TextScore.HasValue)
            .Select(c => c.Provenance.TextScore!.Value)
            .ToList();

        var postgresScores = candidates
            .Where(c => c.Provenance.PostgresScore.HasValue)
            .Select(c => c.Provenance.PostgresScore!.Value)
            .ToList();

        // Normalize each score type
        if (semanticScores.Any())
        {
            float minSemantic = semanticScores.Min();
            float maxSemantic = semanticScores.Max();
            float rangeSemantic = maxSemantic - minSemantic;

            if (rangeSemantic > 0)
            {
                foreach (var candidate in candidates.Where(c => c.Provenance.SemanticScore.HasValue))
                {
                    candidate.Provenance.SemanticScore =
                        (candidate.Provenance.SemanticScore.Value - minSemantic) / rangeSemantic;
                }
            }
        }

        if (textScores.Any())
        {
            float minText = textScores.Min();
            float maxText = textScores.Max();
            float rangeText = maxText - minText;

            if (rangeText > 0)
            {
                foreach (var candidate in candidates.Where(c => c.Provenance.TextScore.HasValue))
                {
                    candidate.Provenance.TextScore =
                        (candidate.Provenance.TextScore.Value - minText) / rangeText;
                }
            }
        }

        if (postgresScores.Any())
        {
            float minPostgres = postgresScores.Min();
            float maxPostgres = postgresScores.Max();
            float rangePostgres = maxPostgres - minPostgres;

            if (rangePostgres > 0)
            {
                foreach (var candidate in candidates.Where(c => c.Provenance.PostgresScore.HasValue))
                {
                    candidate.Provenance.PostgresScore =
                        (candidate.Provenance.PostgresScore.Value - minPostgres) / rangePostgres;
                }
            }
        }
    }

    /// <summary>
    /// Calculates Reciprocal Rank Fusion (RRF) scores for all candidates.
    /// RRF formula: 1 / (k + rank), where k is a constant (default 60).
    /// </summary>
    private void CalculateRRFScores(List<SearchCandidate> candidates)
    {
        int k = _config.RRFConstant;

        foreach (var candidate in candidates)
        {
            float rrfScore = 0f;

            if (candidate.SemanticRank.HasValue)
                rrfScore += 1.0f / (k + candidate.SemanticRank.Value);

            if (candidate.TextRank.HasValue)
                rrfScore += 1.0f / (k + candidate.TextRank.Value);

            if (candidate.PostgresRank.HasValue)
                rrfScore += 1.0f / (k + candidate.PostgresRank.Value);

            candidate.RRFScore = rrfScore;
        }
    }

    /// <summary>
    /// Calculates final combined scores using weighted average of normalized scores and RRF.
    /// </summary>
    private void CalculateFinalScores(List<SearchCandidate> candidates)
    {
        foreach (var candidate in candidates)
        {
            float weightedScore = 0f;
            float totalWeight = 0f;

            // Add weighted normalized scores
            if (candidate.Provenance.SemanticScore.HasValue)
            {
                weightedScore += candidate.Provenance.SemanticScore.Value * _config.SemanticWeight;
                totalWeight += _config.SemanticWeight;
            }

            if (candidate.Provenance.TextScore.HasValue)
            {
                weightedScore += candidate.Provenance.TextScore.Value * _config.TextWeight;
                totalWeight += _config.TextWeight;
            }

            if (candidate.Provenance.PostgresScore.HasValue)
            {
                weightedScore += candidate.Provenance.PostgresScore.Value * _config.PostgresWeight;
                totalWeight += _config.PostgresWeight;
            }

            // Normalize by actual weights used
            if (totalWeight > 0)
                weightedScore /= totalWeight;

            // Combine weighted score (70%) with RRF score (30%)
            // This gives more weight to actual similarity scores while still considering rank positions
            candidate.FinalScore = (weightedScore * 0.7f) + (candidate.RRFScore * 0.3f);

            // Add any boost scores
            candidate.FinalScore += candidate.BoostScore;

            // Clamp to [0, 1] range
            candidate.FinalScore = Math.Clamp(candidate.FinalScore, 0f, 1f);

            // Determine primary source
            candidate.Provenance.DeterminePrimarySource();
        }
    }
}
