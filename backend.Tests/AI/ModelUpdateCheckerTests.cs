using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Tests.AI;

public class ModelUpdateCheckerTests
{
    private static MistralModelCard Card(string id, params string[] aliases) => new(id, aliases, null, null);

    // Mirrors Mistral's real shape: every alias is also listed as its own card
    private static readonly List<MistralModelCard> Cards =
    [
        Card("mistral-small-2603", "mistral-small-latest", "magistral-small-latest"),
        Card("mistral-small-latest", "mistral-small-2603", "magistral-small-latest"),
        Card("mistral-large-4", "mistral-large-4-0"),
        Card("mistral-large-4-0", "mistral-large-4"),
        Card("mistral-large-2512", "mistral-large-latest"),
        Card("mistral-large-latest", "mistral-large-2512"),
        Card("mistral-ocr-latest", "mistral-ocr-4", "mistral-ocr-4-1"),
        Card("mistral-ocr-4", "mistral-ocr-latest", "mistral-ocr-4-1"),
        Card("mistral-ocr-4-1", "mistral-ocr-latest", "mistral-ocr-4"),
    ];

    private static readonly HashSet<string> AllSeen = Cards.Select(c => c.Id).ToHashSet();

    [Theory]
    [InlineData("mistral-small", "mistral-small-2603", ModelUpdateStatus.UpToDate)]
    [InlineData("mistral-ocr", "mistral-ocr-4-1", ModelUpdateStatus.UpToDate)]
    [InlineData("mistral-small", "mistral-small-2509", ModelUpdateStatus.Missing)]
    [InlineData("mistral-small", "mistral-small-latest", ModelUpdateStatus.NotPinned)]
    [InlineData("mistral-large", "mistral-large-4-0", ModelUpdateStatus.UpdateAvailable)]
    public void Status_ComparesPinnedModelToLatestAlias(string prefix, string configured, ModelUpdateStatus expected)
        => Assert.Equal(expected, ModelUpdateChecker.Check(new("x", prefix, configured), Cards, AllSeen).Status);

    [Fact]
    public void Status_IsUpdateAvailable_WhenLatestMovedPastPinnedModel()
    {
        List<MistralModelCard> cards = [.. Cards, Card("mistral-small-2509")];

        Assert.Equal(ModelUpdateStatus.UpdateAvailable, ModelUpdateChecker.Check(new("Small", "mistral-small", "mistral-small-2509"), cards, AllSeen).Status);
    }

    [Fact]
    public void Status_IsWatchOnly_WithoutPinnedModel()
        => Assert.Equal(ModelUpdateStatus.WatchOnly, ModelUpdateChecker.Check(new("Large", "mistral-large", null), Cards, AllSeen).Status);

    [Fact]
    public void Latest_PrefersDatedId_ThenMostSpecificName()
    {
        Assert.Equal("mistral-small-2603", ModelUpdateChecker.Check(new("Small", "mistral-small", null), Cards, AllSeen).Latest);
        Assert.Equal("mistral-ocr-4-1", ModelUpdateChecker.Check(new("OCR", "mistral-ocr", null), Cards, AllSeen).Latest);
    }

    [Fact]
    public void Models_CollapseAliasCards_AndSkipLatestAliases()
    {
        ModelFamilyStatus large = ModelUpdateChecker.Check(new("Large", "mistral-large", "mistral-large-2512", UsedByApp: false), Cards, AllSeen);

        Assert.Equal(2, large.Models.Count);
        Assert.Contains(large.Models, m => m.Names.SequenceEqual(["mistral-large-4", "mistral-large-4-0"]) && !m.IsLatest && !m.IsConfigured);
        Assert.Contains(large.Models, m => m.Names.SequenceEqual(["mistral-large-2512"]) && m.IsLatest && m.IsConfigured);
        Assert.False(large.UsedByApp);
    }

    [Fact]
    public void Model_IsNew_OnlyWhenNoneOfItsNamesWereSeen()
    {
        HashSet<string> seen = [.. AllSeen.Where(id => !id.StartsWith("mistral-large-4"))];

        Assert.True(ModelUpdateChecker.Check(new("Large", "mistral-large", null), Cards, seen).Models.Single(m => m.Names.Contains("mistral-large-4")).IsNew);

        seen.Remove("mistral-ocr-4"); // a new alias on an existing model doesn't make it new
        Assert.False(ModelUpdateChecker.Check(new("OCR", "mistral-ocr", null), Cards, seen).Models.Single().IsNew);
    }

    [Fact]
    public void Deprecation_ComesFromPinnedModel()
    {
        DateTime retiredOn = new(2027, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        List<MistralModelCard> cards = [.. Cards.Where(c => c.Id != "mistral-small-2603"), new("mistral-small-2603", ["mistral-small-latest"], retiredOn, "mistral-small-2609")];

        ModelFamilyStatus small = ModelUpdateChecker.Check(new("Small", "mistral-small", "mistral-small-2603"), cards, AllSeen);

        Assert.Equal(retiredOn, small.Deprecation);
        Assert.Equal("mistral-small-2609", small.DeprecationReplacement);
    }
}
