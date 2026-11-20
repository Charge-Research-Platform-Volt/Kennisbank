namespace KnowledgeBank.Models.Chat;

/// <summary>
/// Who is asking, in which chat and project. Always decided by the server from the signed-in user and the stored chat,
/// never by the model or the client: the project document tools check access with it
/// </summary>
public record ChatToolContext(Guid ChatId, string UserId, bool IsOwner, Guid? ProjectId, string ConnectionId);