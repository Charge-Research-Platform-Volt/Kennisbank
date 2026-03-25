using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using KnowledgeBank.Services;
using PgVector = Pgvector.Vector;

namespace KnowledgeBank.Models;

[Table("resource-chunks")]
public class ResourceChunk
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("resource-id")]
    [ForeignKey("Resource")]
    public required Guid ResourceId { get; set; }

    [Column("chunk-type")]
    public required ChunkType ChunkType { get; set; }

    [Column("chunk-text")]
    public required string ChunkText { get; set; }

    [Column("chunk-part")]
    public required int ChunkPart { get; set; }

    [Column("embedding")]
    public PgVector? Embedding { get; set; }

    [Column("created-at")]
    public required DateTime CreatedAt { get; set; }

    // Navigation properties
    [JsonIgnore]
    public Resource? Resource { get; set; }
}
