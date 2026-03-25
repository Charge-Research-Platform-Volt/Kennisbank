using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeBank.Models;

/// <summary>
/// Represents a chat entity that stores chat conversation information in the database.
/// </summary>
/// <remarks>
/// This class maps to the "chats" table and contains basic chat metadata including
/// the chat identifier, associated user, title, and creation timestamp. Each chat
/// can contain multiple messages through the navigation property.
/// </remarks>
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

/// <summary>
/// Data transfer object for creating a new chat.
/// </summary>
/// <remarks>
/// This DTO is used to transfer the required information for creating a new chat instance,
/// including the user identifier and chat title.
/// </remarks>
public class ChatsCreateDto
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
}
