using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Org.BouncyCastle.Asn1.X509.Qualified;

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
    public required DateTime PublicationDate { get; set; }

    [Column("creation-date")]
    public required DateTime CreationDate { get; set; }

    [Column("license")]
    public string? License { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("filetype")]
    [MaxLength(50)]
    public required string FileType { get; set; }

    [Column("hash")]
    [MaxLength(64)]
    public string? Hash { get; set; }

    #region Direct navigation properties
    // Navigation property for Vector (one to one). JsonIgnore excludes it from response bodies.
    [JsonIgnore] public ResourceVector? Vector { get; set; }

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
    [JsonIgnore] public ICollection<ResourceRelatedOrganisationRelation>? ResourceRelatedOrganisationRelations { get; set; }
    [JsonIgnore] public ICollection<ResourceRelatedSourceRelation>? ResourceRelatedSourceRelations { get; set; }
    [JsonIgnore] public ICollection<ResourceSourceRelation>? ResourceSourceRelations { get; set; }
    [JsonIgnore] public ICollection<ResourceTagRelation>? ResourceTagRelations { get; set; }

    #endregion
}

public class RelatedEntry 
{
    public required string Id { get; set; }
    public string? Relation { get; set; }
}

public class ResourceCreateDto // Data Transfer Object (DTO)
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string TypeId { get; set; }
    public required string LanguageCode { get; set; }
    public string? PublicationCode { get; set; }
    public required DateTime PublicationDate { get; set; }
    public string? License { get; set; }
    public string[] Sources { get; set; } = [];
    public string? Note { get; set; }
    public string[] Tags { get; set; } = [];
    public string[] Authors { get; set; } = [];
    // Tuple: (OrganisationId, role?)
    public RelatedEntry[] Organisations { get; set; } = [];
    public string[] Regions { get; set; } = [];
    // Tuple: (OrganisationId, role?)
    public RelatedEntry[] RelatedOrganisations { get; set; } = [];
    // Tuple: (PersonId, role?)
    public RelatedEntry[] RelatedPersons { get; set; } = [];
    public string[] RelatedSources { get; set; } = [];
}

public class FileResourceCreateDto : ResourceCreateDto
{
    public string? Hash { get; set; } = null;
    public IFormFile? File { get; set; }
}

public class LargeFileFinalizeDto
{
    public required string ResourceId { get; set; }
    public required string FileType { get; set; }
    public required string FileName { get; set; }
    public List<string> BlockIds { get; set; } = new();
}

public class ResourceUploadDto 
{
    public required string Dto { get; set; }
    public required string UploadType { get; set; }
    public IFormFile? File { get; set; }
}

public class ResourceRenameDto
{
    public required string Id { get; set; }
    public required string Title { get; set; }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


