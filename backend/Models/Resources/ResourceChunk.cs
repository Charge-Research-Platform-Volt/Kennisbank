using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using KnowledgeBank.Services.AI;
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

    /// <summary>Where the chunk sits in the resource's stored text (resource-texts): Text[StartChar..EndChar]. Null for the metadata chunk.</summary>
    [Column("start-char")]
    public int? StartChar { get; set; }
    
    [Column("end-char")]
    public int? EndChar { get; set; }

    [Column("embedding")]
    public PgVector? Embedding { get; set; }

    [Column("created-at")]
    public required DateTime CreatedAt { get; set; }

    // Navigation properties
    [JsonIgnore]
    public Resource? Resource { get; set; }
}
