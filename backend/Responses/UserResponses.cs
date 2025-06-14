using KnowledgeBank.Models;

namespace KnowledgeBank.Responses;

public struct UserResponse
{
    public Guid Id { get; }
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? Email { get; }
    public int? CustomAvatarVersion { get; }
    public bool EmailConfirmed { get; }
    public string Role { get; }

    public UserResponse(User user, string role)
    {
        this.Id = new Guid(user.Id);
        this.FirstName = user.FirstName;
        this.LastName = user.LastName;
        this.Email = user.Email!;
        this.CustomAvatarVersion = user.HasCustom ? user.CustomAvatarVersion : null;
        this.EmailConfirmed = user.EmailConfirmed;
        this.Role = role;
    }
}


public struct UserPageResponse
{
    public string Message { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public int PageCount { get; }
    public UserResponse[] Users { get; }

    public UserPageResponse(string message, int pageIndex, int pageSize, int pageCount, UserResponse[] users)
    {
        this.Message = message;
        this.PageIndex = pageIndex;
        this.PageSize = pageSize;
        this.Users = users;
        this.PageCount = pageCount;
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


