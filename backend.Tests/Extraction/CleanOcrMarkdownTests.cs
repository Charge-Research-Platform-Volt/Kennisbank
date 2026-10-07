using KnowledgeBank.Services;

namespace KnowledgeBank.Tests.Extraction;

public class CleanOcrMarkdownTests
{
    [Fact]
    public void RemovesImageReferences()
        => Assert.Equal("Intro\n\nBody", TextExtractionService.CleanOcrMarkdown("Intro\n![img-0.jpeg](img-0.jpeg)\nBody"));

    [Fact]
    public void RemovesEmptyHeadings_ButKeepsRealOnes()
        => Assert.Equal("\n## Results\nText", TextExtractionService.CleanOcrMarkdown("##\n## Results\nText"));

    [Fact]
    public void RemovesEmptyFootnoteArtifacts()
        => Assert.Equal("Energy use fell.", TextExtractionService.CleanOcrMarkdown("Energy use fell.^{}[]"));

    [Fact]
    public void DecodesHtmlEntities()
        => Assert.Equal("Fish & chips < 5 euro", TextExtractionService.CleanOcrMarkdown("Fish &amp; chips &lt; 5 euro"));
}
