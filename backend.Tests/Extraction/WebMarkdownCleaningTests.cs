using KnowledgeBank.Services;

namespace KnowledgeBank.Tests.Extraction;

public class WebMarkdownCleaningTests
{
    [Theory]
    [InlineData("Before\n\n![](https://images.cdn.nos.nl/2/a.webp)\n\nAfter", "Before\n\nAfter")]
    [InlineData("Before ![photo of a council](img.png) after", "Before  after")]
    public void Images_AreRemoved(string input, string expected)
        => Assert.Equal(expected, TextExtractionService.CleanWebMarkdown(input));

    [Theory]
    [InlineData("where Ukraine stores its [Flamingo missiles](https://nos.nl/artikel/2581057-nieuwe-fase) and drones", "where Ukraine stores its Flamingo missiles and drones")]
    [InlineData("see [Foo](https://en.wikipedia.org/wiki/Foo_(bar)) here", "see Foo here")]
    [InlineData("see [the report](https://example.org/report \"Annual report\") here", "see the report here")]
    [InlineData("[![](logo.png)](https://example.org)Home", "Home")]
    public void Links_KeepOnlyTheirText(string input, string expected)
        => Assert.Equal(expected, TextExtractionService.CleanWebMarkdown(input));

    [Fact]
    public void BlankLinesLeftBehind_Collapse()
        => Assert.Equal("A\n\nB", TextExtractionService.CleanWebMarkdown("A   \n\n![](x.png)\n\n![](y.png)\n\nB\n"));

    [Theory]
    [InlineData("As shown in [1], participation grew.")]
    [InlineData("## Heading\n\n*Italic caption:*\n\n- a list item")]
    public void TextWithoutImagesOrLinks_IsUnchanged(string input)
        => Assert.Equal(input, TextExtractionService.CleanWebMarkdown(input));

    [Fact]
    public void CleaningTwice_ChangesNothingMore()
    {
        string once = TextExtractionService.CleanWebMarkdown("Intro\n\n![](a.webp)\n\nRead [more](https://x.nl/a) here.  \n\n\n\nEnd");

        Assert.Equal(once, TextExtractionService.CleanWebMarkdown(once));
    }
}
