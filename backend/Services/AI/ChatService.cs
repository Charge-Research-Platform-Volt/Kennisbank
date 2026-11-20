using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.Domain;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.AI;

public class ChatService(DatabaseContext db, ProjectService projectService)
{
    /// <summary>
    /// A project's chats live on its root, so the same history shows on every folder page. Null when the project doesn't exist
    /// </summary>
    public async Task<Dictionary<DateTime, Chats[]>?> GetChatsGroupedByDateAsync(Guid userId, Guid? projectId)
    {
        Guid? rootId = null;
        if (projectId is Guid id)
        {
            if (await projectService.ResolveLocationAsync(id) is not { } location) return null;
            rootId = location.RootId;
        }

        return (await db.Chats
            .Where(c => c.UserId == userId && c.ProjectId == rootId)
            .OrderByDescending(c => c.CreatedOn)
            .ToArrayAsync())
            .GroupBy(c => c.CreatedOn.Date)
            .OrderByDescending(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedOn).ToArray());
    }

    public async Task<Chats?> GetByIdAsync(Guid id)
        => await db.Chats.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<(Messages[] Items, bool HasMore)> GetMessagesAsync(Guid chatId, DateTime? before = null, int limit = 50)
    {
        IQueryable<Messages> query = db.Messages.Where(m => m.ChatId == chatId);
        if (before != null)
            query = query.Where(m => m.CreatedOn < before);

        Messages[] page = await query.OrderByDescending(m => m.CreatedOn).Take(limit + 1).ToArrayAsync();

        bool hasMore = page.Length > limit;
        Messages[] items = page.Take(limit).OrderBy(m => m.CreatedOn).ToArray();

        return (items, hasMore);
    }

    public async Task<Messages[]> GetMessagesForContextAsync(Guid chatId, DateTime? after)
        => await db.Messages
            .Where(m => m.ChatId == chatId && (after == null || m.CreatedOn > after))
            .OrderBy(m => m.CreatedOn)
            .ToArrayAsync();

    public async Task UpdateCompactionAsync(Guid chatId, string summary, DateTime through) {
        Chats? chat = await db.Chats.FindAsync(chatId);
        if (chat == null) return;
        chat.ContextSummary = summary;
        chat.SummarizedThroughCreatedOn = through;
        await db.SaveChangesAsync();
    }

    public async Task<bool> UpdateTitleAsync(Guid id, string title)
    {
        Chats? chat = await db.Chats.FindAsync(id);
        if (chat == null) return false;
        chat.Title = title;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task UpdateLastContextTokensAsync(Guid chatId, int? tokens)
    {
        Chats? chat = await db.Chats.FindAsync(chatId);
        if (chat == null) return;
        chat.LastContextTokens = tokens;
        await db.SaveChangesAsync();
    }

    public async Task<Guid> CreateChatAsync(ChatsCreateDto dto)
    {
        // Started from a folder page or not, a project chat belongs in the root
        Guid? rootId = null;
        if (dto.ProjectId is Guid projectId)
            rootId = (await projectService.ResolveLocationAsync(projectId)
                ?? throw new ArgumentException("Project not found.", nameof(dto))).RootId;

        var id = Guid.NewGuid();
        await db.Chats.AddAsync(new Chats
        {
            Id = id,
            UserId = dto.UserId,
            Title = dto.Title,
            CreatedOn = DateTime.UtcNow,
            ProjectId = rootId
        });
        await db.SaveChangesAsync();
        return id;
    }

    public async Task<Guid> CreateMessageAsync(MessagesCreateDto dto)
    {
        Guid id = Guid.NewGuid();
        await db.Messages.AddAsync(new Messages
        {
            Id = id,
            ChatId = dto.ChatId,
            MessageRole = dto.MessageRole,
            Content = dto.Content,
            CreatedOn = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return id;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Chats? chat = await db.Chats.FindAsync(id);
        if (chat == null) return false;
        db.Chats.Remove(chat);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task LinkAttachmentsToMessageAsync(Guid chatId, Guid messageId, List<Guid> attachmentIds)
    {
        if (attachmentIds.Count == 0) return;

        List<MessageAttachments> attachments = await db.MessageAttachments.Where(a => a.ChatId == chatId && attachmentIds.Contains(a.Id) && a.MessageId == null).ToListAsync();

        foreach (var attachment in attachments)
            attachment.MessageId = messageId;

        await db.SaveChangesAsync();
    }

    public async Task<List<MessageAttachments>> GetAttachmentsForMessageAsync(Guid messageId)
        => await db.MessageAttachments.Where(a => a.MessageId == messageId).ToListAsync();

    public async Task<List<MessageAttachments>> GetAttachmentsForChatAsync(Guid chatId, bool activeOnly = false)
        => await db.MessageAttachments
            .Where(a => a.ChatId == chatId && (!activeOnly || !a.Detached))
            .OrderBy(a => a.CreatedOn)
            .ToListAsync();

    public async Task<MessageAttachments?> GetAttachmentByIdAsync(Guid id)
        => await db.MessageAttachments.FirstOrDefaultAsync(a => a.Id == id);
}
