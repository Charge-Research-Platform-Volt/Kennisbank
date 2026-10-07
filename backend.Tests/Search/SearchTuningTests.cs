using KnowledgeBank.Services.Search;

namespace KnowledgeBank.Tests.Search;

public class SearchTuningTests
{
    [Theory]
    [InlineData("", 0.3)]
    [InlineData("subsidie", 0.3)]
    [InlineData("groene energie", 0.3)]
    [InlineData("groene energie nederland", 0.5)]
    [InlineData("een twee drie vier vijf zes", 0.5)]
    [InlineData("een twee drie vier vijf zes zeven", 0.7)]
    public void GetSemanticRatio_ScalesWithWordCount(string query, double expected)
        => Assert.Equal(expected, SearchTuning.GetSemanticRatio(query));

    [Fact]
    public void GetSemanticRatio_IgnoresExtraSpaces()
        => Assert.Equal(0.3, SearchTuning.GetSemanticRatio("  groene    energie  "));
}