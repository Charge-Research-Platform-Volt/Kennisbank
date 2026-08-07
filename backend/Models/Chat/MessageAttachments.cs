using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("message-attachments")]
public class MessageAttachments
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("chat-id")]
    public required Guid ChatId { get; set; }

    [Column("message-id")]
    public Guid? MessageId { get; set; }

    [Column("file-name")]
    public required string FileName { get; set; }

    [Column("extension")]
    public required string Extension { get; set; }

    [Column("extracted-text")]
    public string? ExtractedText { get; set; }

    [Column("is-chunked")]
    public required bool IsChunked { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created-on")]
    public required DateTime CreatedOn { get; set; }

    [Column("detached")]
    public bool Detached { get; set; } = false;

    // Navigation properties
    [JsonIgnore] public virtual Chats Chat { get; set; } = null!;
    [JsonIgnore] public virtual Messages? Message { get; set; }
}

public class MessageAttachmentCreateDto
{
    public required string ObjectName { get; set; }
    public required string FileName { get; set; }
}