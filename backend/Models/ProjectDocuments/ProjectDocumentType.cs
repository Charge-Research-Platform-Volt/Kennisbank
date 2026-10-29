using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Models.ProjectDocuments;

[Table("project-document-types")]
[Index(nameof(Name), IsUnique = true)]
public class ProjectDocumentType
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public required string Name { get; set; }

    // Null for the built-in types seeded by the migration
    [Column("created-by")]
    public Guid? CreatedBy { get; set; }
    
    [Column("created-on")]
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public ICollection<ProjectDocument>? Documents { get; set; }
}