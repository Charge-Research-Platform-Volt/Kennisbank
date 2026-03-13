using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

[Table("resources")]
public class Resource
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("title")]
    public required string Title { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("type-id")]
    [ForeignKey("ResourceType")]
    public required Guid TypeId { get; set; }

    [Column("language-code")]
    [MaxLength(2)]
    public required string LanguageCode { get; set; }

    [Column("publication-code")]
    public string? PublicationCode { get; set; }

    [Column("publication-date")]
    public DateTime? PublicationDate { get; set; }

    [Column("publication-date-precision")]
    public PublicationDatePrecision? PublicationDatePrecision { get; set; }

    [Column("creation-date")]
    public required DateTime CreationDate { get; set; }

    [Column("license")]
    public string? License { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("ai-generated-tags")]
    public string? AiGeneratedTags { get; set; }

    [Column("filetype")]
    [MaxLength(50)]
    public required string FileType { get; set; }
    
    [Column("file-ext")]
    [MaxLength(5)]
    public string? FileExt { get; set; }

    [Column("hash")]
    [MaxLength(64)]
    public string? Hash { get; set; }

    [Column("source-url")]
    public string? SourceUrl { get; set; }

    [Column("trashed")]
    public bool Trashed { get; set; } = false;

    [Column("trash-date")]
    public DateTime? TrashDate { get; set; } = null;

    #region Direct navigation properties
    // Navigation property for Vector (one to one). JsonIgnore excludes it from response bodies.

    // Navigation property for Document metadata (1:1)
    [JsonIgnore] public DocumentMetadata? DocumentMetadata { get; set; }

    // Navigation property for Website metadata (1:1)
    [JsonIgnore] public WebsiteMetadata? WebsiteMetadata { get; set; }

    // Navigation property for Audio metadata (1:1)
    [JsonIgnore] public AudioMetadata? AudioMetadata { get; set; }

    // Navigation property for Video metadata (1:1)
    [JsonIgnore] public VideoMetadata? VideoMetadata { get; set; }

    // Navigation property for Resource Type (1:1)
    [JsonIgnore] public ResourceType? ResourceType { get; set; }
    #endregion

    #region Relation navigation properties
    // Navigation properties for the relations a object can have (1:m)
    [JsonIgnore] public ICollection<ResourceAuthorRelation>? ResourceAuthorRelations { get; set; }
    [JsonIgnore] public ICollection<ResourceOrganisationRelation>? ResourceOrganisationRelations { get; set; }
    [JsonIgnore] public ICollection<ResourceRegionRelation>? ResourceRegionRelations { get; set; }
    [JsonIgnore] public ICollection<ResourceRelatedPersonRelation>? ResourceRelatedPersonRelations { get; set; }
    [JsonIgnore] public ICollection<ResourceTagRelation>? ResourceTagRelations { get; set; }

    #endregion
}

public class RelatedEntry
{
    public required string Id { get; set; }
    public string? Relation { get; set; }
}

public class AuthorEntry
{
    public required string Value { get; set; } // GUID for existing entity, or name for new entity
    public string? Type { get; set; } // "person" or "organisation" - needed when creating new entities
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "uploadType")]
[JsonDerivedType(typeof(WebsiteCreateDto), "website")]
[JsonDerivedType(typeof(DocumentCreateDto), "document")]
[JsonDerivedType(typeof(AudioCreateDto), "audio")]
[JsonDerivedType(typeof(VideoCreateDto), "video")]
public class ResourceCreateDto // Data Transfer Object (DTO)
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string TypeId { get; set; }
    public required string LanguageCode { get; set; }
    public string? PublicationCode { get; set; }
    public DateTime? PublicationDate { get; set; }
    public PublicationDatePrecision? PublicationDatePrecision { get; set; }
    public DateTime? CreationDate { get; set; }
    public string? License { get; set; }
    public string? SourceUrl { get; set; }
    public string? Note { get; set; }
    public string[] Tags { get; set; } = [];
    public AuthorEntry[] Authors { get; set; } = [];
    public RelatedEntry[] Organisations { get; set; } = [];
    public string[] Regions { get; set; } = [];
    public RelatedEntry[] RelatedPersons { get; set; } = [];
}

public class FileResourceCreateDto : ResourceCreateDto
{
    public string? Hash { get; set; } = null;
    public string FileExtension { get; set; } = "";
    public required string Id { get; set; }
}

public class ResourceUploadDto
{
    public required string Dto { get; set; }
    public required string UploadType { get; set; }
    public IFormFile? File { get; set; }
}

public class FileUploadInitDto
{
    public required string FileName { get; set; }
    public required long FileSize { get; set; }
}

public class FileUploadFinalizeDto
{
    public required string ObjectName { get; set; }
    public required string UploadId { get; set; }
    public required IDictionary<int, string> PartETags { get; set; }
}