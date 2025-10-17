using KnowledgeBank.Services.Search.Models;
using Serilog;

namespace KnowledgeBank.Services.Search;

/// <summary>
/// Handles re-ranking and boosting of search results based on contextual factors.
/// </summary>
public class SearchRanker
{
    private readonly Serilog.ILogger _logger;
    private readonly HybridSearchConfig _config;

    public SearchRanker(HybridSearchConfig config)
    {
        _config = config;
        _logger = Log.ForContext<SearchRanker>();
    }

    /// <summary>
    /// Applies contextual boosting to search candidates based on various factors.
    /// </summary>
    /// <param name="candidates">List of search candidates to boost</param>
    /// <param name="searchQuery">The original search query</param>
    public void ApplyBoosting(List<SearchCandidate> candidates, string searchQuery)
    {
        if (!_config.EnableBoosting || candidates.Count == 0)
            return;

        _logger.Debug("Applying contextual boosting to {Count} candidates", candidates.Count);

        string queryLower = searchQuery.ToLowerInvariant();

        foreach (var candidate in candidates)
        {
            float boost = 0f;

            if (candidate.Item != null)
            {
                // Boost for exact name match
                if (candidate.Item.Name.Equals(searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    boost += 2.0f; // Massive boost for exact name match
                    _logger.Debug("Exact name match boost applied to {ResourceId}: +2.0",
                        candidate.ResourceId);
                }
                // Boost for name starting with query (very strong boost for prefix matches)
                else if (candidate.Item.Name.StartsWith(searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    boost += 1.5f; // Very strong boost for name prefix match
                    _logger.Debug("Name prefix match boost applied to {ResourceId}: +1.5",
                        candidate.ResourceId);
                }
                // Boost for any word in name starting with query (e.g., "berr" matches "Berrie van der Molen")
                else if (candidate.Item.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Any(word => word.StartsWith(searchQuery, StringComparison.OrdinalIgnoreCase)))
                {
                    boost += 1.0f; // Strong boost for word prefix match - names are HIGH priority
                    _logger.Debug("Name word prefix match boost applied to {ResourceId}: +1.0",
                        candidate.ResourceId);
                }
                // Boost for name containing all query words
                else if (ContainsAllWords(candidate.Item.Name, queryLower))
                {
                    boost += 0.5f; // Medium boost for word containment in name
                    _logger.Debug("Name contains all words boost applied to {ResourceId}: +0.5",
                        candidate.ResourceId);
                }

                // Boost for recent publication date
                if (candidate.Item.PublicationDate.HasValue)
                {
                    DateTime cutoffDate = DateTime.UtcNow.AddDays(-_config.RecentPublicationDays);
                    if (candidate.Item.PublicationDate.Value >= cutoffDate)
                    {
                        // Scale boost based on how recent (linear decay)
                        double daysSincePublication = (DateTime.UtcNow - candidate.Item.PublicationDate.Value).TotalDays;
                        double recencyFactor = 1.0 - (daysSincePublication / _config.RecentPublicationDays);
                        float recencyBoost = (float)(recencyFactor * _config.RecentPublicationBoost);
                        boost += recencyBoost;

                        _logger.Debug("Recency boost applied to {ResourceId}: +{Boost}",
                            candidate.ResourceId, recencyBoost);
                    }
                }

                // Boost for having multiple matching chunks
                if (candidate.MatchedChunks.Count > 2)
                {
                    float chunkBoost = Math.Min(candidate.MatchedChunks.Count * 0.02f, 0.1f); // Max 0.1 boost
                    boost += chunkBoost;
                }

                // Boost for description containing query
                if (!string.IsNullOrEmpty(candidate.Item.Description) &&
                    candidate.Item.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    boost += 0.05f;
                }
            }

            // CRITICAL: Boost for keyword matches in matched chunks
            // This is often the most important signal - if the exact search terms appear in the chunks
            if (candidate.MatchedChunks.Count > 0)
            {
                int chunksWithKeywords = 0;
                string[] queryWords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var chunk in candidate.MatchedChunks)
                {
                    string chunkLower = chunk.ToLowerInvariant();

                    // Check for exact phrase match in chunk
                    if (chunkLower.Contains(queryLower))
                    {
                        chunksWithKeywords++;
                        boost += 0.25f; // Strong boost for exact phrase match
                        _logger.Debug("Exact phrase match in chunk for {ResourceId}: +0.25",
                            candidate.ResourceId);
                    }
                    // Check if chunk contains all query words (even if not as exact phrase)
                    else if (queryWords.Length > 1 && queryWords.All(word => chunkLower.Contains(word)))
                    {
                        chunksWithKeywords++;
                        boost += 0.15f; // Medium boost for all words present
                        _logger.Debug("All query words found in chunk for {ResourceId}: +0.15",
                            candidate.ResourceId);
                    }
                    // Check if chunk contains at least half the query words
                    else if (queryWords.Length > 1)
                    {
                        int matchingWords = queryWords.Count(word => chunkLower.Contains(word));
                        if (matchingWords >= queryWords.Length / 2)
                        {
                            chunksWithKeywords++;
                            float partialBoost = 0.08f * (matchingWords / (float)queryWords.Length);
                            boost += partialBoost;
                            _logger.Debug("Partial query words found in chunk for {ResourceId}: +{Boost:F3}",
                                candidate.ResourceId, partialBoost);
                        }
                    }
                }

                // Additional boost if multiple chunks contain keywords
                if (chunksWithKeywords > 1)
                {
                    float multiChunkBoost = Math.Min(chunksWithKeywords * 0.05f, 0.15f);
                    boost += multiChunkBoost;
                    _logger.Debug("Multiple chunks with keywords for {ResourceId}: +{Boost:F3}",
                        candidate.ResourceId, multiChunkBoost);
                }
            }

            candidate.BoostScore = boost;
        }

        int boostedCount = candidates.Count(c => c.BoostScore > 0);
        _logger.Information("Boosting applied to {BoostedCount}/{TotalCount} candidates",
            boostedCount, candidates.Count);
    }

    /// <summary>
    /// Re-ranks candidates after initial fusion, applying additional ranking signals.
    /// </summary>
    /// <param name="candidates">List of candidates to re-rank</param>
    /// <param name="searchQuery">The original search query</param>
    /// <returns>Re-ranked list of candidates</returns>
    public List<SearchCandidate> ReRank(List<SearchCandidate> candidates, string searchQuery)
    {
        if (!_config.EnableReranking || candidates.Count == 0)
            return candidates;

        _logger.Debug("Re-ranking {Count} candidates", candidates.Count);

        // Apply boosting
        ApplyBoosting(candidates, searchQuery);

        // Recalculate final scores with boosts
        foreach (var candidate in candidates)
        {
            float scoreBeforeBoost = candidate.FinalScore;
            candidate.FinalScore += candidate.BoostScore;
            // Only clamp the minimum to 0, allow scores to go above 1.0 so boosts are effective
            candidate.FinalScore = Math.Max(candidate.FinalScore, 0f);

            // Log score changes for debugging
            if (candidate.Item != null)
            {
                _logger.Debug("Score for '{Name}': Base={BaseScore:F3}, Boost={Boost:F3}, Final={FinalScore:F3}",
                    candidate.Item.Name, scoreBeforeBoost, candidate.BoostScore, candidate.FinalScore);
            }
        }

        // Sort by final score
        var reranked = candidates
            .OrderByDescending(c => c.FinalScore)
            .ToList();

        _logger.Information("Re-ranking complete. Score range: {Min:F3} - {Max:F3}",
            reranked.LastOrDefault()?.FinalScore ?? 0,
            reranked.FirstOrDefault()?.FinalScore ?? 0);

        // Log top 3 results for debugging
        _logger.Information("Top 3 results after re-ranking:");
        for (int i = 0; i < Math.Min(3, reranked.Count); i++)
        {
            var item = reranked[i];
            _logger.Information("  {Rank}. '{Name}' - Score: {Score:F3} (Boost: {Boost:F3})",
                i + 1, item.Item?.Name ?? "Unknown", item.FinalScore, item.BoostScore);
        }

        return reranked;
    }

    /// <summary>
    /// Filters candidates by minimum relevance score threshold.
    /// </summary>
    /// <param name="candidates">List of candidates to filter</param>
    /// <returns>Filtered list of candidates above the threshold</returns>
    public List<SearchCandidate> FilterByRelevance(List<SearchCandidate> candidates)
    {
        int originalCount = candidates.Count;

        var filtered = candidates
            .Where(c => c.FinalScore >= _config.MinimumRelevanceScore)
            .ToList();

        int filteredCount = originalCount - filtered.Count;
        if (filteredCount > 0)
        {
            _logger.Information("Filtered out {Count} candidates below threshold {Threshold:F3}",
                filteredCount, _config.MinimumRelevanceScore);
        }

        return filtered;
    }

    /// <summary>
    /// Checks if a text contains all words from a query.
    /// </summary>
    private bool ContainsAllWords(string text, string query)
    {
        string textLower = text.ToLowerInvariant();
        string[] queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return queryWords.All(word => textLower.Contains(word));
    }
}
