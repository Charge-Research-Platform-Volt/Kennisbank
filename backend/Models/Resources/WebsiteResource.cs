using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;


[Table("website-metadata")]
public class WebsiteResource
{
    [Column("object-id")]
    [Key]
    [ForeignKey("ObjectItem")]
    public required Guid ObjectId { get; set; }

    // Navigation property to parent (1:1)
    [JsonIgnore] public Resource? ObjectItem { get; set; }
}