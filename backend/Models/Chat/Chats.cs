
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeBank.Models;

[Table("chats")]
public class Chats
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("user-id")]
    public required Guid UserId { get; set; }


    [Column("title")]
    public required string Title { get; set; }


    [Column("creation-date")]
    public required DateTime CreationDate { get; set; }

    // Navigation property
    public virtual ICollection<Messages> Messages { get; set; } = new List<Messages>();
}

public class ChatsCreateDto
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
}