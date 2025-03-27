using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("person-organisation")]
public class PersonOrganisationLink
{
    [Column("person-id")]
    [ForeignKey("Person")]
    public required Guid PersonId { get; set; }

    [Column("organisation-id")]
    [ForeignKey("Organisation")]
    public required Guid OrganisationId { get; set; }
}