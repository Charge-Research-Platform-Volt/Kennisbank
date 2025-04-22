using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;


[Table("resource-vectors")]
public class ResourceVector
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    // Store the vector as a tsvector type in PostgreSQL
    [Column("vector", TypeName = "text")]
    public required string Vector { get; set; }

    // Navigation property (one to one). JsonIgnore excludes it from response bodies.
    [JsonIgnore] public Resource? Resource { get; set; }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


