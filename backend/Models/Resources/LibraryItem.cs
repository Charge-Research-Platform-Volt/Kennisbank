using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeBank.Models;

public class LibraryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? PublicationDate { get; set; }
    public PublicationDatePrecision? PublicationDatePrecision { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public DateTime CreatedOn { get; set; }
    public Guid? TypeId { get; set; }
    public Guid? JournalId { get; set; }
    public EmbeddingStatus EmbeddingStatus { get; set; }
    [NotMapped]
    public List<string> Chunks { get; set; } = [];
}

public class LibrarySearchResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? PublicationDate { get; set; }
    public PublicationDatePrecision? PublicationDatePrecision { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public float Relevance { get; set; }

    // Conversion method
    public LibraryItem ToLibraryItem()
    {
        return new LibraryItem
        {
            Id = Id,
            Name = Name,
            Description = Description,
            PublicationDate = PublicationDate,
            PublicationDatePrecision = PublicationDatePrecision,
            Type = Type,
            FileType = FileType,
            CreatedOn = CreatedOn,
        };
    }
}

public class TrashItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? PublicationDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime TrashDate { get; set; }
}
