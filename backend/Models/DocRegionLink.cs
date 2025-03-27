using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("doc-region")]
public class DocRegionLink
{
    [Column("doc-id")]
    public required Guid DocId { get; set; }

    [Column("region")]
    public required string Region { get; set; }
}