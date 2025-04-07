using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
namespace KnowledgeBank.Models;

[Table("tags")]
[Index(nameof(Name), IsUnique = true)]
public class Tag
{
    [Key]
    [Column("id")]
    public required Guid Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("is-standardized")]
    public required bool IsStandardized = false;

    [Column("is-approved")]
    public bool IsApproved { get; set; } = false;

    [Column("approved-on")]
    public DateTime? ApprovedOn { get; set; }

    [Column("approvedBy")]
    public Guid? ApprovedBy { get; set; }

    // TODO: Should be the actual User object or userId, not just a string.
    [Column("created-by")]
    public required string CreatedBy { get; set; }

    [Column("created-on")]
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore] public ICollection<ResourceTagRelation>? Resources { get; set; }
}

public class TagCreateDto
{
    public required string Name { get; set; }
    public required bool IsStandardized { get; set; } = false;
    public bool IsApproved { get; set; } = false;
    public string? ApprovedBy { get; set; }
    // TODO: Should be User ID, see TODO above.
    public required string CreatedBy { get; set; }
}