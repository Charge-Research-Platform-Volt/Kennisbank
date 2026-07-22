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
    public List<string> Aliases { get; set; } = [];
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? EmailAddress { get; set; }
    // Tuple: (OrganisationId, Relation?)
    public RelatedEntry[] OrganisationRelations { get; set; } = [];
}

public class OrganisationUpdateDto
{
    public string? Name { get; set; }
    public List<string>? Aliases { get; set; }
    public string? Description { get; set; }
    public string? EmailAddress { get; set; }
    public string? Website { get; set; }
}

public class OrganisationDetailDto
{
    public string Name { get; set; } = null!;
    public List<string> Aliases { get; set; } = [];
    public string? Description { get; set; }
    public string? EmailAddress { get; set; }
    public string? Website { get; set; }
    public DateTime CreatedOn { get; set; }
    public RelationItemDto[] Authored { get; set; } = [];
    public RelationItemDto[] RelatedResources { get; set; } = [];
    public RelationItemDto[] TargetOrganisations { get; set; } = [];
    public RelationItemDto[] SourceOrganisations { get; set; } = [];
    public RelationItemDto[] Persons { get; set; } = [];
}
