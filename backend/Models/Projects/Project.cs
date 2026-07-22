using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("projects")]
public class Project
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("title")]
    public required string Title { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created-on")]
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    [Column("created-by")]
    public required Guid CreatedBy { get; set; }

    [Column("project-type")]
    [MaxLength(10)]
    public required string ProjectType { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ICollection<ProjectTagRelation>? ProjectTagRelations { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ICollection<ProjectMemberRelation>? ProjectMemberRelations { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ICollection<ProjectItemRelation>? ProjectItemRelations { get; set; }

    [JsonIgnore][InverseProperty("ChildFolder")] public ICollection<ProjectFolderRelation>? ParentFolders { get; set; }
    [JsonIgnore][InverseProperty("ParentFolder")] public ICollection<ProjectFolderRelation>? ChildFolders { get; set; }
}

public class ProjectCreateDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string ProjectType { get; set; }
    public Guid[] Tags { get; set; } = [];
    public Guid[] Members { get; set; } = [];
}

public class ProjectListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Search { get; set; }
    public Guid? MemberFilter { get; set; }
    public Guid[]? Tags { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ProjectListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedOn { get; set; }
    public string ProjectType { get; set; } = string.Empty;
    public List<ProjectListTagDto> Tags { get; set; } = [];
    public List<ProjectListMemberDto> Members { get; set; } = [];
}

public class ProjectListTagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProjectListMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? CustomAvatarVersion { get; set; }
    public bool HasCustom { get; set; }
}

public class ProjectListResult
{
    public ProjectListItemDto[] Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int PageCount { get; set; }
}

public class ProjectInfoDto
{
    public Project? Project { get; set; }
    public Project? RootProject { get; set; }
    public List<ProjectFolderDto> Folders { get; set; } = [];
    public List<ProjectItem> Items { get; set; } = [];
    public List<ProjectMemberDto> Members { get; set; } = [];
    public List<Tag?> Tags { get; set; } = [];
    public List<ProjectAncestor> Ancestors { get; set; } = [];
}

public record ProjectAncestor(Guid Id, string Title);

public class ProjectItem : LibraryItem
{
    public string AddedBy { get; set; } = "Unknown";
}

public class ProjectMemberDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int? CustomAvatarVersion { get; set; }
    public bool EmailConfirmed { get; set; }
}

public class ProjectFolderDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AddedBy { get; set; } = "Unknown";
    public int Depth { get; set; }
}

public class UpdateProjectDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Guid[]? Tags { get; set; }
    public Guid[]? Members { get; set; }
}

public class AddFolderDto
{
    public required string Name { get; set; }
}