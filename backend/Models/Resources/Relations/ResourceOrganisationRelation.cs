using System.ComponentModel.DataAnnotations;
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
    // the direct role of the organisation on the resource (like publisher)
    public string? Role { get; set; }

    // Navigation property to Resource (1:1)
    [JsonIgnore] public Resource? Resource { get; set; }

    // Navigation property to Organisation (1:1)
    [JsonIgnore] public Organisation? Organisation { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


