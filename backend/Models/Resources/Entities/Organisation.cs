using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("organisations")]
public class Organisation : Entity
{
    [Column("website")]
    public string? Website { get; set; }

    // Organisation-specific navigation properties
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

