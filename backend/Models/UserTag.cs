using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
namespace KnowledgeBank.Models;

[Table("user_tags")]
[Index(nameof(Name), IsUnique = true)]
public class UserTag
{
    [Key]
    [Column("id")]
    public required Guid Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("is_approved")]
    public bool IsApproved { get; set; }

    [JsonIgnore]
    public List<FileItem> Files { get; } = [];
}