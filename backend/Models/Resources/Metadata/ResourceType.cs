using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("resource-types")]
public class ResourceType
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    // Navigation properties
    [JsonIgnore] public ICollection<Resource>? Resources { get; set; }
}

public class ResourceTypeCreateDto
{
    public required string Name { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


