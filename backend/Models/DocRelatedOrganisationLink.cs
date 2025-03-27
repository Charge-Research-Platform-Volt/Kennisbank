using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("doc-related_organisation")]
public class DocRelatedOrganisationLink
{
    [Column("doc-id")]
    [ForeignKey("Document")]
    public required Guid DocId { get; set; }

    [Column("organisation-id")]
    [ForeignKey("Organisation")]
    public required Guid OrganisationId { get; set; }
}