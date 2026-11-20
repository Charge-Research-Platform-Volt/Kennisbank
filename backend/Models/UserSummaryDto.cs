using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Models;

/// <summary>
/// Who did something, with what the frontend's avatar needs. Id is null when the user can't be found anymore.
/// </summary>
public record UserSummaryDto(Guid? Id, string Name, int? CustomAvatarVersion)
{
    public static readonly UserSummaryDto Unknown = new(null, "Unknown", null);
}

public static class UserSummaryQueries
{
    /// <summary>Looks up several users at once, keyed by their string id as stored in the "added-by" style columns.</summary>
    public static async Task<Dictionary<string, UserSummaryDto>> GetUserSummariesAsync(this DatabaseContext db, IEnumerable<string?> userIds)
    {
        string[] ids = [.. userIds.OfType<string>().Distinct()];
        if (ids.Length == 0) return [];

        var users = await db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.HasCustom, u.CustomAvatarVersion })
            .ToListAsync();

        return users.ToDictionary(
            u => u.Id,
            u => new UserSummaryDto(Guid.Parse(u.Id), $"{u.FirstName} {u.LastName}", u.HasCustom ? u.CustomAvatarVersion : null));
    }

    public static UserSummaryDto Resolve(this Dictionary<string, UserSummaryDto> users, string? userId)
        => userId != null && users.TryGetValue(userId, out UserSummaryDto? user) ? user : UserSummaryDto.Unknown;
}
