using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

[Table("project-resource")]
public class ProjectResourceRelation
{
    [Column("project-id")]
    [ForeignKey("Project")]
    public required Guid ProjectId { get; set; }

    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    // Navigation properties
    [JsonIgnore] public Project? Project { get; set; }
    [JsonIgnore] public Resource? Resource { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)