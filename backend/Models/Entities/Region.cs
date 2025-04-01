using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("regions")]
[Index(nameof(Name), IsUnique = true)]
public class Region
{
    [Column("id")]
    [Key]
    public required Guid RegionId { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    // Navigation property
    [JsonIgnore] public ICollection<ResourceRegionRelation>? Resources { get; set; }
}