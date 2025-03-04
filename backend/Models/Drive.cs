using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;


[Table("drive")]
public class Drive
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
}

public class DriveCreateDto //Data Transfer Object (DTO)
{
    public required string Name { get; set; }
    public required string Description { get; set; }
}