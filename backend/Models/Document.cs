using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;


[Table("documents")]
public class Document
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("title")]
    [Required]
    public required string Title { get; set; }

    [Column("type")]
    [Required]
    public required string Type { get; set; }

    [Column("description")]
    public string Description { get; set; }

    [Column("language")]
    [Required]
    public required string Language { get; set; }

    [Column("lincences")]
    public string Licences { get; set; }
    
    [Column("publication_id")]
    public string Publication_id { get; set; }
    
    [Column("note")]
    public string Note { get; set; }

    [Column("created_at")]
    [Required]
    public required DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [Required]
    public required DateTime UpdatedAt { get; set; }

    [Column("filetype")]
    [Required]
    [MaxLength(50)]
    public required string FileType { get; set; }

    [Column("hash")]
    [MaxLength(64)]
    public string? Hash { get; set; }


    // Navigation property (one to one). JsonIgnore excludes it from response bodies.
    [JsonIgnore]
    public FileVector? Vector { get; set; }
}

public class StorageDocumentUploadDto //Data Transfer Object (DTO)
{
    public required string Title { get; set; }
    public required string Type { get; set; }
    public string Description { get; set; }
    public required string Language { get; set; }
    public string Licences { get; set; }
    public string Publication_id { get; set; }
    public string Note { get; set; }
    public required IFormFile File { get; set; }
    public string? Hash { get; set; } = null;
    public bool Overwrite { get; set; } = false;
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class StorageRenameDocumentDto
{
    public required string Id { get; set; }
    public required string Tile { get; set; }
}