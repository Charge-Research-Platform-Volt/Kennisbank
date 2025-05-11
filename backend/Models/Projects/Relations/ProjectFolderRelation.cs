using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

[Table("project-folder")]
public class ProjectFolderRelation
{
    [Column("root-id")]
    [ForeignKey("Project")]
    public required Guid RootId { get; set; }

    [Column("child-id")]
    [ForeignKey("Project")]
    public required Guid ChildId { get; set; }

    // Navigation properties
    [JsonIgnore] public Project? Root { get; set; }
    [JsonIgnore] public Project? Child { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)