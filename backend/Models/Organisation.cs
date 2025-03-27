using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace KnowledgeBank.Models;

[Table("organisations")]
[Index(nameof(Name), IsUnique = true)]
public class Organisation
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("name")]
    [Required]
    public required string Name { get; set; }
    
    [Column("description")]
    public string Decription { get; set; }
    
    [Column("url")]
    public string URL { get; set; }
    
    [Column("email_address")]
    public string Email_address { get; set; }
}