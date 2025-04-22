using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("resource-related_organisation")]
public class ResourceRelatedOrganisationRelation
{
    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("organisation-id")]
    [ForeignKey("Organisation")]
    public required Guid OrganisationId { get; set; }

    [Column("role")]
    // This is the indirect role of the organisation on the resource (like referenced)
    public string? Role { get; set; }

    // Navigation properties
    [JsonIgnore] public Resource? Resource { get; set; }
    [JsonIgnore] public Organisation? RelatedOrganisation { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


