using System.ComponentModel.DataAnnotations.Schema;
namespace KnowledgeBank.Models;

[Table("FileUserTag")]
public class FileUserTag
{
    public required Guid DocId { get; set; }
    public required Guid UserTagId { get; set; }
}