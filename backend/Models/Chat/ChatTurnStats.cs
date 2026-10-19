namespace KnowledgeBank.Models;

/// <summary>
/// Filled in by the orchestration loop while one chat turn runs. Feeds the chat.* usage metrics and the chat eval runner.
/// </summary>
public class ChatTurnStats
{
    public int Rounds { get; set; }
    public int ToolCalls { get; set; }
    public TimeSpan? FirstToken { get; set; }
    public TimeSpan FinalRound { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public List<string> ToolResults { get; } = [];
}