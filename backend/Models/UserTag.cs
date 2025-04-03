using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
namespace KnowledgeBank.Models;

[Table("user-tags")]
[Index(nameof(Name), IsUnique = true)]
public class UserTag
{
    [Key]
    [Column("id")]
    public required Guid Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("is-approved")]
    public bool IsApproved { get; set; } = false;

    // TODO: Should be the actual User object or userId, not just a string.
    [Column("user")]
    public required string User { get; set; }

    [Column("created-on")]
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    [Column("approved-on")]
    public DateTime? ApprovedOn { get; set; }

    // Navigation properties
    [JsonIgnore] public ICollection<ResourceUserTagRelation>? Resources { get; set; }
}

public class UserTagCreateDto
{
    public required string Name { get; set; }
    public bool IsApproved { get; set; } = false;
    // TODO: Should be User ID, see TODO above.
    public required string User { get; set; }
    public DateTime? ApprovedOn { get; set; }
}