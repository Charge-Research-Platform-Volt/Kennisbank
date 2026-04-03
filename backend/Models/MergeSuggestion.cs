namespace KnowledgeBank.Models;

/// <summary>
/// Represents a pair of similar items that may be candidates for merging.
/// </summary>
public class MergeSuggestion
{
    public Guid Id1 { get; set; }
    public string Name1 { get; set; } = string.Empty;
    public Guid Id2 { get; set; }
    public string Name2 { get; set; } = string.Empty;
    public float Score { get; set; }
}
