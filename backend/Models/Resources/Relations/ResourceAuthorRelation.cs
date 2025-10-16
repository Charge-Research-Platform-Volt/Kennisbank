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
    [ForeignKey("Author")]
    public required Guid AuthorId { get; set; }

    // Navigation property to Resource (1:1)
    [JsonIgnore] public Resource? Resource { get; set; }

    // Navigation property to Author (Person or Organisation via Entity base class)
    [JsonIgnore] public Entity? Author { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


