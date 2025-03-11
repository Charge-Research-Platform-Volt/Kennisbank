using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Models;

[Table("users")]
public class User
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("email")]
    [Required]
    public required string Email { get; set; }

    [Column("first_name")]
    public required string FirstName { get; set; }

    [Column("last_name")]
    public required string LastName { get; set; }

    [Column("password")]
    [Required]
    public required string HashedPassword { get; set; }
}