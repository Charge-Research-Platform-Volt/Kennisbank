using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("resource-author")]
public class ResourceAuthorRelation
{

    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("author-id")]
    public required Guid AuthorId { get; set; }

    // Navigation property to Resource (1:1)
    [JsonIgnore] public Resource? Resource { get; set; }

    // Navigation property to Person (1:1)
    [JsonIgnore] public ResourceGridItem? Author { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


