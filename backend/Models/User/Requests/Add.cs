using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models.User.Requests;

public class Add {
    [Required]
    public required string Email { get; set; }
    [Required]
    public required string Password { get; set; }
    public string? Name { get; set; }
}