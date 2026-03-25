using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("resource-organisation")]
public class ResourceOrganisationRelation
{
    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("organisation-id")]
    [ForeignKey("Organisation")]
    public required Guid OrganisationId { get; set; }

    [Column("role")]
    // The role of the organisation on the resource (like publisher, referenced, etc.)
    public string? Role { get; set; }

    // Navigation properties
    [JsonIgnore] public Resource? Resource { get; set; }
    [JsonIgnore] public Organisation? Organisation { get; set; }
}
