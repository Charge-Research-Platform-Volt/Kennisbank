using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("persons")]
[Index(nameof(Name), IsUnique = true)]
public class Person
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("occupation")]
    public required string Occupation { get; set; }
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("email-address")]
    public string? EmailAddress { get; set; }
    
    [Column("linkedin")]
    public string? Linkedin { get; set; }

    // Navigation property for the Author Resource Relation (1:m)
    [JsonIgnore] public ICollection<ResourceAuthorRelation>? AuthoredResources { get; set; }
    [JsonIgnore] public ICollection<PersonOrganisationRelation>? RelatedOrganisations { get; set; }

    [JsonIgnore][InverseProperty("SourcePerson")] public ICollection<PersonRelationship>? TargetRelationships { get; set; }
    [JsonIgnore][InverseProperty("TargetPerson")] public ICollection<PersonRelationship>? SourceRelationships { get; set; }

}