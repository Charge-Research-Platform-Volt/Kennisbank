using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("resource-related_person")]
public class ResourceRelatedPersonRelation
{
    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("person-id")]
    [ForeignKey("Person")]
    public required Guid PersonId { get; set; }

    [Column("role")]
    // This is the role the person has in the resource (like editor)
    public string? Role { get; set; }

    // Navigation properties
    [JsonIgnore] public Resource? Resource { get; set; }
    [JsonIgnore] public Person? Person { get; set; }
}
