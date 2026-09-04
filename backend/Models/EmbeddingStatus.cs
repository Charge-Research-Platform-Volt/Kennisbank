using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmbeddingStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}