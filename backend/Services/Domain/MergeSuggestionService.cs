using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class MergeSuggestionService(DatabaseContext db)
{
    public async Task DismissAsync(string entityType, Guid id1, Guid id2)
    {
        // Sort IDs lower first, higher second (this is reflecting the merge suggestions logic)
        var (lo, hi) = Normalize(id1, id2);

        bool exists = await db.DismissedMergeSuggestions.AnyAsync(d => d.EntityType == entityType && d.Id1 == lo && d.Id2 == hi);
        if (exists) return;

        db.DismissedMergeSuggestions.Add(new DismissedMergeSuggestion
        {
            EntityType = entityType,
            Id1 = lo,
            Id2 = hi
        });

        await db.SaveChangesAsync();
    }

    public async Task UndoAsync(string entityType, Guid id1, Guid id2)
    {
        var (lo, hi) = Normalize(id1, id2);

        DismissedMergeSuggestion? row = await db.DismissedMergeSuggestions.FirstOrDefaultAsync(d => d.EntityType == entityType && d.Id1 == lo && d.Id2 == hi);
        if (row == null) return;

        db.DismissedMergeSuggestions.Remove(row);
        await db.SaveChangesAsync();
    }

    private static (Guid, Guid) Normalize(Guid id1, Guid id2)
        => id1 < id2 ? (id1, id2) : (id2, id1);
}