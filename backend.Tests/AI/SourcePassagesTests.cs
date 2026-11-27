using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Tests.AI;

public class SourcePassagesTests
{
    // Paragraph i is "Paragraph ii xxx..." (98 characters) at 100 * i, followed by "\n\n"
    private const int ParagraphCount = 30;
    private static readonly string Body = string.Join("\n\n", Enumerable.Range(0, ParagraphCount).Select(Paragraph));

    private static string Paragraph(int i) => $"Paragraph {i:D2} {new string('x', 85)}";
    private static MatchedChunk ChunkOf(int paragraph) => new($"Document: Report\n\n{Paragraph(paragraph)}", paragraph * 100, paragraph * 100 + 98);

    private static ResourceText Text(string text, int[]? pageStarts = null)
        => new() { ResourceId = Guid.NewGuid(), Text = text, PageStarts = pageStarts, ExtractedAt = DateTime.UtcNow };

    [Fact]
    public void WithoutText_ChunksAreShownAsTheyAre()
    {
        List<string> passages = SourcePassages.Build([new MatchedChunk("Title: Report"), new MatchedChunk("Some chunk", 0, 10)], null);

        Assert.Equal(["Title: Report", "Some chunk"], passages);
    }

    [Fact]
    public void ChunkWithoutPosition_IsShownAsItIs()
        => Assert.Equal(["Title: Report\nAuthors: X"], SourcePassages.Build([new MatchedChunk("Title: Report\nAuthors: X")], Text(Body)));

    [Theory]
    [InlineData(0, 99_999)]
    [InlineData(-1, 50)]
    [InlineData(50, 50)]
    public void PositionThatDoesNotFitTheText_ShowsTheChunkAsItIs(int start, int end)
        => Assert.Equal(["stale chunk"], SourcePassages.Build([new MatchedChunk("stale chunk", start, end)], Text(Body)));

    [Fact]
    public void WithoutContext_ThePassageIsTheBareChunk()
    {
        string passage = Assert.Single(SourcePassages.Build([ChunkOf(15)], Text(Body), contextChars: 0));

        Assert.Equal(Paragraph(15), passage);
    }

    [Fact]
    public void Slice_AddsTheWholeParagraphsAroundTheChunkThatFit()
    {
        // 250 characters of context reach into paragraphs 12 and 18, which don't fit whole
        string passage = Assert.Single(SourcePassages.Build([ChunkOf(15)], Text(Body), contextChars: 250));

        Assert.Equal(string.Join("\n\n", Enumerable.Range(13, 5).Select(Paragraph)), passage);
    }

    [Fact]
    public void ChunkNearTheEdges_ReachesTheStartAndEndOfTheText()
    {
        Assert.StartsWith(Paragraph(0), Assert.Single(SourcePassages.Build([ChunkOf(1)], Text(Body), contextChars: 250)));
        Assert.EndsWith(Paragraph(ParagraphCount - 1), Assert.Single(SourcePassages.Build([ChunkOf(ParagraphCount - 2)], Text(Body), contextChars: 250)));
    }

    [Fact]
    public void WithoutParagraphBreaks_TheSliceIsCutAtSentences()
    {
        string text = string.Concat(Enumerable.Range(0, 100).Select(i => $"Sentence number {i:D2} ends here. "));
        int start = text.IndexOf("Sentence number 50", StringComparison.Ordinal);
        int end = text.IndexOf("Sentence number 52", StringComparison.Ordinal) - 1;

        string passage = Assert.Single(SourcePassages.Build([new MatchedChunk("chunk", start, end)], Text(text), contextChars: 100));

        Assert.StartsWith("Sentence number ", passage);
        Assert.EndsWith("ends here.", passage);
        Assert.Contains(text[start..end], passage);
    }

    [Fact]
    public void OverlappingSlices_AreMerged()
    {
        string passage = Assert.Single(SourcePassages.Build([ChunkOf(15), ChunkOf(17)], Text(Body), contextChars: 250));

        Assert.Equal(string.Join("\n\n", Enumerable.Range(13, 7).Select(Paragraph)), passage);
    }

    [Fact]
    public void SlicesWithOnlyWhitespaceBetween_AreMerged()
    {
        string passage = Assert.Single(SourcePassages.Build([ChunkOf(10), ChunkOf(11)], Text(Body), contextChars: 0));

        Assert.Equal($"{Paragraph(10)}\n\n{Paragraph(11)}", passage);
    }

    [Fact]
    public void Passages_KeepTheRankOfTheirBestChunk()
    {
        List<string> passages = SourcePassages.Build([ChunkOf(25), new MatchedChunk("metadata"), ChunkOf(11), ChunkOf(10), ChunkOf(2)], Text(Body), contextChars: 0);

        Assert.Equal([Paragraph(25), "metadata", $"{Paragraph(10)}\n\n{Paragraph(11)}", Paragraph(2)], passages);
    }

    [Fact]
    public void Label_ShowsThePage()
    {
        string passage = Assert.Single(SourcePassages.Build([ChunkOf(15)], Text(Body, pageStarts: [0, 1000, 2000]), contextChars: 0));

        Assert.Equal($"(p. 2)\n{Paragraph(15)}", passage);
    }

    [Fact]
    public void Label_ShowsThePageRangeOfASliceAcrossPages()
    {
        string passage = Assert.Single(SourcePassages.Build([ChunkOf(19), ChunkOf(20)], Text(Body, pageStarts: [0, 1000, 2000]), contextChars: 0));

        Assert.StartsWith("(pp. 2-3)\n", passage);
    }

    [Fact]
    public void Label_ShowsTheSectionTheSliceOpensIn()
    {
        string text = "# Introduction\n\nOpening words.\n\n## Results\n\nFirst finding.\n\nSecond finding.";
        int start = text.IndexOf("Second", StringComparison.Ordinal);

        string passage = Assert.Single(SourcePassages.Build([new MatchedChunk("chunk", start, text.Length)], Text(text), contextChars: 0));

        Assert.Equal("(section \"Results\")\nSecond finding.", passage);
    }

    [Fact]
    public void Label_LeavesOutTheSectionWhenTheSliceOpensWithItsHeading()
    {
        string text = "# Introduction\n\nOpening words.\n\n## Results\n\nFirst finding.";
        int start = text.IndexOf("## Results", StringComparison.Ordinal);

        string passage = Assert.Single(SourcePassages.Build([new MatchedChunk("chunk", start, text.Length)], Text(text), contextChars: 0));

        Assert.Equal("## Results\n\nFirst finding.", passage);
    }

    [Fact]
    public void Label_CombinesPageAndSection()
    {
        string text = "# Methods\n\nWe asked people.\nWe counted answers.\n";
        int start = text.IndexOf("We counted", StringComparison.Ordinal);

        string passage = Assert.Single(SourcePassages.Build([new MatchedChunk("chunk", start, text.Length)], Text(text, pageStarts: [0, start]), contextChars: 0));

        Assert.Equal("(p. 2, section \"Methods\")\nWe counted answers.", passage);
    }

    [Fact]
    public void RandomChunks_AlwaysGiveSlicesThatContainThem()
    {
        Random random = new(42);
        string[] words = ["council", "budget", "Citizens", "vote.", "assembly", "\n", "\n\n", "pilot?", "the", "of"];

        for (int run = 0; run < 200; run++)
        {
            string text = string.Join(" ", Enumerable.Range(0, random.Next(50, 800)).Select(_ => words[random.Next(words.Length)]));
            int start = random.Next(0, text.Length - 1);
            int end = random.Next(start + 1, Math.Min(text.Length, start + 400) + 1);
            string chunk = text[start..end].Trim();
            if (chunk.Length == 0) continue;

            string passage = Assert.Single(SourcePassages.Build([new MatchedChunk("chunk", start, end)], Text(text), contextChars: random.Next(0, 300)));

            Assert.Contains(chunk, passage);
            Assert.Equal(passage.Trim(), passage);
        }
    }
}
