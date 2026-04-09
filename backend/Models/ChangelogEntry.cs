using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeBank.Models;

[Table("changelog")]
public class ChangelogEntry
{
    [Column("id")]
    [Key]
    public int Id { get; set; }

    [Column("title")]
    public required string Title { get; set; }

    [Column("body")]
    public required string Body { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChangelogEntryCreateDto
{
    public required string Title { get; set; }
    public required string Body { get; set; }
}