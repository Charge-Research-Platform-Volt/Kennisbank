using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

[Table("organisation-relationships")]
public class OrganisationRelationship
{
    [Column("source-organisation-id")]
    public required Guid SourceOrganisationId { get; set; }

    [Column("target-organisation-id")]
    public required Guid TargetOrganisationId { get; set; }

    [Column("relation")]
    // like "parent company"
    // this means that source is a parent company of target
    // so basically you fill in this "[SOURCE] is [RELATION] of [TARGET]"
    public string? Relation { get; set; }

    // Navigation properties
    [ForeignKey("SourceOrganisationId")]
    [JsonIgnore]
    public Organisation? SourceOrganisation { get; set; }

    [ForeignKey("TargetOrganisationId")]
    [JsonIgnore]
    public Organisation? TargetOrganisation { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


