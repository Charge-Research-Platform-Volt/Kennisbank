using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace KnowledgeBank.Models;

[Table("websites")]
[Index(nameof(URL), IsUnique = true)]
public class Website
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("url")]
    public required string URL { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("description")]
    public required string Description { get; set; }

    [Column("created_at")]
    public required DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public required DateTime UpdatedAt { get; set; }

}

public class WebUploadDto //Data Transfer Object (DTO)
{
    public required string URL { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool Overwrite { get; set; } = false;
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class WebRenameDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}