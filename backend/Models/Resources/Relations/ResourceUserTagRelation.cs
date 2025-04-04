using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

[Table("resource-user_tag")]
public class ResourceUserTagRelation
{
    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("tag-id")]
    [ForeignKey("UserTag")]
    public required Guid TagId { get; set; }

    [Column("is-approved")]
    public bool IsApproved { get; set; } = false;

    [Column("approved-on")]
    public DateTime? ApprovedOn { get; set; }

    [Column("approved-by")]
    public Guid? ApprovedBy { get; set; }

    // Navigation properties
    [JsonIgnore] public Resource? Resource { get; set; }
    [JsonIgnore] public UserTag? UserTag { get; set; }
}