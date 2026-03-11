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

    [Column("email-address")]
    public string? EmailAddress { get; set; }

    [Column("creation-date")]
    public required DateTime CreationDate { get; set; }

    [Column("trashed")]
    public bool Trashed { get; set; } = false;

    [Column("trash-date")]
    public DateTime? TrashDate { get; set; } = null;

    [NotMapped]
    public string EntityType => GetType().Name.ToLower();

    // Navigation property for the Author Resource Relation (1:m)
    [JsonIgnore] public ICollection<ResourceAuthorRelation>? ResourceAuthorRelations { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
