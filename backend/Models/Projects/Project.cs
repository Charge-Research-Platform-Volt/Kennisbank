using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using KnowledgeBank.Responses;

namespace KnowledgeBank.Models;

/// <summary>
/// A project is a structure that is defined by its ID, and has an unique title. A description is optional, and a creation
/// date / deletion date is always given at its creation (deletion date due to it not being implemented at the moment). Project type
/// is always either root project or a folder, which is a project, without the optional description and no tags.
/// 
/// Author: Justin Liem
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
    // Navigation properties for the relations a object can have (1:m)
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ICollection<ProjectTagRelation>? ProjectTagRelations { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ICollection<ProjectCreatorRelation>? ProjectCreatorRelations { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ICollection<ProjectItemRelation>? ProjectItemRelations { get; set; }

    #endregion

    [JsonIgnore][InverseProperty("ChildFolder")] public ICollection<ProjectFolderRelation>? ParentFolders { get; set; }
    [JsonIgnore][InverseProperty("ParentFolder")] public ICollection<ProjectFolderRelation>? ChildFolders { get; set; }
}

/// <summary>
/// Does not include folders or resources as those will be added only AFTER creation
/// 
/// Author: Justin Liem
/// </summary>
public class ProjectCreateDto // Data Transfer Object (DTO)
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string ProjectType {get; set;}
    public string[] Tags { get; set; } = [];
    public string[] Creators { get; set; } = [];
}

/// <summary>
/// Dto containing properties used for filtering projects, such as a search query or pagination
/// 
/// Author: Justin Liem
/// </summary>
public class FilterProjectDto
{
    // Pagination
    public bool UsePaging { get; set; } = false;
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 100;

    //Filtering
    public string? SearchQuery { get; set; } = null;
    public string? CreatedBy { get; set; } = null;
    public DateTime? StartDate { get; set; } = null;
    public DateTime? EndDate { get; set; } = null;
    public Guid[]? Tags { get; set; } = null;
}

/// <summary>
/// Page response for fetching projects
/// 
/// Author: Justin Liem
/// </summary>
public class ProjectPageResponse
{
    public Project[] Projects { get; set; } = [];
    public int? PageIndex { get; set; }
    public int? PageSize { get; set; }
    public int? PageCount { get; set; }
    public ProjectPageResponse(Project[] projects, int? pageIndex = null, int? pageSize = null, int? pageCount = null)
    {
        this.PageIndex = pageIndex;
        this.PageSize = pageSize;
        this.Projects = projects;
        this.PageCount = pageCount;
    }
}

/// <summary>
/// Dto containing information about the project / folder for fetching content
///
/// Author: Justin Liem
/// </summary>
public class ProjectInfoDto
{
    public Project? Project { get; set; } = null;
    public List<FolderWithAddedBy> Folders { get; set; } = [];
    public List<ResourceGridItemWithAddedBy> Items { get; set; } = [];
    public List<UserResponse> Creators { get; set; } = [];
    public List<Tag?> Tags { get; set; } = [];
}

/// <summary>
/// Wrapper for a library item (resource, person, or organisation grid entry) with an Added By property.
/// </summary>
/// <param name="item">The grid item.</param>
/// <param name="addedBy">Name of the user that added the item to the project/folder.</param>
public class ResourceGridItemWithAddedBy(ResourceGridItem item, string addedBy)
{
    public ResourceGridItem Item { get; set; } = item;
    public string AddedBy { get; set; } = addedBy;
}

/// <summary>
/// DTO for updating project properties.
/// Only non-null fields are applied.
/// </summary>
public class UpdateProjectDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Creators { get; set; }
}

/// <summary>
/// DTO for creating a folder inside a project or folder.
/// </summary>
public class AddFolderDto
{
    public required string Name { get; set; }
}

/// <summary>
/// Wrapper for folders containing an Added By property
/// 
/// Author: Justin Liem
/// </summary>
/// <param name="folder">Folder in the wrapper.</param>
/// <param name="addedBy">Guid of the user that added the folder to a folder/project.</param>
public class FolderWithAddedBy(Project folder, string addedBy)
{
    public Project? Folder { get; set; } = folder;
    public string? AddedBy { get; set; } = addedBy;
}
// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)