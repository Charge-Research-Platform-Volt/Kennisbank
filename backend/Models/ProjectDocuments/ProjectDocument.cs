using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models.ProjectDocuments;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectDocumentVisibility
{
    Restricted,
    ProjectMembers,
    Everyone
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectDocumentAccessType
{
    Viewer,
    Editor
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectDocumentSource
{
    Upload,
    GoogleDrive
}

[Table("project-documents")]
public class ProjectDocument
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    // Always the root project, documents are not attached to folders
    [Column("project-id")]
    public required Guid ProjectId { get; set; }

    [Column("title")]
    [MaxLength(300)]
    public required string Title { get; set; }

    [Column("type-id")]
    public required Guid TypeId { get; set; }

    [Column("visibility")]
    [MaxLength(20)]
    public required ProjectDocumentVisibility Visibility { get; set; }

    // The level that visibility grants; people in Access can be granted more, never less
    [Column("general-access-type")]
    [MaxLength(10)]
    public required ProjectDocumentAccessType GeneralAccessType { get; set; }

    [Column("source")]
    [MaxLength(20)]
    public ProjectDocumentSource Source { get; set; } = ProjectDocumentSource.Upload;

    [Column("created-by")]
    public required string CreatedBy { get; set; }

    [Column("created-on")]
    public required DateTime CreatedOn { get; set; }

    [Column("updated-on")]
    public required DateTime UpdatedOn { get; set; }



    [JsonIgnore] public Project? Project { get; set; }
    [JsonIgnore] public ProjectDocumentType? Type { get; set; }
    [JsonIgnore] public ICollection<ProjectDocumentVersion>? Versions { get; set; }
    [JsonIgnore] public ICollection<ProjectDocumentAccess>? Access { get; set; }
}