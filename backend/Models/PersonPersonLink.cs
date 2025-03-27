using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("person-person")]
public class PersonPersonLink
{
    [Column("person-id")]
    public required Guid PersonId { get; set; }

    [Column("person-id2")]
    public required Guid PersonId2 { get; set; }
}