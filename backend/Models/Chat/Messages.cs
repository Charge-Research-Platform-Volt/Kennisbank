using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;


[Table("messages")]
public class Messages
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }


    [Column("chat-id")]
    public required Guid ChatId { get; set; }

    [Column("message-role")]
    [MaxLength(50)]
    public required string MessageRole { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("creation-date")]
    public required DateTime CreationDate { get; set; }

    // Navigation properties
    [JsonIgnore] public virtual Chats Chat { get; set; } = null!;

}



public class MessagesCreateDto
{
    public Guid SenderId { get; set; }
    public Guid ChatId { get; set; }
    public string MessageRole { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

// Message role enum
public enum MessageRole
{
    User,
    Assistant
}