using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using KnowledgeBank.Services;

namespace KnowledgeBank.Models;

[Table("entity-chunks")]
public class EntityChunk 
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }
    
    [Column("entity-id")]
    [ForeignKey("Entity")]
    public required Guid EntityId { get; set; }
    
    [Column("chunk-type")]
    public required ChunkType ChunkType { get; set; }
    
    [Column("chunk-text")]
    public required string ChunkText { get; set; }
    
    [Column("chunk-part")]
    public required int ChunkPart { get; set; }
    
    [Column("embedding")]
    public Pgvector.Vector? Embedding { get; set; }
    
    [Column("created-at")]
    public required DateTime CreatedAt { get; set; }
    
    [JsonIgnore]
    public Entity? Entity { get; set; }
}