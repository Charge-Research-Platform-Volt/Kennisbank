using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace KnowledgeBank.Models;

[Table("persons")]
[Index(nameof(Name), IsUnique = true)]
public class Person
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("name")]
    [Required]
    public required string Name { get; set; }

    [Column("occupation")]
    [Required]
    public required string Occupation { get; set; }
    
    [Column("description")]
    public string Decription { get; set; }
    
    [Column("email_address")]
    public string Email_address { get; set; }
    
    [Column("linkedin")]
    public string Linkedin { get; set; }
}