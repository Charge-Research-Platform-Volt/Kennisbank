using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Models;

[Table("regions")]
[Index(nameof(Name), IsUnique = true)]
public class Region
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

    // Navigation property
    [JsonIgnore] public ICollection<ResourceRegionRelation>? ResourceRegionRelations { get; set; }
}

public class RegionCreateDto
{
    public required string Name { get; set; }
}
