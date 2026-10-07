using KnowledgeBank.Models;
using KnowledgeBank.Services.Domain;

namespace KnowledgeBank.Tests.Domain;

public class LibraryQueryHelpersTests
{
    private static readonly Guid IdA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid IdB = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid IdC = Guid.Parse("00000000-0000-0000-0000-00000000000c");

    private static readonly LibraryItem[] Items =
    [
        new() { Id = IdB, Name = "Beta", Type = "resource", PublicationDate = new DateTime(2022, 1, 1), CreatedOn = new DateTime(2026, 1, 3) },
        new() { Id = IdA, Name = "Alpha", Type = "person", PublicationDate = new DateTime(2024, 1, 1), CreatedOn = new DateTime(2026, 1, 1) },
        new() { Id = IdC, Name = "Gamma", Type = "organisation", PublicationDate = new DateTime(2023, 1, 1), CreatedOn = new DateTime(2026, 1, 2) },
    ];

    // ParseGuids

    [Fact]
    public void ParseGuids_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Empty(LibraryService.ParseGuids(null));
        Assert.Empty(LibraryService.ParseGuids([]));
    }

    [Fact]
    public void ParseGuids_SkipsInvalidValues()
        => Assert.Equal([IdA, IdB], LibraryService.ParseGuids([IdA.ToString(), "not-a-guid", "", IdB.ToString()]));

    // ApplySorting

    [Theory]
    [InlineData("name", "asc", new[] { "Alpha", "Beta", "Gamma" })]
    [InlineData("NAME", "DESC", new[] { "Gamma", "Beta", "Alpha" })]
    [InlineData("type", "asc", new[] { "Gamma", "Alpha", "Beta" })]
    [InlineData("publicationDate", "asc", new[] { "Beta", "Gamma", "Alpha" })]
    [InlineData("publicationDate", "desc", new[] { "Alpha", "Gamma", "Beta" })]
    [InlineData("createdOn", "asc", new[] { "Alpha", "Gamma", "Beta" })]
    [InlineData(null, null, new[] { "Beta", "Gamma", "Alpha" })]          // default: newest first
    [InlineData("unknown", "asc", new[] { "Beta", "Gamma", "Alpha" })]
    public void ApplySorting_SortsByRequestedField(string? sortBy, string? direction, string[] expectedNames)
        => Assert.Equal(expectedNames, LibraryService.ApplySorting(Items.AsQueryable(), sortBy, direction).Select(i => i.Name));

    [Fact]
    public void ApplySorting_EqualValues_AreOrderedByIdForStablePaging()
    {
        LibraryItem[] sameName =
        [
            new() { Id = IdC, Name = "Same" },
            new() { Id = IdA, Name = "Same" },
            new() { Id = IdB, Name = "Same" },
        ];

        Assert.Equal([IdA, IdB, IdC], LibraryService.ApplySorting(sameName.AsQueryable(), "name", "asc").Select(i => i.Id));
    }

    // MergeSuggestionService.Normalize

    [Fact]
    public void NormalizePair_IsIndependentOfOrder()
    {
        Assert.Equal((IdA, IdB), MergeSuggestionService.Normalize(IdA, IdB));
        Assert.Equal((IdA, IdB), MergeSuggestionService.Normalize(IdB, IdA));
    }
}
