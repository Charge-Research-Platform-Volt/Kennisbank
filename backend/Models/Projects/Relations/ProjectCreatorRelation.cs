using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

/// <summary>
/// Relation describing that of a project and a creator. Has navigation properties of
/// the corresponding project and creator. This is a many-to-many relationship.
/// 
/// Author: Justin Liem
/// </summary>
[Table("project-creator")]
public class ProjectCreatorRelation
{
    [Column("project-id")]
    [ForeignKey("Project")]
    public required Guid ProjectId { get; set; }

    [Column("creator-id")]
    [ForeignKey("Creator")]
    public required string CreatorId { get; set; }

    // Navigation properties
    [JsonIgnore] public Project? Project { get; set; }
    [JsonIgnore] public User? Creator { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)