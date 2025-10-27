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
    public List<string> Authors { get; set; } = new();
    public string? PublicationCode { get; set; }
    public List<string> Tags { get; set; } = new();
}