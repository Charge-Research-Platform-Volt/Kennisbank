using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

public class UserUpdate {
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Name { get; set; }
}