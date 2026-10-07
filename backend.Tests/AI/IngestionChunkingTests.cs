using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Tests.AI;

public class IngestionChunkingTests
{
    private static readonly string LongText = string.Join(" ",
        Enumerable.Range(1, 400).Select(i => $"Sentence {i} is about grid congestion and renewable energy."));

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void EmptyText_HasNoChunks(string text)
        => Assert.Empty(IngestionService.SplitTextIntoChunks(text));

    [Fact]
    public void ShortText_IsOneChunk()
        => Assert.Equal(["Heat pumps are efficient."], IngestionService.SplitTextIntoChunks("Heat pumps are efficient."));

    [Fact]
    public void LongText_IsSplitIntoSeveralChunks_WithoutLosingContent()
    {
        List<string> chunks = IngestionService.SplitTextIntoChunks(LongText);

        Assert.True(chunks.Count > 1);
        foreach (int i in new[] { 1, 200, 400 })
            Assert.Contains(chunks, c => c.Contains($"Sentence {i} is about"));
    }

    [Fact]
    public void LongText_ConsecutiveChunksOverlap()
    {
        List<string> chunks = IngestionService.SplitTextIntoChunks(LongText);

        for (int i = 1; i < chunks.Count; i++)
        {
            string firstSentence = chunks[i][..chunks[i].IndexOf('.')];
            Assert.Contains(firstSentence, chunks[i - 1]);
        }
    }

    [Fact]
    public void LongText_ChunksStayWithinTokenBudget()
    {
        // The default token estimate is about four characters per token
        int maxChars = IngestionService.MaxChunkTokens * 4;

        Assert.All(IngestionService.SplitTextIntoChunks(LongText), c => Assert.True(c.Length <= maxChars, $"chunk of {c.Length} chars"));
    }

    [Fact]
    public void OverlapNotSmallerThanChunkSize_FallsBackToDefaults()
        => Assert.Equal(
            IngestionService.SplitTextIntoChunks(LongText),
            IngestionService.SplitTextIntoChunks(LongText, chunkSize: 100, overlapSize: 100));

    [Fact]
    public void MarkdownMode_KeepsAllSections()
    {
        string markdown = string.Join("\n\n", Enumerable.Range(1, 60).Select(i => $"## Section {i}\n\n{LongText[..300]}"));

        List<string> chunks = IngestionService.SplitTextIntoChunks(markdown, markdownSplit: true);

        Assert.True(chunks.Count > 1);
        Assert.Contains(chunks, c => c.Contains("## Section 1"));
        Assert.Contains(chunks, c => c.Contains("## Section 60"));
    }
}
