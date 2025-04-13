namespace KnowledgeBank.Responses;

public struct UserResponse
{
    public Guid Id { get; }
    public string? Username { get; }
    public string? Email { get; }
    public bool emailConfirmed { get; }
    public string Role { get; }

    public UserResponse(Guid id, string? username, string? email, bool emailConfirmed, string role)
    {
        this.Id = id;
        this.Username = username;
        this.Email = email;
        this.emailConfirmed = emailConfirmed;
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