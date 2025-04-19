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
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


