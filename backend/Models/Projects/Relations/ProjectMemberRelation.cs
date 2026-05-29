using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("project-member")]
public class ProjectMemberRelation
{
    [Column("project-id")]
    [ForeignKey("Project")]
    public required Guid ProjectId { get; set; }

    [Column("member-id")]
    [ForeignKey("Member")]
    public required string UserId { get; set; }

    [JsonIgnore] public Project? Project { get; set; }
    [JsonIgnore] public User? Member { get; set; }
}