using KnowledgeBank.Models;
using KnowledgeBank.Services;
using KnowledgeBank.Tests.Helpers;
using static KnowledgeBank.Tests.Helpers.TestDocuments;

namespace KnowledgeBank.Tests.Extraction;

public class PptxExtractionTests
{
    private static OcrResult Extract(params Slide[] slides) => TextExtractionService.ExtractPptx(new MemoryStream(Pptx(slides)));

    [Fact]
    public void EachSlide_IsASectionWithItsTitle()
    {
        OcrResult result = Extract(
            new Slide(Shape("title", "Budget 2026") + Shape("body", "Income rises", "Costs fall")),
            new Slide(Shape("ctrTitle", "Questions?")));

        Assert.Equal("## Slide 1: Budget 2026\n\nIncome rises\nCosts fall\n\n## Slide 2: Questions?\n\n", result.Text);
    }

    [Fact]
    public void RunsOfOneParagraph_StayOnOneLine()
    {
        string shape = """<p:sp><p:nvSpPr><p:cNvPr id="2" name="s"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr/><p:txBody><a:bodyPr/><a:p><a:r><a:t>A </a:t></a:r><a:r><a:rPr b="1"/><a:t>bold</a:t></a:r><a:r><a:t> word</a:t></a:r><a:br/><a:r><a:t>next line</a:t></a:r></a:p></p:txBody></p:sp>""";

        Assert.Contains("A bold word\nnext line", Extract(new Slide(shape)).Text);
    }

    [Fact]
    public void PageStarts_AreWhereEachSlideStarts()
    {
        OcrResult result = Extract(new Slide(Shape("title", "One")), new Slide(Shape("title", "Two")), new Slide(Shape("title", "Three")));

        Assert.Equal(3, result.PageStarts!.Length);
        Assert.Equal(2, ResourceText.PageAt(result.PageStarts, result.Text.IndexOf("Two", StringComparison.Ordinal)));
        Assert.All(result.PageStarts.Select((start, i) => (start, i)), s => Assert.StartsWith($"## Slide {s.i + 1}", result.Text[s.start..]));
    }

    [Fact]
    public void SlideWithoutText_KeepsTheNumbersAligned()
    {
        OcrResult result = Extract(new Slide(Shape("title", "One")), new Slide(""), new Slide(Shape("title", "Three")));

        Assert.DoesNotContain("Slide 2", result.Text);
        Assert.Contains("## Slide 3: Three", result.Text);
        Assert.Equal(3, ResourceText.PageAt(result.PageStarts, result.Text.IndexOf("Three", StringComparison.Ordinal)));
    }

    [Fact]
    public void SlideWithoutTitle_IsStillASection()
        => Assert.StartsWith("## Slide 1\n\nJust text", Extract(new Slide(Shape(null, "Just text"))).Text);

    [Fact]
    public void SlideNumberFooterAndDate_AreLeftOut()
    {
        string text = Extract(new Slide(Shape("title", "T") + Shape("sldNum", "4") + Shape("ftr", "Confidential") + Shape("dt", "27-11-2026"))).Text;

        Assert.Equal("## Slide 1: T\n\n", text);
    }

    [Fact]
    public void Tables_BecomeRowsOfCells()
        => Assert.Contains("Quarter | Spend\nQ1 | 10k", Extract(new Slide(Shape("title", "Costs") + Table(["Quarter", "Spend"], ["Q1", "10k"]))).Text);

    [Fact]
    public void SpeakerNotes_AreIncludedWithoutTheNotesPageNumber()
    {
        string text = Extract(new Slide(Shape("title", "Plan"), Notes: "Mention the delay")).Text;

        Assert.Contains("Speaker notes:\nMention the delay", text);
        Assert.DoesNotContain("7", text);
    }

    [Fact]
    public void DeckWithoutAnyText_GivesNothing()
    {
        OcrResult result = Extract(new Slide(""), new Slide(Shape("sldNum", "2")));

        Assert.Equal("", result.Text);
        Assert.Null(result.PageStarts);
    }

    [Fact]
    public void SmartArt_IsReadFromItsDiagramPart()
    {
        string text = Extract(new Slide(Shape("title", "Process"), SmartArt: ["Plan", "Build", "Measure"])).Text;

        Assert.Equal("## Slide 1: Process\n\nPlan\nBuild\nMeasure\n\n", text);
    }
}

