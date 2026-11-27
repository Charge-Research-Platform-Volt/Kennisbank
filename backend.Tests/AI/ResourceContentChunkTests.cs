using System.Text;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Vector;

namespace KnowledgeBank.Tests.AI;

public class ResourceContentChunkTests
{
    private static string Paragraphs(string label, int count)
    {
        StringBuilder sb = new();
        for (int i = 0; i < count; i++)
            sb.Append($"{label} paragraph {i} explains how councils organise citizen participation in practice.\n\n");
        return sb.ToString();
    }

    private static string? SectionOf(ResourceChunkInput chunk)
        => chunk.Text.Split('\n').FirstOrDefault(l => l.StartsWith("Section: "))?["Section: ".Length..];

    [Fact]
    public void EmptyText_GivesNoChunks()
        => Assert.Empty(IngestionService.BuildContentChunks("Title", "   ", firstPart: 1));

    [Fact]
    public void Positions_PointAtTheBareChunkInTheText()
    {
        string text = Paragraphs("Body", 120);

        List<ResourceChunkInput> chunks = IngestionService.BuildContentChunks("Participation report", text, firstPart: 1);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c =>
        {
            Assert.NotNull(c.StartChar);
            Assert.NotNull(c.EndChar);
            Assert.EndsWith(text[c.StartChar!.Value..c.EndChar!.Value], c.Text);
            Assert.Equal(ChunkType.ContentText, c.Type);
        });
    }

    [Fact]
    public void Parts_AreNumberedFromFirstPart()
    {
        List<ResourceChunkInput> chunks = IngestionService.BuildContentChunks("Title", Paragraphs("Body", 120), firstPart: 1);

        Assert.Equal(Enumerable.Range(1, chunks.Count), chunks.Select(c => c.Part));
    }

    [Fact]
    public void Context_StartsWithTheDocumentTitle()
    {
        ResourceChunkInput chunk = Assert.Single(IngestionService.BuildContentChunks("Participation report", "Short body.", firstPart: 1));

        Assert.Equal("Document: Participation report\n\nShort body.", chunk.Text);
    }

    [Fact]
    public void WithoutTitleOrHeading_TheChunkIsBare()
    {
        ResourceChunkInput chunk = Assert.Single(IngestionService.BuildContentChunks("  ", "Short body.", firstPart: 1));

        Assert.Equal("Short body.", chunk.Text);
    }

    [Fact]
    public void Section_IsTheHeadingAboveWhereEachChunksOwnTextStarts()
    {
        string text = "# Introduction\n\n" + Paragraphs("Intro", 40) + "## Methods ##\n\n" + Paragraphs("Methods", 40);
        int methodsAt = text.IndexOf("## Methods", StringComparison.Ordinal);

        List<ResourceChunkInput> chunks = IngestionService.BuildContentChunks("Report", text, firstPart: 1);

        Assert.Contains(chunks, c => SectionOf(c) == "Introduction");
        Assert.Contains(chunks, c => SectionOf(c) == "Methods");

        // A chunk's own text starts after the previous chunk's end; before that is overlap
        int previousEnd = 0;
        foreach (ResourceChunkInput chunk in chunks)
        {
            int ownTextFrom = Math.Max(chunk.StartChar!.Value, previousEnd);
            while (char.IsWhiteSpace(text[ownTextFrom])) ownTextFrom++;
            Assert.Equal(ownTextFrom < methodsAt ? "Introduction" : "Methods", SectionOf(chunk));
            previousEnd = chunk.EndChar!.Value;
        }
    }

    [Fact]
    public void FirstChunkOfASection_GetsThatSectionEvenWhenItOpensInTheOverlap()
    {
        string text = Paragraphs("Preface", 40) + "# Chapter one\n\n" + Paragraphs("Chapter", 10);
        int chapterAt = text.IndexOf("# Chapter one", StringComparison.Ordinal);

        List<ResourceChunkInput> chunks = IngestionService.BuildContentChunks("Book", text, firstPart: 1);
        ResourceChunkInput last = chunks[^1];

        Assert.Null(SectionOf(chunks[0]));
        Assert.True(last.StartChar < chapterAt, "the last chunk should open in the overlap before the heading, or this test proves nothing");
        Assert.Equal("Chapter one", SectionOf(last));
    }

    [Fact]
    public void LongTitle_IsTruncated()
    {
        string title = new('t', 500);

        ResourceChunkInput chunk = Assert.Single(IngestionService.BuildContentChunks(title, "Body.", firstPart: 1));
        string documentLine = chunk.Text.Split('\n')[0];

        Assert.True(documentLine.Length < 250, $"document line is {documentLine.Length} characters");
    }

    [Fact]
    public void HashtagWithoutSpace_IsNotAHeading()
    {
        ResourceChunkInput chunk = Assert.Single(IngestionService.BuildContentChunks("Post", "#participation is trending.\n\nBody.", firstPart: 1));

        Assert.Null(SectionOf(chunk));
    }
}
