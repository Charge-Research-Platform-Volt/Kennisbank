using System.Linq.Expressions;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class JournalService(DatabaseContext db)
{
    #region Queries

    public async Task<Journal?> GetByIdAsync(Guid id, bool includeRelations = false)
    {
        IQueryable<Journal> query = db.Journals;

        if (includeRelations)
            query = query.Include(j => j.Resources);

        return await query.FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<Journal[]> GetAllAsync(Expression<Func<Journal, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<Journal> query = db.Journals;

        if (includeRelations)
            query = query.Include(j => j.Resources);

        if (predicate != null)
            query = query.Where(predicate);

        return await query.OrderBy(j => j.Name).ToArrayAsync();
    }

    public async Task<(Journal[] Items, int TotalCount)> GetPageAsync(int page, int pageSize, Expression<Func<Journal, bool>>? predicate = null, bool includeRelations = false)
    {
        IQueryable<Journal> query = db.Journals;

        if (includeRelations)
            query = query.Include(r => r.Resources);

        if (predicate != null)
            query = query.Where(predicate);

        int totalCount = await query.CountAsync();

        Journal[] items = await query
            .OrderBy(j => j.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        return (items, totalCount);
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await db.Journals.AnyAsync(j => j.Id == id);

    public async Task<bool> ExistsAsync(Expression<Func<Journal, bool>> predicate)
        => await db.Journals.AnyAsync(predicate);

    public async Task<int> CountAsync(Expression<Func<Journal, bool>>? predicate = null)
    {
        IQueryable<Journal> query = db.Journals;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public async Task<Guid?> FindIdByNameAsync(string name)
        => await db.Journals.Where(j => j.Name == name).Select(j => (Guid?)j.Id).FirstOrDefaultAsync();

    #endregion

    #region Commands

    public async Task<Guid> CreateAsync(string name, Guid createdBy)
    {
        Journal journal = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };

        db.Journals.Add(journal);
        await db.SaveChangesAsync();
        return journal.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, Action<Journal> update)
    {
        Journal? journal = await db.Journals.FindAsync(id);
        if (journal == null) return false;

        update(journal);
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Journal? journal = await db.Journals.FindAsync(id);
        if (journal == null) return false;

        db.Journals.Remove(journal);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task MergeAsync(Guid keepId, Guid removeId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.Resources.Where(r => r.JournalId == removeId).ExecuteUpdateAsync(s => s.SetProperty(r => r.JournalId, keepId));

        Journal removeJournal = await db.Journals.FindAsync(removeId) ?? throw new InvalidOperationException($"Journal {removeId} not found during merge.");

        db.Journals.Remove(removeJournal);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    #endregion

    #region Suggestions

    public async Task<List<MergeSuggestion>> GetMergeSuggestionsAsync(float threshold = 0.6f, int limit = 20)
    {
        return await db.Database.SqlQuery<MergeSuggestion>($"""
            SELECT
                a.id        AS "Id1",
                a.name      AS "Name1",
                b.id        AS "Id2",
                b.name      AS "Name2",
                similarity(a.name, b.name) AS "Score"
            FROM journals a
            JOIN journals b ON a.id < b.id
            WHERE similarity(a.name, b.name) > {threshold}
            ORDER BY "Score" DESC
            LIMIT {limit}
        """).ToListAsync();
    }

    #endregion
}