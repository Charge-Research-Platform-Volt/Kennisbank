using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

[Table("resource-tag")]
public class ResourceTagRelation
{
    [Column("resource-id")]
    public required Guid ResourceId { get; set; }

    [Column("tag-id")]
    public required Guid TagId { get; set; }

    // Navigation properties
    [JsonIgnore] public Resource? Resource { get; set; }
    [JsonIgnore] public Tag? Tag { get; set; }
}