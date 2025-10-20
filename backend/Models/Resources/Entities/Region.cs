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

    // Navigation property
    [JsonIgnore] public ICollection<ResourceRegionRelation>? ResourceRegionRelations { get; set; }
}

public class RegionCreateDto
{
    public required string Name { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


