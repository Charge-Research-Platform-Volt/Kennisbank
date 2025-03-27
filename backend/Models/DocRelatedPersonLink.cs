using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("doc-related_person")]
public class DocRelatedPersonLink
{
    [Column("doc-id")]
    [ForeignKey("Document")]
    public required Guid DocId { get; set; }

    [Column("person-id")]
    [ForeignKey("Person")]
    public required Guid PersonId { get; set; }
}