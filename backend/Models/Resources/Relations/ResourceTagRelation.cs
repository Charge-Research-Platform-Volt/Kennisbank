using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

[Table("resource-tag")]
public class ResourceTagRelation
{
    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("tag-id")]
    [ForeignKey("Tag")]
    public required Guid TagId { get; set; }
    
    [Column("added-by")]
    public Guid? AddedBy { get; set; }

    // Navigation properties
    [JsonIgnore] public Resource? Resource { get; set; }
    [JsonIgnore] public Tag? Tag { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


