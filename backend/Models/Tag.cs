using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("tags")]
public class Tag
{
    [Column("name")]
    [Key]
    public required string Name { get; set; }
}

public class TagCreateDto //Data Transfer Object (DTO)
{
    public required string Name { get; set; }
}

public class TagDeleteDto //Data Transfer Object (DTO)
{
    public required string Name { get; set; }
}