using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("doc-source")]
public class DocSourceLink
{
    [Column("doc-id")]
    public required Guid DocId { get; set; }

    [Column("source")]
    public required string Source { get; set; }
}