using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    // TODO: Should be the actual User object or userId, not just a string.
    [Column("user")]
    public required string User { get; set; }

    [Column("created_on")]
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    [Column("approved_on")]
    public DateTime? ApprovedOn { get; set; }
}