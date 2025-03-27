using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("doc-related_source")]
public class DocRelateSourceLink
{
    [Column("doc-id")]
    public required Guid DocId { get; set; }

    [Column("source")]
    public required string Source { get; set; }
}