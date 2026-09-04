using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeBank.Models;

[Table("dismissed-merge-suggestions")]
public class DismissedMergeSuggestion
{
    [Column("entity-type")]
    public required string EntityType { get; set; }

    [Column("id1")]
    public required Guid Id1 { get; set; }

    [Column("id2")]
    public required Guid Id2 { get; set; }

    [Column("dismissed-on")]
    public DateTime DismissedOn { get; set; } = DateTime.UtcNow;
}