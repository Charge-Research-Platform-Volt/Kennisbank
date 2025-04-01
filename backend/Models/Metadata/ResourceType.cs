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

    [Column("type")]
    public required string Type { get; set; }

    // Navigation properties
    [JsonIgnore] public Resource? Resource { get; set; }
}