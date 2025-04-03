using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

[Table("resource-user_tag")]
public class ResourceUserTagRelation
{
    [Column("resource-id")]
    public required Guid ResourceId { get; set; }

    [Column("tag-id")]
    public required Guid TagId { get; set; }

    // Navigation properties
    [JsonIgnore] public Resource? Resource { get; set; }
    [JsonIgnore] public UserTag? UserTag { get; set; }
}