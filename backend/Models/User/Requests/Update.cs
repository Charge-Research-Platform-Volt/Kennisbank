using System.ComponentModel.DataAnnotations;
namespace KnowledgeBank.Models;

public class UserUpdate {
    [Required]
    public required string Password { get; set; }
    public string? Email { get; set; }
    public string? NewPassword { get; set; }
    public string? Name { get; set; }
}