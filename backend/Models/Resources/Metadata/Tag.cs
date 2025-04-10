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
    public required bool IsStandardized { get; set; } = false;

    [Column("is-approved")]
    public bool IsApproved { get; set; } = false;

    [Column("approved-on")]
    public DateTime? ApprovedOn { get; set; }

    [Column("approved-by")]
    public Guid? ApprovedBy { get; set; }

    [Column("created-by")]
    public required Guid CreatedBy { get; set; }

    [Column("created-on")]
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore] public ICollection<ResourceTagRelation>? Resources { get; set; }
}

public class TagCreateDto
{
    public required string Name { get; set; }
    public bool IsApproved { get; set; } = false;
    public string? ApprovedBy { get; set; }
    public string CreatedBy { get; set; } = "";
}