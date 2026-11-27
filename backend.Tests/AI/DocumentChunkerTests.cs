using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Tests.AI;

public class DocumentChunkerTests
{
    private static string Prose(int paragraphs, int sentencesPerParagraph, string newline = "\n")
    {
        System.Text.StringBuilder sb = new();
        for (int p = 0; p < paragraphs; p++)
        {
            for (int s = 0; s < sentencesPerParagraph; s++)
                sb.Append($"Sentence {s} of paragraph {p} talks about democratic participation and councils. ");
            sb.Append(newline).Append(newline);
        }
        return sb.ToString();
    }

    /// <summary>What must hold for any text: in bounds, trimmed, nothing lost, always advancing, bounded overlap</summary>
    private static void AssertInvariants(string text, List<ChunkSpan> spans, int chunkChars, int overlapChars)
    {
        Assert.All(spans, s =>
        {
            Assert.InRange(s.Start, 0, text.Length - 1);
            Assert.InRange(s.End, s.Start + 1, text.Length);
            Assert.True(s.Length <= chunkChars);
            Assert.False(char.IsWhiteSpace(text[s.Start]));
            Assert.False(char.IsWhiteSpace(text[s.End - 1]));
        });

        for (int i = 0; i < text.Length; i++)
            if (!char.IsWhiteSpace(text[i]))
                Assert.Contains(spans, s => s.Start <= i && i < s.End);

        foreach ((ChunkSpan previous, ChunkSpan next) in spans.Zip(spans.Skip(1)))
        {
            Assert.True(next.Start > previous.Start);
            Assert.True(previous.End - next.Start <= overlapChars);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \n\n \t")]
    public void EmptyOrWhitespace_GivesNoChunks(string text) => Assert.Empty(DocumentChunker.Split(text));

    [Fact]
    public void ShortText_IsOneTrimmedChunk()
    {
        const string text = "  Short text.  ";
        ChunkSpan span = Assert.Single(DocumentChunker.Split(text));
        Assert.Equal("Short text.", text[span.Start..span.End]);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Prose_BreaksAtParagraphsWithOverlap(string newline)
    {
        string text = Prose(40, 6, newline);
        List<ChunkSpan> spans = DocumentChunker.Split(text);

        AssertInvariants(text, spans, DocumentChunker.DefaultChunkChars, DocumentChunker.DefaultOverlapChars);
        Assert.All(spans.SkipLast(1), s => Assert.StartsWith(newline + newline, text[s.End..].TrimStart(' ')));
        Assert.All(spans.Skip(1), s => Assert.True(char.IsWhiteSpace(text[s.Start - 1]), "overlap starts mid-word"));
        Assert.All(spans.Zip(spans.Skip(1)), p => Assert.True(p.First.End > p.Second.Start, "no overlap"));
    }

    [Fact]
    public void OneLongParagraph_BreaksAtSentenceEnds()
    {
        string text = Prose(1, 200).Replace("\n", " ");
        List<ChunkSpan> spans = DocumentChunker.Split(text);

        AssertInvariants(text, spans, DocumentChunker.DefaultChunkChars, DocumentChunker.DefaultOverlapChars);
        Assert.All(spans.SkipLast(1), s => Assert.Equal('.', text[s.End - 1]));
    }

    [Fact]
    public void TextWithoutWhitespace_IsCutHard()
    {
        string text = new('x', 5000);
        List<ChunkSpan> spans = DocumentChunker.Split(text);

        AssertInvariants(text, spans, DocumentChunker.DefaultChunkChars, DocumentChunker.DefaultOverlapChars);
        Assert.Equal(3, spans.Count);
    }

    [Fact]
    public void RandomTexts_KeepInvariants()
    {
        Random random = new(42);
        char[] alphabet = ['a', 'b', ' ', '\n', '.', '?', 'c', 'd', 'e', '\r'];

        for (int run = 0; run < 300; run++)
        {
            string text = new([.. Enumerable.Range(0, random.Next(0, 6000)).Select(_ => alphabet[random.Next(alphabet.Length)])]);
            int chunkChars = random.Next(20, 2500);
            int overlapChars = random.Next(0, chunkChars / 2);

            AssertInvariants(text, DocumentChunker.Split(text, chunkChars, overlapChars), chunkChars, overlapChars);
        }
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(100, 50)]
    [InlineData(100, -1)]
    public void InvalidSizes_Throw(int chunkChars, int overlapChars)
        => Assert.Throws<ArgumentOutOfRangeException>(() => DocumentChunker.Split("abc", chunkChars, overlapChars));
}
