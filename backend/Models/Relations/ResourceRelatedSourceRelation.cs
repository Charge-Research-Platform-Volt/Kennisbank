using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("resource-related_source")]
public class ResourceRelatedSourceRelation
{
    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("source")]
    public required string Source { get; set; }

    // Navigation properties
    [JsonIgnore] public Resource? Resource { get; set; }
}