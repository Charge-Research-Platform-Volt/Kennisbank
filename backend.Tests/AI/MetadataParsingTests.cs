using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using static KnowledgeBank.Services.AI.MetadataExtractionService;

namespace KnowledgeBank.Tests.AI;

public class MetadataParsingTests
{
    // ParsePublicationDate

    [Fact]
    public void ParsePublicationDate_YearOnly()
        => Assert.Equal((new DateTime(2024, 1, 1), PublicationDatePrecision.Year), ParsePublicationDate("2024"));

    [Fact]
    public void ParsePublicationDate_YearAndMonth()
        => Assert.Equal((new DateTime(2024, 3, 1), PublicationDatePrecision.Month), ParsePublicationDate("2024-03"));

    [Fact]
    public void ParsePublicationDate_FullDate()
        => Assert.Equal((new DateTime(2024, 3, 15), PublicationDatePrecision.Day), ParsePublicationDate(" 2024-03-15 "));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2024-13")]
    [InlineData("2024-02-30")]
    [InlineData("15-03-2024")]
    [InlineData("March 2024")]
    [InlineData("24")]
    [InlineData("2024-03-15-01")]
    public void ParsePublicationDate_InvalidInput_ReturnsNothing(string? raw)
        => Assert.Equal((null, null), ParsePublicationDate(raw));

    // IsInitialNameMatch

    [Theory]
    [InlineData("J. Smith", "John Smith")]
    [InlineData("John Smith", "J Smith")]
    [InlineData("john smith", "J. SMITH")]
    [InlineData("A. B. Jansen", "Anna Bea Jansen")]
    public void IsInitialNameMatch_InitialsMatchFullNames(string a, string b)
        => Assert.True(IsInitialNameMatch(a, b));

    [Theory]
    [InlineData("John Smith", "John Smith")]   // identical names aren't an *initial* match
    [InlineData("J. Smith", "John Smyth")]     // surname differs
    [InlineData("K. Smith", "John Smith")]     // initial differs
    [InlineData("J. A. Smith", "John Smith")]  // different number of parts
    [InlineData("Smith", "S")]                 // single-part names never match
    public void IsInitialNameMatch_RejectsNonMatches(string a, string b)
        => Assert.False(IsInitialNameMatch(a, b));

    // GroundAndFilter

    [Fact]
    public void GroundAndFilter_KeepsOnlyRealEntitiesFoundInTheText()
    {
        const string source = "This report by TNO and Eneco cites Jan de Vries and others.";
        TempEntity[] entities =
        [
            Entity("TNO"),
            Entity("Eneco"),
            Entity("eneco"),                 // duplicate, different case
            Entity("Jan de Vries"),          // already an author
            Entity("Shell"),                 // not in the source text
            Entity("AB"),                    // too short
            Entity("Smith et al."),
            Entity("Unknown author"),
            Entity("Anonymous"),
            Entity("  "),
        ];

        List<TempEntity> result = GroundAndFilter(entities, source, ["jan de vries"]);

        Assert.Equal(["TNO", "Eneco"], result.Select(e => e.Name));
    }

    // TrimText

    [Fact]
    public void TrimText_ShortText_IsUnchanged()
        => Assert.Equal("short text", TrimText("short text", firstChars: 5, lastChars: 5));

    [Fact]
    public void TrimText_WithoutEnding_KeepsBeginningAndMarksRestOmitted()
        => Assert.Equal("abcde\n\n[...rest of document omitted...]", TrimText("abcdefghijklmnop", firstChars: 5, lastChars: 0));

    [Fact]
    public void TrimText_WithEnding_KeepsBothEndsAndMarksMiddleOmitted()
        => Assert.Equal("abc\n\n[...middle section omitted...]\n\nnop", TrimText("abcdefghijklmnop", firstChars: 3, lastChars: 3));

    // ChunkText

    [Fact]
    public void ChunkText_SplitsWithOverlap()
        => Assert.Equal(["abcd", "defg", "ghij"], ChunkText("abcdefghij", chunkSize: 4, overlap: 1));

    [Fact]
    public void ChunkText_ShortText_IsOneChunk()
        => Assert.Equal(["abc"], ChunkText("abc", chunkSize: 10, overlap: 2));

    [Fact]
    public void ChunkText_EmptyText_HasNoChunks()
        => Assert.Empty(ChunkText(""));

    // ExtractPublicationCode

    [Theory]
    [InlineData("Available at https://doi.org/10.1016/j.enpol.2023.113456.", "DOI: 10.1016/j.enpol.2023.113456")]
    [InlineData("(doi: 10.1234/abc-def)", "DOI: 10.1234/abc-def")]
    [InlineData("Preprint arXiv: 2301.12345 [cs.CL]", "arXiv: 2301.12345")]
    [InlineData("PMID: 31452104", "PMID: 31452104")]
    [InlineData("ISSN 0301-421X", "ISSN: 0301-421X")]
    [InlineData("ISBN 978-0-306-40615-7", "ISBN: 978-0-306-40615-7")]
    [InlineData("ISBN 0306406152", "ISBN: 0306406152")]
    public void ExtractPublicationCode_RecognisesIdentifiers(string text, string expected)
        => Assert.Equal(expected, ExtractPublicationCode(text));

    [Fact]
    public void ExtractPublicationCode_PrefersDoiOverOtherIdentifiers()
        => Assert.Equal("DOI: 10.1234/xyz", ExtractPublicationCode("ISBN 978-0-306-40615-7, doi 10.1234/xyz"));

    [Fact]
    public void ExtractPublicationCode_NoIdentifier_ReturnsNull()
        => Assert.Null(ExtractPublicationCode("Just a regular paragraph about heat pumps in 2024."));

    private static TempEntity Entity(string name) => new() { Name = name, Type = "organisation" };
}
