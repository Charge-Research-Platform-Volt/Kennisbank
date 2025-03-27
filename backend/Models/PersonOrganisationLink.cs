using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("person-organisation")]
public class PersonOrganisationLink
{
    [Column("person-id")]
    public required Guid PersonId { get; set; }

    [Column("organisation-id")]
    public required Guid OrganisationId { get; set; }
}