using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace KnowledgeBank.Models;

[Table("regions")]
[Index(nameof(Name), IsUnique = true)]
public class Region
{
    [Column("id")]
    [Key]
    public required Guid RegionId { get; set; }

    [Column("name")]
    [Required]
    public required string Name { get; set; }

}