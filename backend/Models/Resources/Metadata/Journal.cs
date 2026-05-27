using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Models;

[Table("journals")]
[Index(nameof(Name), IsUnique = true)]
public class Journal
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("created-by")]
    public required Guid CreatedBy { get; set; }

    [Column("created-on")]
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ICollection<Resource>? Resources { get; set; }
}

public class JournalCreateDto
{
    public required string Name { get; set; }
}