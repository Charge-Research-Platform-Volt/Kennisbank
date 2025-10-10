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
    public string? Description { get; set; }
    
    [Column("website")]
    public string? Website { get; set; }
    
    [Column("email-address")]
    public string? EmailAddress { get; set; }
    
    [Column("creation-date")]
    public required DateTime CreationDate { get; set; }

    [Column("trashed")]
    public bool Trashed { get; set; } = false;

    [Column("trash-date")]
    public DateTime? TrashDate { get; set; } = null;

    // Navigation properties
    [JsonIgnore] public ICollection<ResourceAuthorRelation>? ResourceAuthorRelations { get; set; }
    [JsonIgnore] public ICollection<ResourceOrganisationRelation>? ResourceOrganisationRelations { get; set; }
    [JsonIgnore] public ICollection<PersonOrganisationRelation>? PersonOrganisationRelations { get; set; }

    [JsonIgnore][InverseProperty("SourceOrganisation")] public ICollection<OrganisationRelationship>? TargetRelationships { get; set; }
    [JsonIgnore][InverseProperty("TargetOrganisation")] public ICollection<OrganisationRelationship>? SourceRelationships { get; set; }
}

public class OrganisationCreateDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? EmailAddress { get; set; }
    // Tuple: (OrganisationId, Relation?)
    public RelatedEntry[] OrganisationRelations { get; set; } = [];
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)

