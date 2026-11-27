using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Tests.AI;

public class SearchResultFormattingTests
{
    // ~100 estimated tokens at 3 characters per token
    private static readonly string Filler = new('x', 300);

    private static LibraryItemWithChunks Hit(string name, float score, params string[] chunks) => new()
    {
        Item = new LibraryItem { Id = Guid.NewGuid(), Name = name, Type = "resource", Description = $"{name} description" },
        MatchedChunks = [.. chunks.Select(c => new MatchedChunk(c))],
        Score = score
    };

    [Fact]
    public void NoHitAboveThreshold_ReturnsNoResults()
        => Assert.Equal("No relevant results found", ChatToolExecutor.FormatSearchResults([Hit("A", 0.1f, "text")]));

    [Fact]
    public void HitsBelowThreshold_AreLeftOut()
    {
        LibraryItemWithChunks relevant = Hit("A", 0.8f, "kept passage");
        LibraryItemWithChunks irrelevant = Hit("B", 0.1f, "dropped passage");

        string result = ChatToolExecutor.FormatSearchResults([relevant, irrelevant]);

        Assert.Contains("kept passage", result);
        Assert.DoesNotContain("dropped passage", result);
        Assert.DoesNotContain(irrelevant.Item.Id.ToString(), result);
    }

    [Fact]
    public void Passages_AreVerbatimUnderCitationLine()
    {
        LibraryItemWithChunks hit = Hit("A", 0.8f, "first passage", "second passage");

        string result = ChatToolExecutor.FormatSearchResults([hit]);

        Assert.Contains($"Cite as: [SRC:{hit.Item.Id}]", result);
        Assert.Contains("first passage\n---\nsecond passage", result);
    }

    [Fact]
    public void Hits_KeepTheirRankingOrder()
    {
        LibraryItemWithChunks first = Hit("First", 0.9f, "p1");
        LibraryItemWithChunks second = Hit("Second", 0.8f, "p2");

        string result = ChatToolExecutor.FormatSearchResults([first, second]);

        Assert.True(result.IndexOf("First (resource)", StringComparison.Ordinal) < result.IndexOf("Second (resource)", StringComparison.Ordinal));
    }

    [Fact]
    public void Budget_GivesEveryHitItsBestPassageBeforeAnySecond()
    {
        // Markers that can't occur in the GUIDs of the result
        LibraryItemWithChunks a = Hit("A", 0.9f, Filler + "<A1>", Filler + "<A2>");
        LibraryItemWithChunks b = Hit("B", 0.8f, Filler + "<B1>", Filler + "<B2>");

        string result = ChatToolExecutor.FormatSearchResults([a, b], tokenBudget: 250);

        Assert.Contains("<A1>", result);
        Assert.Contains("<B1>", result);
        Assert.DoesNotContain("<A2>", result);
        Assert.DoesNotContain("<B2>", result);
    }

    [Fact]
    public void HitsWithoutRoom_AreListedByIdWithoutCitation()
    {
        LibraryItemWithChunks a = Hit("A", 0.9f, Filler);
        LibraryItemWithChunks b = Hit("B", 0.8f, Filler);

        string result = ChatToolExecutor.FormatSearchResults([a, b], tokenBudget: 150);

        Assert.Contains($"Cite as: [SRC:{a.Item.Id}]", result);
        Assert.Contains("More matches", result);
        Assert.Contains($"id: {b.Item.Id}", result);
        Assert.DoesNotContain($"[SRC:{b.Item.Id}]", result);
    }

    [Fact]
    public void NoMoreMatchesSection_WhenEverythingFits()
        => Assert.DoesNotContain("More matches", ChatToolExecutor.FormatSearchResults([Hit("A", 0.9f, "short")]));

    [Fact]
    public void HitWithoutChunks_FallsBackToDescription()
    {
        string result = ChatToolExecutor.FormatSearchResults([Hit("Person X", 0.7f)]);

        Assert.Contains("Description: Person X description", result);
        Assert.DoesNotContain("Passages:", result);
    }

    [Fact]
    public void PositionedChunks_ShowTheirSliceOfTheStoredText()
    {
        string body = "Opening paragraph.\n\nThe matched paragraph.\n\nClosing paragraph.";
        int start = body.IndexOf("The matched", StringComparison.Ordinal);
        LibraryItemWithChunks hit = Hit("A", 0.8f);
        hit.MatchedChunks = [new MatchedChunk("Document: A\n\nThe matched paragraph.", start, start + "The matched paragraph.".Length)];
        ResourceText text = new() { ResourceId = hit.Item.Id, Text = body, PageStarts = [0], ExtractedAt = DateTime.UtcNow };

        string result = ChatToolExecutor.FormatSearchResults([hit], new Dictionary<Guid, ResourceText> { [hit.Item.Id] = text });

        Assert.Contains($"Passages:\n(p. 1)\n{body}", result);
        Assert.DoesNotContain("Document: A", result);
    }

    [Fact]
    public void HitsWithoutStoredText_ShowTheirChunks()
    {
        LibraryItemWithChunks hit = Hit("A", 0.8f);
        hit.MatchedChunks = [new MatchedChunk("chunk with a position", 0, 21)];

        string result = ChatToolExecutor.FormatSearchResults([hit], new Dictionary<Guid, ResourceText>());

        Assert.Contains("Passages:\nchunk with a position", result);
    }
}
