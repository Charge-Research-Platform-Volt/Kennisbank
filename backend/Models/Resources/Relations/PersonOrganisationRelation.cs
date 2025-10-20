using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

[Table("person-organisation")]
public class PersonOrganisationRelation
{
    [Column("person-id")]
    [ForeignKey("Person")]
    public required Guid PersonId { get; set; }

    [Column("organisation-id")]
    [ForeignKey("Organisation")]
    public required Guid OrganisationId { get; set; }

    [Column("role")]
    // This is the role the person has in the organisation
    public string? Role { get; set; }

    // Navigation property
    [JsonIgnore] public Person? Person { get; set; }
    [JsonIgnore] public Organisation? Organisation { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


