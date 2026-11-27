using KnowledgeBank.Models;
using KnowledgeBank.Services;

namespace KnowledgeBank.Tests.Extraction;

public class OcrPagesTests
{
    [Fact]
    public void BuildOcrResult_JoinsPagesAndRecordsWhereEachStarts()
    {
        OcrResult result = TextExtractionService.BuildOcrResult(["Page one", "Page two", "Page three"], "", "mistral-ocr-test");

        Assert.Equal("Page one\nPage two\nPage three\n", result.Text);
        Assert.Equal([0, 9, 18], result.PageStarts!);
        Assert.All(result.PageStarts!.Zip(["Page one", "Page two", "Page three"]), p => Assert.StartsWith(p.Second, result.Text[p.First..]));
    }

    [Fact]
    public void BuildOcrResult_CleansEachPageButKeepsThePagesRaw()
    {
        List<string> pages = ["Intro &amp; aims\n![img-0.jpeg](img-0.jpeg)", "Body"];

        OcrResult result = TextExtractionService.BuildOcrResult(pages, "", "mistral-ocr-test");

        Assert.DoesNotContain("![img-0.jpeg]", result.Text);
        Assert.Contains("Intro & aims", result.Text);
        Assert.Same(pages, result.Pages);
    }

    [Fact]
    public void BuildOcrResult_PageWithoutTextKeepsPagesAligned()
    {
        OcrResult result = TextExtractionService.BuildOcrResult(["First", "![img-0.jpeg](img-0.jpeg)", "Third"], "", "mistral-ocr-test");

        Assert.Equal("First\nThird\n", result.Text);
        Assert.Equal([0, 6, 6], result.PageStarts!);
        Assert.Equal(3, ResourceText.PageAt(result.PageStarts, result.Text.IndexOf("Third", StringComparison.Ordinal)));
    }

    [Fact]
    public void BuildOcrResult_KeepsHeaderFooterAndModel()
    {
        OcrResult result = TextExtractionService.BuildOcrResult(["Text"], "Header line", "mistral-ocr-2512");

        Assert.Equal("Header line", result.HeaderFooterText);
        Assert.Equal("mistral-ocr-2512", result.Model);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(8, 1)]
    [InlineData(9, 2)]
    [InlineData(17, 2)]
    [InlineData(18, 3)]
    [InlineData(10_000, 3)]
    public void PageAt_FindsThePageHoldingThePosition(int position, int expectedPage)
        => Assert.Equal(expectedPage, ResourceText.PageAt([0, 9, 18], position));

    [Fact]
    public void PageAt_WithoutPages_IsNull()
    {
        Assert.Null(ResourceText.PageAt(null, 5));
        Assert.Null(ResourceText.PageAt([], 5));
    }

    [Fact]
    public void PageAt_InstanceUsesItsPageStarts()
    {
        ResourceText text = new() { ResourceId = Guid.NewGuid(), Text = "Page one\nPage two\n", PageStarts = [0, 9], ExtractedAt = DateTime.UtcNow };

        Assert.Equal(2, text.PageAt(12));
    }
}
