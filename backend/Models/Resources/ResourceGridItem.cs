namespace KnowledgeBank.Models;

public class ResourceGridItem 
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? PublicationDate { get; set; }
    public string Type { get; set; } = string.Empty;
}