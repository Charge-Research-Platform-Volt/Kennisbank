using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("document-metadata")]
public class DocumentMetadata
{
    [Column("resource-id")]
    [Key]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("abstract")]
    public string? Abstract { get; set; }

    // Navigation property to parent (1:1)
    [JsonIgnore] public Resource? Resource { get; set; }
}

public class DocumentCreateDto : FileResourceCreateDto
{
    public string? Abstract { get; set; }
}
