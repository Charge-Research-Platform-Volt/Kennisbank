using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;


[Table("file_vectors")]
public class FileVector
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("file_id")]
    [Required]
    [ForeignKey("FileItem")]
    public required Guid FileId { get; set; }

    // Store the vector as a tsvector type in PostgreSQL
    [Column("vector", TypeName = "text")]
    public required string Vector { get; set; }

    // Navigation property (one to one)
    public FileItem File { get; set; } = null!;
}
