namespace KnowledgeBank.Services.Search;

public static class SearchTuning
{
    public static double GetSemanticRatio(string query)
    {
        int wordCount = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return wordCount switch
        {
            <= 2 => 0.3,
            <= 6 => 0.5,
            _ => 0.7
        };
    }
}
