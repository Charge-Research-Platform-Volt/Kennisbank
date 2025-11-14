namespace KnowledgeBank.Models;

/// <summary>
/// Metadata extracted from a document using LLM analysis
/// </summary>
public class ExtractedMetadata
{
    public string? Title { get; set; }
    public string? Abstract { get; set; }
    public string? Description { get; set; }
    public DateTime? PublicationDate { get; set; }
    public string? LanguageCode { get; set; }
    public List<AuthorWithSimilars> Authors { get; set; } = [];
    public string? PublicationCode { get; set; }
    public List<string> Tags { get; set; } = [];
}

/// <summary>
/// Represents an author with potential similar entities from the database
/// </summary>
public class AuthorWithSimilars
{
    public string Name { get; set; } = string.Empty;
    public List<SimilarEntity> Similars { get; set; } = [];
}

/// <summary>
/// Represents a similar entity found in the database during metadata extraction
/// </summary>
public class SimilarEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public float Score { get; set; }
    public string Type { get; set; } = string.Empty; // "person" or "organisation"
}