namespace KnowledgeBank.Services.AI;

public class AiService(MistralHttpClient mistralClient)
{
    private const string summarizePrompt = """
        You are a precise research summarizer. Extract only facts directly stated in the excerpts that are relevant to the query context.
    """;

    public async Task<string> SummarizeChunksAsync(string userQuestion, string searchQuery, string title, List<string> chunks, CancellationToken ct = default)
    {
        string excerpts = string.Join("\n\n", chunks);
        string prompt = $"""
            User question: {userQuestion}
            Search query used: {searchQuery}
            Source: {title}

            Relevant excerpts:
            {excerpts}

            Summarize the information in the excerpts that relates to the topic of the user question and search query.
            For broad or general questions, include the substantive content and detail from the excerpts rather than narrowing to only the sentences that literally answer the question — a topically relevant fact should be included even if it isn't a direct answer.
            Do not infer beyond what is stated or add outside knowledge — every fact must come from the excerpts.
            If the excerpts contain nothing related to the topic, respond with exactly: NO_RELEVANT_CONTENT
            Be as concise as possible while preserving all relevant detail — do not pad, do not truncate important facts.
        """;

        var request = new MistralChatRequest
        {
            Messages = [
                new { role = "system", content = summarizePrompt },
                new { role = "user", content = prompt }
            ],
            Temperature = 0f,
            ReasoningEffort = MistralReasoningEffort.None
        };

        MistralCompletion result = await mistralClient.CompleteAsync(request, ct: ct);
        return result.Content ?? "";
    }
}