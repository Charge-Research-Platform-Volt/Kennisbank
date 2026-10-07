using KnowledgeBank.Models;
using KnowledgeBank.Services.Search;

namespace KnowledgeBank.Tests.Search;

public class LibraryFilterExpressionTests
{
    private const string Tag1 = "3f2b1c4e-9a7d-4b2e-8f1a-0c9d8e7f6a5b";
    private const string Tag2 = "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d";

    private static string? Build(LibraryFilterOptions? options) => LibrarySearchIndexService.BuildFilterExpressions(options);

    [Fact]
    public void NullOrEmptyOptions_ProduceNoFilter()
    {
        Assert.Null(Build(null));
        Assert.Null(Build(new LibraryFilterOptions()));
        Assert.Null(Build(new LibraryFilterOptions { TagFilter = [], RegionFilter = [], TypeFilter = [] }));
    }

    [Fact]
    public void TypeFilter_BecomesInClause()
        => Assert.Equal("type IN [\"resource\", \"person\"]", Build(new LibraryFilterOptions { TypeFilter = ["resource", "person"] }));

    [Fact]
    public void PublicationDateBounds_KeepItemsWithoutADate()
    {
        string? filter = Build(new LibraryFilterOptions { PubdateMin = "2024-01-01", PubdateMax = "2024-12-31" });

        Assert.Equal(
            "(publicationDateTimestamp IS NULL OR publicationDateTimestamp >= 1704067200) AND " +
            "(publicationDateTimestamp IS NULL OR publicationDateTimestamp <= 1735603200)",
            filter);
    }

    [Fact]
    public void UnparseableDate_IsIgnored()
        => Assert.Null(Build(new LibraryFilterOptions { PubdateMin = "not a date" }));

    [Fact]
    public void TagFilter_AnyMode_MatchesAnyTag_AndOnlyRestrictsResources()
        => Assert.Equal(
            $"(type != \"resource\" OR tagIds IN [\"{Tag1}\", \"{Tag2}\"])",
            Build(new LibraryFilterOptions { TagFilter = [Tag1, Tag2], TagFilterMode = "any" }));

    [Fact]
    public void TagFilter_AllMode_RequiresEveryTag()
        => Assert.Equal(
            $"(type != \"resource\" OR tagIds = \"{Tag1}\" AND tagIds = \"{Tag2}\")",
            Build(new LibraryFilterOptions { TagFilter = [Tag1, Tag2], TagFilterMode = "ALL" }));

    [Fact]
    public void RegionFilter_UsesRegionIds()
        => Assert.Equal(
            $"(type != \"resource\" OR regionIds IN [\"{Tag1}\"])",
            Build(new LibraryFilterOptions { RegionFilter = [Tag1] }));

    [Fact]
    public void ResourceTypeAndJournalFilters_OnlyRestrictResources()
    {
        Assert.Equal(
            $"(type != \"resource\" OR typeId IN [\"{Tag1}\"])",
            Build(new LibraryFilterOptions { ResourceTypeFilter = [Tag1] }));

        Assert.Equal(
            $"(type != \"resource\" OR journalId IN [\"{Tag1}\"])",
            Build(new LibraryFilterOptions { JournalFilter = [Tag1] }));
    }

    [Fact]
    public void MultipleFilters_AreJoinedWithAnd()
        => Assert.Equal(
            $"type IN [\"resource\"] AND (type != \"resource\" OR tagIds IN [\"{Tag1}\"])",
            Build(new LibraryFilterOptions { TypeFilter = ["resource"], TagFilter = [Tag1] }));

    // Filter values come straight from the request, so anything that isn't a GUID (or a known type)
    // must never reach the Meilisearch filter string.

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("x\"] OR type = \"person")]
    public void InvalidIds_AreDropped(string invalid)
    {
        LibraryFilterOptions options = new()
        {
            TagFilter = [invalid],
            RegionFilter = [invalid],
            ResourceTypeFilter = [invalid],
            JournalFilter = [invalid]
        };

        Assert.Null(Build(options));
    }

    [Fact]
    public void InvalidIds_AreDropped_ValidOnesKept()
        => Assert.Equal(
            $"(type != \"resource\" OR tagIds IN [\"{Tag1}\"])",
            Build(new LibraryFilterOptions { TagFilter = ["garbage", Tag1] }));

    [Theory]
    [InlineData("banana")]
    [InlineData("resource\"] OR type IN [\"person")]
    public void UnknownTypes_AreDropped(string invalid)
        => Assert.Equal(
            "type IN [\"organisation\"]",
            Build(new LibraryFilterOptions { TypeFilter = [invalid, "organisation"] }));
}
