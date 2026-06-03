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

    public async Task<bool> UpdateTitleAsync(Guid id, string title)
    {
        Chats? chat = await db.Chats.FindAsync(id);
        if (chat == null) return false;
        chat.Title = title;
        await db.SaveChangesAsync();
        return true;
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

    public async Task<Chats?> GetWithMessagesAsync(Guid id)
        => await db.Chats
            .Include(c => c.Messages.OrderBy(m => m.CreatedOn))
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task CreateMessageAsync(MessagesCreateDto dto)
    {
        await db.Messages.AddAsync(new Messages
        {
            Id = Guid.NewGuid(),
            ChatId = dto.ChatId,
            MessageRole = dto.MessageRole,
            Content = dto.Content,
            CreatedOn = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Chats? chat = await db.Chats.FindAsync(id);
        if (chat == null) return false;
        db.Chats.Remove(chat);
        await db.SaveChangesAsync();
        return true;
    }
}
