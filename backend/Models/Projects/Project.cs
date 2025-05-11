using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("projects")]
public class Project
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("title")]
    public required string Title { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("language-code")]
    [MaxLength(2)]
    public required string LanguageCode { get; set; }

    [Column("creation-date")]
    public required DateTime CreationDate { get; set; }

    [Column("deletion-date")]
    public required DateTime DeletionDate { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("filetype")]
    [MaxLength(10)]
    public required string ProjectType { get; set; }

    #region Relation navigation properties
    // Navigation properties for the relations a object can have (1:m)
    [JsonIgnore] public ICollection<ProjectTagRelation>? ProjectTagRelations { get; set; }

    [JsonIgnore] public ICollection<ProjectCreatorRelation>? ProjectCreatorRelations { get; set; }

    [JsonIgnore] public ICollection<ProjectFolderRelation>? ProjectFolderRelations { get; set; }

    [JsonIgnore] public ICollection<ProjectResourceRelation>? ProjectResourcesRelations { get; set; }

    #endregion
}

// Does not include folders or resources as those will be added only AFTER creation
public class ProjectCreateDto // Data Transfer Object (DTO)
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string LanguageCode { get; set; }
    public required DateTime CreationDate { get; set; }
    public required DateTime DeletionDate { get; set; }
    public string? Note { get; set; }
    public required string ProjectType {get; set;}
    public string[] Tags { get; set; } = [];
    public string[] Creators { get; set; } = [];
}

public class ProjectRenameDto
{
    public required string Id { get; set; }
    public required string Title { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)