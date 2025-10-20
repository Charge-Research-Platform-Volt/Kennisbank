using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

/// <summary>
/// Relation between a project and a tag, has navigation properties to the corresponding project and tag.
/// This is a many-to-many relationship.
///
/// Author: Justin Liem
/// </summary>
[Table("project-tag")]
public class ProjectTagRelation
{
    [Column("project-id")]
    [ForeignKey("Project")]
    public required Guid ProjectId { get; set; }

    [Column("tag-id")]
    [ForeignKey("Tag")]
    public required Guid TagId { get; set; }
    
    [Column("added-by")]
    public Guid? AddedBy { get; set; }

    // Navigation properties
    [JsonIgnore] public Project? Project { get; set; }
    [JsonIgnore] public Tag? Tag { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)