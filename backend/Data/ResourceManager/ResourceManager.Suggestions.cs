using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Data
{
    // This part is for finding merge suggestions using trigram similarity (pg_trgm)
    public partial class ResourceManager
    {
        /// <summary>
        /// Returns pairs of tags whose names are similar above the given threshold,
        /// ordered by similarity score descending.
        /// </summary>
        public async Task<List<MergeSuggestion>> GetTagMergeSuggestionsAsync(float threshold = 0.6f, int limit = 20)
        {
            return await database.Database.SqlQuery<MergeSuggestion>($"""
                SELECT
                    a.id        AS "Id1",
                    a.name      AS "Name1",
                    b.id        AS "Id2",
                    b.name      AS "Name2",
                    similarity(a.name, b.name) AS "Score"
                FROM tags a
                JOIN tags b ON a.id < b.id
                WHERE similarity(a.name, b.name) > {threshold}
                ORDER BY "Score" DESC
                LIMIT {limit}
                """).ToListAsync();
        }

        /// <summary>
        /// Returns pairs of regions whose names are similar above the given threshold,
        /// ordered by similarity score descending.
        /// </summary>
        public async Task<List<MergeSuggestion>> GetRegionMergeSuggestionsAsync(float threshold = 0.6f, int limit = 20)
        {
            return await database.Database.SqlQuery<MergeSuggestion>($"""
                SELECT
                    a.id        AS "Id1",
                    a.name      AS "Name1",
                    b.id        AS "Id2",
                    b.name      AS "Name2",
                    similarity(a.name, b.name) AS "Score"
                FROM regions a
                JOIN regions b ON a.id < b.id
                WHERE similarity(a.name, b.name) > {threshold}
                ORDER BY "Score" DESC
                LIMIT {limit}
                """).ToListAsync();
        }

        /// <summary>
        /// Returns pairs of resource types whose names are similar above the given threshold,
        /// ordered by similarity score descending.
        /// </summary>
        public async Task<List<MergeSuggestion>> GetResourceTypeMergeSuggestionsAsync(float threshold = 0.6f, int limit = 20)
        {
            return await database.Database.SqlQuery<MergeSuggestion>($"""
                SELECT
                    a.id        AS "Id1",
                    a.name      AS "Name1",
                    b.id        AS "Id2",
                    b.name      AS "Name2",
                    similarity(a.name, b.name) AS "Score"
                FROM "resource-types" a
                JOIN "resource-types" b ON a.id < b.id
                WHERE similarity(a.name, b.name) > {threshold}
                ORDER BY "Score" DESC
                LIMIT {limit}
                """).ToListAsync();
        }
    }
}
