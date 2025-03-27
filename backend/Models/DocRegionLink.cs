using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("doc-region")]
public class DocRegionLink
{
    [Column("doc-id")]
    [ForeignKey("Document")]
    public required Guid DocId { get; set; }

    [Column("region-id")]
    [ForeignKey("Region")]
    public required Guid RegionId { get; set; }
}