using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

public class ResourceGridItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? PublicationDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
}

public class ResourceGridItemWithChunks : ResourceGridItem
{
    public List<string> Chunks { get; set; } = new List<string>();
}

public class ResourceGridSearchResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? PublicationDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public float Relevance { get; set; }

    // Conversion method
    public ResourceGridItem ToResourceGridItem()
    {
        return new ResourceGridItem
        {
            Id = Id,
            Name = Name,
            Description = Description,
            PublicationDate = PublicationDate,
            Type = Type,
            FileType = FileType,
            CreationDate = CreationDate,
        };
    }
}
