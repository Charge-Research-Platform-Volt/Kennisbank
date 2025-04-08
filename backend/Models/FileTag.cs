using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("FileTag")]
public class FileTag
{
    public required Guid DocId { get; set; }
    public required Guid TagId { get; set; }
}