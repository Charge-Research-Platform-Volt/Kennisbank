using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace KnowledgeBank.Services.Domain;

public class UserService(DatabaseContext db)
{
    private readonly Serilog.ILogger logger = Log.ForContext<UserService>();

    public async Task<Dictionary<string, string>> GetRoleMapAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.ToList();
        var pairs = await db.UserRoles
            .Where(ur => ids.Contains(ur.UserId))
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();
        return pairs.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.First().Name!);
    }

    public async Task<(User[] Users, int TotalCount)> GetPageAsync(int pageIndex, int pageSize, string? searchQuery = null, string? excludeId = null)
    {
        IQueryable<User> query = db.Users;
        if (!string.IsNullOrEmpty(searchQuery))
            query = query.Where(u => EF.Functions.ILike(u.FirstName + " " + u.LastName, $"%{searchQuery}%"));
        if (!string.IsNullOrEmpty(excludeId))
            query = query.Where(u => u.Id != excludeId);

        int skip = (pageIndex - 1) * pageSize;
        User[] users = await query.OrderBy(u => u.Email).Skip(skip).Take(pageSize).ToArrayAsync();
        int totalCount = await db.Users.CountAsync();
        return (users, totalCount);
    }

    public async Task<(User[] MatchedUsers, Invitation[] MatchedInvites)> GetCombinedAsync(string? searchQuery = null)
    {
        IQueryable<User> userQuery = db.Users;
        if (!string.IsNullOrEmpty(searchQuery))
            userQuery = userQuery.Where(u =>
                EF.Functions.ILike(u.Email!, $"%{searchQuery}%") ||
                EF.Functions.ILike(u.FirstName + " " + u.LastName, $"%{searchQuery}%"));

        User[] matchedUsers = await userQuery.OrderBy(u => u.Email).ToArrayAsync();

        IQueryable<Invitation> inviteQuery = db.Invitations
            .Where(i => i.CreatedAt > DateTime.UtcNow.AddHours(-168));
        if (!string.IsNullOrEmpty(searchQuery))
            inviteQuery = inviteQuery.Where(i => EF.Functions.ILike(i.Email, $"%{searchQuery}%"));

        Invitation[] matchedInvites = await inviteQuery.OrderBy(i => i.Email).ToArrayAsync();

        return (matchedUsers, matchedInvites);
    }

    // Returns (InvitationId, RawToken) or null if a pending invitation for that email already exists
    public async Task<(Guid InvitationId, Guid RawToken)?> CreateInvitationAsync(string email, string role)
    {
        bool alreadyInvited = await db.Invitations.AnyAsync(i => i.Email == email
                                                               && i.CreatedAt > DateTime.UtcNow.AddHours(-168));
        if (alreadyInvited) return null;

        Guid rawToken = Guid.NewGuid();
        Guid invitationId = Guid.NewGuid();

        db.Invitations.Add(new Invitation
        {
            Id = invitationId,
            Email = email,
            Token = ShaUtils.Sha256(rawToken.ToString()),
            CreatedAt = DateTime.UtcNow,
            Role = role,
        });

        await db.SaveChangesAsync();
        return (invitationId, rawToken);
    }

    public async Task<Invitation[]> GetPendingInvitationsAsync()
        => await db.Invitations
            .Where(i => i.CreatedAt > DateTime.UtcNow.AddHours(-168))
            .OrderByDescending(i => i.CreatedAt)
            .ToArrayAsync();

    // Returns (Invitation, new RawToken) or null if not found / expired
    public async Task<(Invitation Invitation, Guid RawToken)?> RefreshInvitationAsync(Guid id)
    {
        Invitation? invitation = await db.Invitations
            .FirstOrDefaultAsync(i => i.Id == id && i.CreatedAt > DateTime.UtcNow.AddHours(-168));

        if (invitation == null) return null;

        Guid rawToken = Guid.NewGuid();
        invitation.Token = ShaUtils.Sha256(rawToken.ToString());
        invitation.CreatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return (invitation, rawToken);
    }

    public async Task<bool> CancelInvitationAsync(Guid id)
    {
        Invitation? invitation = await db.Invitations.FirstOrDefaultAsync(i => i.Id == id);
        if (invitation == null) return false;

        db.Invitations.Remove(invitation);
        await db.SaveChangesAsync();
        return true;
    }

    // Validates token hash, deletes invitation, returns it for role assignment — or null if invalid/expired
    public async Task<Invitation?> ValidateAndConsumeInvitationAsync(string rawToken)
    {
        string hash = ShaUtils.Sha256(rawToken);
        Invitation? invitation = await db.Invitations
            .FirstOrDefaultAsync(i => i.Token == hash && i.CreatedAt > DateTime.UtcNow.AddHours(-168));

        if (invitation == null)
        {
            logger.Warning("Invalid or expired invitation token attempted");
            return null;
        }

        db.Invitations.Remove(invitation);
        await db.SaveChangesAsync();
        return invitation;
    }

    public async Task<int> GetLatestChangelogIdAsync()
        => await db.Changelog.MaxAsync(c => (int?)c.Id) ?? 0;
}