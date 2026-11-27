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
        MatchedChunks = [.. chunks],
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
        LibraryItemWithChunks a = Hit("A", 0.9f, Filler + "a1", Filler + "a2");
        LibraryItemWithChunks b = Hit("B", 0.8f, Filler + "b1", Filler + "b2");

        string result = ChatToolExecutor.FormatSearchResults([a, b], tokenBudget: 250);

        Assert.Contains("a1", result);
        Assert.Contains("b1", result);
        Assert.DoesNotContain("a2", result);
        Assert.DoesNotContain("b2", result);
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
}
