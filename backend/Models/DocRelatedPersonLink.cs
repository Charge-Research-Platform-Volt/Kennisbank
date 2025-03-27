using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("doc-related_person")]
public class DocRelatePersonLink
{
    [Column("doc-id")]
    public required Guid DocId { get; set; }

    [Column("person-id")]
    public required Guid PersonId { get; set; }
}