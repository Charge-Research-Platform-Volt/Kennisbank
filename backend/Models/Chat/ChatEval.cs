namespace KnowledgeBank.Models;

/// <summary>
/// One case from Eval/chat-eval.json: one or more user messages, asked in order within the same conversation
/// </summary>
public class ChatEvalCase
{
    public required string Id { get; init; }
    public required List<string> Turns { get; init; }
}

public class ChatEvalCaseResult
{
    public required string Id { get; init; }
    public List<ChatEvalTurnResult> Turns { get; init; } = [];
}

public class ChatEvalTurnResult
{
    public required string Question { get; init; }
    public string Answer { get; set; } = "";
    public string? Error { get; set; }
    public int Rounds { get; set; }
    public int ToolCalls { get; set; }
    public double? FirstTokenMs { get; set; }
    public double FinalRoundMs { get; set; }
    public double TotalMs { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int Citations { get; set; }
    public List<string> UnsupportedCitations { get; set; } = [];
    public List<string> ToolResults { get; set; } = [];
}

public class ChatEvalRunSummaryDto
{
    public Guid Id { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public required string Model { get; init; }
    public string? Label { get; init; }
    public required string Status { get; init; }
    public int CasesTotal { get; init; }
    public int CasesDone { get; init; }
    public int Turns { get; init; }
    public int FailedTurns { get; init; }
    public double? AvgTotalMs { get; init; }
    public double? AvgFirstTokenMs { get; init; }
    public double? AvgRounds { get; init; }
    public double? AvgToolCalls { get; init; }
    public long PromptTokens { get; init; }
    public long CompletionTokens { get; init; }
    public int Citations { get; init; }
    public int UnsupportedCitations { get; init; }
}

public class ChatEvalRunDetailDto
{
    public required ChatEvalRunSummaryDto Summary { get; init; }
    public required List<ChatEvalCaseResult> Cases { get; init; }
}
