using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models.ProjectDocuments;

// Per-person grants, on top of the document's general access
[Table("project-document-access")]
public class ProjectDocumentAccess
{
    [Column("document-id")]
    public required Guid DocumentId { get; set; }

    [Column("user-id")]
    public required string UserId { get; set; }

    [Column("access-type")]
    [MaxLength(10)]
    public required ProjectDocumentAccessType AccessType { get; set; }

    [JsonIgnore] public ProjectDocument? Document { get; set; }
    [JsonIgnore] public User? User { get; set; }
}