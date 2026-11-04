using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using PgVector = Pgvector.Vector;

namespace KnowledgeBank.Models.ProjectDocuments;

// Only the latest version of a document is chunked; Meilisearch does retrieval, this table is what a reindex rebuilds from
[Table("project-document-chunks")]
public class ProjectDocumentChunk
{
    [Column("id")]
    [Key]
    public required Guid Id { get; set; }

    [Column("document-id")]
    public required Guid DocumentId { get; set; }

    [Column("version-id")]
    public required Guid VersionId { get; set; }

    [Column("chunk-part")]
    public required int ChunkPart { get; set; }

    [Column("chunk-text")]
    public required string ChunkText { get; set; }

    [Column("embedding")]
    public PgVector? Embedding { get; set; }

    [Column("created-at")]
    public required DateTime CreatedAt { get; set; }

    [JsonIgnore] public ProjectDocument? Document { get; set; }
}