using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("entities")]
[Index(nameof(Name), IsUnique = true)]
public abstract class Entity
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("aliases")]
    public List<string> Aliases { get; set; } = [];

    [Column("email-address")]
    public string? EmailAddress { get; set; }

    [Column("created-on")]
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    [Column("created-by")]
    public required Guid CreatedBy { get; set; }

    [Column("trashed")]
    public bool Trashed { get; set; } = false;

    [Column("trash-date")]
    public DateTime? TrashDate { get; set; } = null;

    [Column("embedding-status")]
    public EmbeddingStatus EmbeddingStatus { get; set; } = EmbeddingStatus.Pending;

    [Column("embedding-error")]
    public string? EmbeddingError { get; set; }

    [NotMapped]
    public string EntityType => GetType().Name.ToLower();

    // Navigation property for the Author Resource Relation (1:m)
    [JsonIgnore] public ICollection<ResourceAuthorRelation>? ResourceAuthorRelations { get; set; }
}

public class RelationItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Role { get; set; }
    public string? Relation { get; set; }
    public string? FileType { get; set; }
}