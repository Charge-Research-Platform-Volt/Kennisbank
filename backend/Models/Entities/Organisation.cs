using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("organisations")]
[Index(nameof(Name), IsUnique = true)]
public class Organisation
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }
    
    [Column("description")]
    public string? Decription { get; set; }
    
    [Column("url")]
    public string? URL { get; set; }
    
    [Column("email-address")]
    public string? EmailAddress { get; set; }

    // Navigation properties
    [JsonIgnore] public ICollection<ResourceOrganisationRelation>? OwnedResources { get; set; }
    [JsonIgnore] public ICollection<ResourceRelatedOrganisationRelation>? RelatedResources { get; set; }
    [JsonIgnore] public ICollection<PersonOrganisationRelation>? RelatedPersons { get; set; }

    [JsonIgnore][InverseProperty("SourceOrganisation")] public ICollection<OrganisationRelationship>? TargetRelationships { get; set; }
    [JsonIgnore][InverseProperty("TargetOrganisation")] public ICollection<OrganisationRelationship>? SourceRelationships { get; set; }
}