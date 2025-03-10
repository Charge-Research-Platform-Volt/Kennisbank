using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace KnowledgeBank.Models.User;

[Table("users")]
[Index("Email", IsUnique = true)]
public class User
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("email")]
    [Required]
    public required string Email { get; set; }

    [Column("username")]
    public string? Name { get; set; }

    [Column("password")]
    [Required]
    public required string HashedPassword { get; set; }
}