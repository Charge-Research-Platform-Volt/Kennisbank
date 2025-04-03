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

    [Column("person-id")]
    [ForeignKey("Person")]
    public required Guid PersonId { get; set; }

    // Navigation property to Resource (1:1)
    [JsonIgnore] public Resource? Resource { get; set; }

    // Navigation property to Person (1:1)
    [JsonIgnore] public Person? Person { get; set; }
}