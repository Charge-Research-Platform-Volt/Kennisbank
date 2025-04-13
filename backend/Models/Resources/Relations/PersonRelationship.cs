using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

[Table("person-person")]
public class PersonRelationship
{
    [Column("source-person-id")]
    public required Guid SourcePersonId { get; set; }

    [Column("target-person-id")]
    public required Guid TargetPersonId { get; set; }
    
    [Column("relation")]
    // like "the boss"
    // this means that source is the boss of target
    // so basically you fill in this "[SOURCE] is [RELATION] of [TARGET]"
    public string? Relation { get; set; }

    // Navigation properties
    [ForeignKey("SourcePersonId")]
    [JsonIgnore]
    public Person? SourcePerson { get; set; }

    [ForeignKey("TargetPersonId")]
    [JsonIgnore]
    public Person? TargetPerson { get; set; }
}