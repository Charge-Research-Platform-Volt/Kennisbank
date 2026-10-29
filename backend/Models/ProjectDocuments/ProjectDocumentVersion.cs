using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models.ProjectDocuments;

[Table("project-document-versions")]
public class ProjectDocumentVersion
{
    // Also the storage key: project-docs/{id}
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("document-id")]
    public required Guid DocumentId { get; set; }

    [Column("version-number")]
    public required int VersionNumber { get; set; }

    [Column("extension")]
    [MaxLength(10)]
    public required string Extension { get; set; }

    [Column("original-file-name")]
    public required string OriginalFileName { get; set; }

    // Filled in by the processing pipeline, which downloads the file anyway
    [Column("size-bytes")]
    public long? SizeBytes { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("uploaded-by")]
    public required string UploadedBy { get; set; }

    [Column("uploaded-on")]
    public required DateTime UploadedOn { get; set; }

    [Column("preview-status")]
    public EmbeddingStatus PreviewStatus { get; set; } = EmbeddingStatus.Pending;

    [Column("embedding-status")]
    public EmbeddingStatus EmbeddingStatus { get; set; } = EmbeddingStatus.Pending;

    [Column("embedding-error")]
    public string? EmbeddingError { get; set; }

    [Column("embedding-failures")]
    public int EmbeddingFailures { get; set; }
    
    [Column("embedding-last-attempt")]
    public DateTime? EmbeddingLastAttempt { get; set; }

    [Column("extracted-text")]
    public string? ExtractedText { get; set; }

    // External connection (Google Drive)
    [Column("external-id")]
    public string? ExternalId { get; set; }

    [Column("external-revision-id")]
    public string? ExternalRevisionId { get; set; }

    [JsonIgnore] public ProjectDocument? Document { get; set; }
}