using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
namespace KnowledgeBank.Models;

[Table("tags")]
[Index(nameof(Name), IsUnique = true)]
public class Tag
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [JsonIgnore]
    public List<FileItem> Files { get; } = [];
}