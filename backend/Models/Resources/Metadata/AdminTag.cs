using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
namespace KnowledgeBank.Models;

[Table("admin-tags")]
[Index(nameof(Name), IsUnique = true)]
public class AdminTag
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    // Navigation properties
    [JsonIgnore] public ICollection<ResourceAdminTagRelation>? Resources { get; set; }
}

public class AdminTagCreateDto
{
    public required string Name { get; set; }
}