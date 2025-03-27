using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("doc-author")]
public class DocAuthorLink
{
    [Column("doc-id")]
    public required Guid DocId { get; set; }

    [Column("person-id")]
    public required Guid PersonId { get; set; }
}