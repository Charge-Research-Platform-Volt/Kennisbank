using KnowledgeBank.Utils;

namespace KnowledgeBank.Services.AI;

public class AiService(MistralHttpClient mistralClient)
{
    private const string summarizePrompt = """
        You are a precise research summarizer. Extract only facts directly stated in the excerpts that are relevant to the query context.
    """;

    public async Task<string> SummarizeChunksAsync(string queryContext, string title, List<string> chunks, CancellationToken ct = default)
    {
        string excerpts = string.Join("\n\n", chunks);
        string prompt = $"""
            Query context: {queryContext}
            Source: {title}

            Relevant excerpts:
            {excerpts}

            Extract only information directly relevant to the query context. Factual only, no inference. Max 150 words.
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

        MistralCompletion result = await mistralClient.CompleteAsync(request, ct);
        return result.Content ?? "";
    }
}