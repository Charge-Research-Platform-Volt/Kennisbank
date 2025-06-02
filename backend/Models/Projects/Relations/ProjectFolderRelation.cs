using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

/// <summary>
/// Relation describing that of 2 projects, with the parent being either a root project or a folder,
/// while the child can only be a folder. Has navigation properties to the parent/child "folder". This
/// is a many-to-many relationship.
/// 
/// Author: Justin Liem
/// </summary>
[Table("project-folder")]
public class ProjectFolderRelation
{
    [Column("parent-id")]
    public required Guid ParentId { get; set; }

    [Column("child-id")]
    public required Guid ChildId { get; set; }

    [Column("added-by")]
    public string? AddedBy { get; set; }

    // Navigation properties
    [ForeignKey("ParentId")]
    [JsonIgnore]
    public Project? ParentFolder { get; set; }

    [ForeignKey("ChildId")]
    [JsonIgnore]
    public Project? ChildFolder { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)