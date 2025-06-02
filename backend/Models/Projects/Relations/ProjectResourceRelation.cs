using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace KnowledgeBank.Models;

/// <summary>
/// Relation containing the project an associated resource. Has navigation properties to the 
/// corresponding project and resource. This is a many-to-many relationship.
/// 
/// Author: Justin Liem
/// </summary>
[Table("project-resource")]
public class ProjectResourceRelation
{
    [Column("project-id")]
    [ForeignKey("Project")]
    public required Guid ProjectId { get; set; }

    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("added-by")]
    public string? AddedBy { get; set; }

    // Navigation properties
    [JsonIgnore] public Project? Project { get; set; }
    [JsonIgnore] public Resource? Resource { get; set; }}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)