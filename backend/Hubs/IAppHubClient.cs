using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Hubs;

public interface IAppHubClient
{
    Task ChatTitleUpdated(string chatId);
    Task ChatThinking();
    Task ChatReasoningChunk(string text);
    Task ChatToolStatus(string tool, string label);
    Task MistralStatusChanged(MistralStatusPayload status);
    Task EmbeddingStatusChanged(Guid id, string status);
}