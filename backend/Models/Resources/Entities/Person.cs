using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("persons")]
public class Person : Entity
{
    [Column("occupation")]
    public required string Occupation { get; set; }

    [Column("linkedin")]
    public string? Linkedin { get; set; }

    // Person-specific navigation properties
    [JsonIgnore] public ICollection<PersonOrganisationRelation>? PersonOrganisationRelations { get; set; }
    [JsonIgnore] public ICollection<ResourceRelatedPersonRelation>? ResourceRelatedPersonRelations { get; set; }

    [JsonIgnore][InverseProperty("SourcePerson")] public ICollection<PersonRelationship>? TargetRelationships { get; set; }
    [JsonIgnore][InverseProperty("TargetPerson")] public ICollection<PersonRelationship>? SourceRelationships { get; set; }

}

public class PersonCreateDto
{
    public required string Name { get; set; }
    public required string Occupation { get; set; }
    public string? Description { get; set; }
    public string? EmailAddress { get; set; }
    public string? Linkedin { get; set; }
    // Tuple: (OrganisationId, Role?)
    public RelatedEntry[] OrganisationRelations { get; set; } = Array.Empty<RelatedEntry>();
    // Tuple: (PersonId, Relation?)
    public RelatedEntry[] PersonRelations { get; set; } = Array.Empty<RelatedEntry>();
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


