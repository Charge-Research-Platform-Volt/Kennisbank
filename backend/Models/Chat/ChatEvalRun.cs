using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeBank.Models;

public enum ChatEvalRunStatus { Running, Completed, Cancelled, Failed, Interrupted }

/// <summary>
/// One run of the chat eval cases. The results are stored as a single JSON document, since they're only ever read and written whole.
/// </summary>
[Table("chat-eval-runs")]
public class ChatEvalRun
{
    [Column("id")]
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("started-at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column("completed-at")]
    public DateTime? CompletedAt { get; set; }

    [Column("model")]
    public required string Model { get; set; }

    [Column("label")]
    public string? Label { get; set; }

    [Column("status")]
    public ChatEvalRunStatus Status { get; set; } = ChatEvalRunStatus.Running;

    [Column("cases-total")]
    public int CasesTotal { get; set; }

    [Column("cases-done")]
    public int CasesDone { get; set; }

    // Serialized List<ChatEvalCaseResult>
    [Column("results", TypeName = "jsonb")]
    public string Results { get; set; } = "[]";
}