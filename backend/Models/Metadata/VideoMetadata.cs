using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;


[Table("video-metadata")]
public class VideoMetadata
{
    [Column("resource-id")]
    [Key]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    // The length of the audio file in ms
    [Column("length")]
    public ulong? Length { get; set; }

    // Navigation property to parent (1:1)
    [JsonIgnore] public Resource? Resource { get; set; }
}

public class VideoCreateDto : FileResourceCreateDto
{
    public ulong? Length { get; set; }
}