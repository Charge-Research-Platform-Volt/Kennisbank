using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;


[Table("website-metadata")]
public class WebsiteMetadata
{
    [Column("resource-id")]
    [Key]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("url")]
    public required string Url { get; set; }

    [Column("accessed-on")]
    public DateTime? AccessedOn { get; set; }

    // Navigation property to parent (1:1)
    [JsonIgnore] public Resource? Resource { get; set; }
}

public class WebsiteCreateDto : ResourceCreateDto
{
    public required string Url { get; set; }
    public DateTime? AccessedOn { get; set; }
}

public enum WebsiteColumn{
    Url, 
    Title
}