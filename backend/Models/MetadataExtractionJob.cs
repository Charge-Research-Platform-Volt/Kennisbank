namespace KnowledgeBank.Models;

public enum JobStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class MetadataExtractionJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? Type { get; set; } // "file" or "web"
    public string? Value { get; set; } // file GUID or URL
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public object? Result { get; set; } // ExtractedMetadata when completed
    public string? ErrorMessage { get; set; }
    public int ProgressPercentage { get; set; } = 0;
    public string? StatusMessage { get; set; } = "Queued";
}
