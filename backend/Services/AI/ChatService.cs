using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.AI;

public class ChatService(DatabaseContext db)
{
    public async Task<Dictionary<DateTime, Chats[]>> GetChatsGroupedByDateAsync(Guid userId, Guid? projectId)
        => (await db.Chats
            .Where(c => c.UserId == userId && c.ProjectId == projectId)
            .OrderByDescending(c => c.CreatedOn)
            .ToArrayAsync())
            .GroupBy(c => c.CreatedOn.Date)
            .OrderByDescending(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedOn).ToArray());

    public async Task<Chats?> GetByIdAsync(Guid id)
        => await db.Chats.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Messages[]> GetMessagesAsync(Guid chatId)
        => await db.Messages.Where(m => m.ChatId == chatId).OrderBy(m => m.CreatedOn).ToArrayAsync();

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
        var id = Guid.NewGuid();
        await db.Chats.AddAsync(new Chats
        {
            Id = id,
            UserId = dto.UserId,
            Title = dto.Title,
            CreatedOn = DateTime.UtcNow,
            ProjectId = dto.ProjectId
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

    public async Task<List<MessageAttachments>> GetAttachmentsForChatAsync(Guid chatId)
        => await db.MessageAttachments.Where(a => a.ChatId == chatId).OrderBy(a => a.CreatedOn).ToListAsync();

    public async Task<MessageAttachments?> GetAttachmentByIdAsync(Guid id)
        => await db.MessageAttachments.FirstOrDefaultAsync(a => a.Id == id);
}
