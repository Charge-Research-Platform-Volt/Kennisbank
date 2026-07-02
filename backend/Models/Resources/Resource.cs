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

    [Column("abstract")]
    public string? Abstract { get; set; }

    [Column("type-id")]
    [ForeignKey("ResourceType")]
    public Guid? TypeId { get; set; }

    [Column("language-code")]
    [MaxLength(2)]
    public required string LanguageCode { get; set; }

    [Column("publication-code")]
    public string? PublicationCode { get; set; }

    [Column("publication-date")]
    public DateTime? PublicationDate { get; set; }

    [Column("journal-id")]
    [ForeignKey("Journal")]
    public Guid? JournalId { get; set; }

    [Column("publication-date-precision")]
    public PublicationDatePrecision? PublicationDatePrecision { get; set; }

    [Column("created-on")]
    public required DateTime CreatedOn { get; set; }

    [Column("created-by")]
    public required Guid CreatedBy { get; set; }

    [Column("license")]
    public string? License { get; set; }

    [Column("note")]
    public string? Note { get; set; }

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
    [JsonIgnore] public ResourceType? ResourceType { get; set; }

    [JsonIgnore] public Journal? Journal { get; set; }
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
    public string? SuggestedAlias { get; set; }
}

public class AuthorEntry
{
    public required string Value { get; set; } // GUID for existing entity, or name for new entity
    public string? Type { get; set; } // "person" or "organisation" - needed when creating new entities
    public string? SuggestedAlias { get; set; }
}

public class ResourceCreateDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Abstract { get; set; }
    public required string TypeId { get; set; }
    public required string LanguageCode { get; set; }
    public string? PublicationCode { get; set; }
    public DateTime? PublicationDate { get; set; }
    public string? JournalId { get; set; }
    public PublicationDatePrecision? PublicationDatePrecision { get; set; }
    public string? License { get; set; }
    public string? SourceUrl { get; set; }
    public string? Note { get; set; }
    public string[] Tags { get; set; } = [];
    public AuthorEntry[] Authors { get; set; } = [];
    public RelatedEntry[] Organisations { get; set; } = [];
    public string[] Regions { get; set; } = [];
    public RelatedEntry[] RelatedPersons { get; set; } = [];
    // File-specific (null for websites)
    public string? FileId { get; set; }
    public string? FileExtension { get; set; }
    public string? Hash { get; set; }
}

public class ResourceUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Abstract { get; set; }
    public string? TypeId { get; set; }
    public string? LanguageCode { get; set; }
    public string? PublicationCode { get; set; }
    public DateTime? PublicationDate { get; set; }
    public string? JournalId { get; set; }
    public PublicationDatePrecision? PublicationDatePrecision { get; set; }
    public string? License { get; set; }
    public string? Note { get; set; }
    public string? SourceUrl { get; set; }
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

public class ResourceDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Abstract { get; set; }
    public Guid? TypeId { get; set; }
    public string? TypeName { get; set; }
    public string LanguageCode { get; set; } = null!;
    public string? PublicationCode { get; set; }
    public DateTime? PublicationDate { get; set; }
    public PublicationDatePrecision? PublicationDatePrecision { get; set; }
    public Guid? JournalId { get; set; }
    public string? JournalName { get; set; }
    public DateTime CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public string? License { get; set; }
    public string? Note { get; set; }
    public string FileType { get; set; } = null!;
    public string? FileExt { get; set; }
    public string? Hash { get; set; }
    public string? SourceUrl { get; set; }
    public bool Trashed { get; set; }
    public DateTime? TrashDate { get; set; }
    public RelationItemDto[] Authors { get; set; } = [];
    public RelationItemDto[] Organisations { get; set; } = [];
    public RelationItemDto[] Regions { get; set; } = [];
    public RelationItemDto[] RelatedPersons { get; set; } = [];
    public RelationItemDto[] Tags { get; set; } = [];
}
