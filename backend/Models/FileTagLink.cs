using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("doc-tag")]
public class FileTagLink
{
    [Column("doc-id")]
    public required Guid DocId { get; set; }

    [Column("tag-id")]
    public required Guid TagId { get; set; }
}