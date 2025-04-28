using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
namespace KnowledgeBank.Models;

[Table("tags")]
[Index(nameof(Name), IsUnique = true)]
public class Tag
{
    [Key]
    [Column("id")]
    public required Guid Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("is-standardized")]
    public required bool IsStandardized { get; set; } = false;

    [Column("is-approved")]
    public bool IsApproved { get; set; } = false;

    [Column("approved-on")]
    public DateTime? ApprovedOn { get; set; }

    [Column("approved-by")]
    public Guid? ApprovedBy { get; set; }

    [Column("created-by")]
    public required Guid CreatedBy { get; set; }

    [Column("created-on")]
    public required DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore] public ICollection<ResourceTagRelation>? ResourceTagRelations { get; set; }
    
    // Non-mapped, runtime-only properties used for view logic / API response shaping.
    [NotMapped]
    public int UsageCount { get; set; }
}

public class TagCreateDto
{
    public required string Name { get; set; }
    public bool IsApproved { get; set; } = false;
    public string? ApprovedBy { get; set; }
    public string CreatedBy { get; set; } = "";
}

/// <summary>
/// DTO for tag filtering options
/// </summary>
public class TagFilterOptions
{
    // Pagination
    public bool UsePaging { get; set; } = false;
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 100;

    // Filtering
    public string? SearchQuery { get; set; }
    public Guid? CreatedBy { get; set; }
    public bool OnlyOwnedByCurrentUser { get; set; } = false;
    public bool? IsApproved { get; set; }
    public bool? IsStandardized { get; set; }
    public DateTime? CreatedFromDate { get; set; }
    public DateTime? CreatedToDate { get; set; }
    public DateTime? ApprovedFromDate { get; set; }
    public DateTime? ApprovedToDate { get; set; }

    // Additional processing
    public bool IncludeUsageCount { get; set; } = true;

    // Sorting
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = true;
    
    // Dynamic weighted sorting
    /// <summary>
    /// Comma-separated list of property weight expressions in format "PropertyName:Weight"
    /// Example: "IsStandardized:2,IsApproved:1,UsageCount:0.5"
    /// </summary>
    public string? WeightedSort { get; set; } 
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


