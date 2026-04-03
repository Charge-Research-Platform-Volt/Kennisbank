using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("invitations")]
public class Invitation
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("email")]
    public required string Email { get; set; }

    [Column("token")]
    public required string Token { get; set; }

    [Column("created_at")]
    public required DateTime CreatedAt { get; set; }

    [Column("role")]
    public string Role { get; set; } = "user";
}
