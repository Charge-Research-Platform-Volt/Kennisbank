using KnowledgeBank.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("resource-resourcetype")]
public class ResourceResourceTypeRelation
{
    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("resourcetype-id")]
    [ForeignKey("ResourceType")]
    public required Guid ResourceTypeId { get; set; }

    // Navigation properties
    [JsonIgnore] public Resource? Resource { get; set;}
    [JsonIgnore] public ResourceType? ResourceType { get; set; }
}
