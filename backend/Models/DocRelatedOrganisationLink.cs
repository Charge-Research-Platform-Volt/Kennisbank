using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("doc-related_organisation")]
public class DocRelatedOrganisationLink
{
    [Column("doc-id")]
    public required Guid DocId { get; set; }

    [Column("organisation-id")]
    public required Guid OrganisationId { get; set; }
}