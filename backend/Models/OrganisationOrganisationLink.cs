using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("organisation-organistaion")]
public class OrganisationOrganisationLink
{
    [Column("organisation-id")]
    public required Guid OrganisationId { get; set; }

    [Column("organisation-id2")]
    public required Guid OrganisationId2 { get; set; }
}