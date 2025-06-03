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
    
    [Column("creation-date")]
    public required DateTime CreationDate { get; set; }

    [Column("trashed")]
    public bool Trashed { get; set; } = false;

    [Column("trash-date")]
    public DateTime? TrashDate { get; set; } = null;

    // Navigation property for the Author Resource Relation (1:m)
    [JsonIgnore] public ICollection<ResourceAuthorRelation>? ResourceAuthorRelations { get; set; }
    [JsonIgnore] public ICollection<PersonOrganisationRelation>? PersonOrganisationRelations { get; set; }

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


