using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("persons")]
public class Person : Entity
{
    [Column("occupation")]
    public string? Occupation { get; set; }

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
    public List<string> Aliases { get; set; } = [];
    public string? Occupation { get; set; }
    public string? Description { get; set; }
    public string? EmailAddress { get; set; }
    public string? Linkedin { get; set; }
    // Tuple: (OrganisationId, Role?)
    public RelatedEntry[] OrganisationRelations { get; set; } = Array.Empty<RelatedEntry>();
    // Tuple: (PersonId, Relation?)
    public RelatedEntry[] PersonRelations { get; set; } = Array.Empty<RelatedEntry>();
}

public class PersonUpdateDto
{
    public string? Name { get; set; }
    public List<string>? Aliases { get; set; }
    public string? Description { get; set; }
    public string? EmailAddress { get; set; }
    public string? Occupation { get; set; }
    public string? Linkedin { get; set; }
}

public class PersonDetailDto
{
    public string Name { get; set; } = null!;
    public List<string> Aliases { get; set; } = [];
    public string? Description { get; set; }
    public string? Occupation { get; set; }
    public string? EmailAddress { get; set; }
    public string? Linkedin { get; set; }
    public DateTime CreatedOn { get; set; }
    public RelationItemDto[] Authored { get; set; } = [];
    public RelationItemDto[] RelatedResources { get; set; } = [];
    public RelationItemDto[] TargetPersons { get; set; } = [];
    public RelationItemDto[] SourcePersons { get; set; } = [];
    public RelationItemDto[] RelatedOrganisations { get; set; } = [];
}