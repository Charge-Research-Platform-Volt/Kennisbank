using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

/// <summary>
/// A project is a structure that is defined by its ID, and has an unique title. A description is optional, and a creation
/// date / deletion date is always given at its creation (deletion date due to it not being implemented at the moment). Project type
/// is always either root project or a folder, which is a project, without the optional description and no tags.
/// </summary>
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

    [Column("creation-date")]
    public required DateTime CreationDate { get; set; } = DateTime.UtcNow;

    [Column("deletion-date")]
    public required DateTime DeletionDate { get; set; } = DateTime.UtcNow;

    [Column("project-type")]
    [MaxLength(10)]
    public required string ProjectType { get; set; } // Either "root" or "folder"

    #region Relation navigation properties
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ICollection<ProjectTagRelation>? ProjectTagRelations { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ICollection<ProjectCreatorRelation>? ProjectCreatorRelations { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ICollection<ProjectItemRelation>? ProjectItemRelations { get; set; }
    #endregion

    [JsonIgnore][InverseProperty("ChildFolder")] public ICollection<ProjectFolderRelation>? ParentFolders { get; set; }
    [JsonIgnore][InverseProperty("ParentFolder")] public ICollection<ProjectFolderRelation>? ChildFolders { get; set; }
}

public class ProjectCreateDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string ProjectType { get; set; }
    public string[] Tags { get; set; } = [];
    public string[] Creators { get; set; } = [];
}

public class FilterProjectDto
{
    public bool UsePaging { get; set; } = false;
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public string? SearchQuery { get; set; } = null;
    public string? CreatedBy { get; set; } = null;
    public DateTime? StartDate { get; set; } = null;
    public DateTime? EndDate { get; set; } = null;
    public Guid[]? Tags { get; set; } = null;
}

public class ProjectPageResponse
{
    public Project[] Projects { get; set; } = [];
    public int? PageIndex { get; set; }
    public int? PageSize { get; set; }
    public int? PageCount { get; set; }
    public int? TotalCount { get; set; }
    public ProjectPageResponse(Project[] projects, int? pageIndex = null, int? pageSize = null, int? pageCount = null, int? totalCount = null)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        Projects = projects;
        PageCount = pageCount;
        TotalCount = totalCount;
    }
}

public class ProjectInfoDto
{
    public Project? Project { get; set; } = null;
    public Project? RootProject { get; set; } = null;
    public List<FolderWithAddedBy> Folders { get; set; } = [];
    public List<ResourceGridItemWithAddedBy> Items { get; set; } = [];
    public List<object> Creators { get; set; } = [];
    public List<Tag?> Tags { get; set; } = [];
    public List<ProjectAncestor> Ancestors { get; set; } = [];
}

public record ProjectAncestor(Guid Id, string Title);

public class ResourceGridItemWithAddedBy(ResourceGridItem item, string addedBy)
{
    public ResourceGridItem Item { get; set; } = item;
    public string AddedBy { get; set; } = addedBy;
}

public class UpdateProjectDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Creators { get; set; }
}

public class AddFolderDto
{
    public required string Name { get; set; }
}

public class FolderWithAddedBy(Project folder, string addedBy)
{
    public Project? Folder { get; set; } = folder;
    public string? AddedBy { get; set; } = addedBy;
}
