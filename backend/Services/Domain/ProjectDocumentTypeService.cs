using KnowledgeBank.Data;
using KnowledgeBank.Models.ProjectDocuments;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.Domain;

public class ProjectDocumentTypeService(DatabaseContext db)
{
    public async Task<ProjectDocumentType[]> GetAllAsync()
        => await db.ProjectDocumentTypes.AsNoTracking().OrderBy(t => t.Name).ToArrayAsync();

    public async Task<ProjectDocumentType?> GetByIdAsync(Guid id)
        => await db.ProjectDocumentTypes.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<bool> ExistsAsync(Guid id)
        => await db.ProjectDocumentTypes.AnyAsync(t => t.Id == id);

    // Case-insensitive, so "Minutes" and "minutes" don't both end up in the list
    public async Task<Guid?> FindIdByNameAsync(string name)
    {
        string normalized = name.Trim().ToLower();
        return await db.ProjectDocumentTypes
            .Where(t => t.Name.ToLower() == normalized)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> CountUsageAsync(Guid id)
        => await db.ProjectDocuments.CountAsync(d => d.TypeId == id);

    public async Task<Guid> CreateAsync(string name, Guid createdBy)
    {
        ProjectDocumentType type = new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };

        db.ProjectDocumentTypes.Add(type);
        await db.SaveChangesAsync();
        return type.Id;
    }

    public async Task DeleteAsync(Guid id)
        => await db.ProjectDocumentTypes.Where(t => t.Id == id).ExecuteDeleteAsync();
}