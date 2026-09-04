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

public class FailedEmbeddingItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Error { get; set; }
}

public class EmbeddingStatusSummaryDto
{
    public bool IsRunning { get; set; }
    public Dictionary<string, int> ResourceCounts { get; set; } = [];
    public Dictionary<string, int> EntityCounts { get; set; } = [];
    public int ResourcesWithoutMetadataEmbeddings { get; set; }
    public int ResourcesWithoutContentEmbeddings { get; set; }
    public int EntitiesWithoutEmbeddings { get; set; }
    public List<FailedEmbeddingItemDto> IncompleteItems { get; set; } = [];
}