using System.ComponentModel.DataAnnotations;
namespace KnowledgeBank.Models;

public class Update {
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Name { get; set; }
}