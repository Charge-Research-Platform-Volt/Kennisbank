using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

/// <summary>
/// Relation linking a project/folder to any library item (resource, person, or organisation).
/// Uses the shared LibraryView for resolving item details. This is a many-to-many relationship.
/// </summary>
[Table("project-item")]
public class ProjectItemRelation
{
    [Column("project-id")]
    [ForeignKey("Project")]
    public required Guid ProjectId { get; set; }

    [Column("item-id")]
    public required Guid ItemId { get; set; }

    [Column("added-by")]
    public string? AddedBy { get; set; }

    // Navigation properties
    [JsonIgnore] public Project? Project { get; set; }
}
