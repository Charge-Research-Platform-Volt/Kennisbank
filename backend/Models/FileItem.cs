using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;


[Table("files")]
public class FileItem
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("name")]
    [Required]
    public required string Name { get; set; }

    [Column("description")]
    [Required]
    [MaxLength(255)]
    public required string Description { get; set; }

    [Column("filetype")]
    [Required]
    [MaxLength(50)]
    public required string FileType { get; set; }

    // Navigation property (one to one)
    public FileVector? Vector { get; set; }
}

public class StorageUploadDto //Data Transfer Object (DTO)
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required IFormFile File { get; set; }
    public bool Overwrite { get; set; } = false;
}

public class StorageRenameDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}