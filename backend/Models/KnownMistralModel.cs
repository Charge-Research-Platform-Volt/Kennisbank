using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeBank.Models;

/// <summary>
/// A model id seen in Mistral's model list, so the dashboard can flag models released since the last check.
/// </summary>
[Table("known_mistral_models")]
public class KnownMistralModel
{
    [Column("id")]
    [Key]
    public required string Id { get; set; }

    [Column("first_seen_at")]
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;

    [Column("acknowledged")]
    public bool Acknowledged { get; set; }
}