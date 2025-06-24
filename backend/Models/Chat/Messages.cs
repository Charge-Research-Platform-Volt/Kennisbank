using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;



/// <summary>
/// Represents a message entity in the chat system, stored in the "messages" table.
/// Contains message content, role information, and metadata for chat conversations.
/// </summary>
/// <remarks>
/// This entity is mapped to the "messages" database table and represents individual messages
/// within chat conversations. Each message is associated with a specific chat through the ChatId foreign key.
/// </remarks>
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



/// <summary>
/// Data Transfer Object for creating new chat messages.
/// Contains the essential information required to create a message within a chat conversation.
/// </summary>
/// <remarks>
/// This DTO is used when creating new messages in the chat system, containing references
/// to the sender, the chat conversation, the role of the message sender, and the actual message content.
/// </remarks>
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


