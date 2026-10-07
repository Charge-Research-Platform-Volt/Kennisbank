using KnowledgeBank.Data;

namespace KnowledgeBank.Tests.Data;

public class FiletypeTests
{
    [Theory]
    [InlineData(".PDF", "pdf")]
    [InlineData(" docx ", "docx")]
    [InlineData("png", "png")]
    public void TrimExtension_NormalisesExtension(string input, string expected)
        => Assert.Equal(expected, Filetype.TrimExtension(input));

    [Theory]
    [InlineData(".pdf", "document")]
    [InlineData("JPG", "document")]
    [InlineData(".mp3", "audio")]
    [InlineData("mov", "video")]
    public void ConvertExtensionToFiletype_MapsToUploadType(string extension, string expected)
        => Assert.Equal(expected, Filetype.ConvertExtensionToFiletype(extension));

    [Fact]
    public void ConvertExtensionToFiletype_UnsupportedExtension_Throws()
        => Assert.Throws<KeyNotFoundException>(() => Filetype.ConvertExtensionToFiletype(".exe"));

    [Fact]
    public void SupportChecks_MatchTheirCategory()
    {
        Assert.True(Filetype.Supported(".pdf"));
        Assert.False(Filetype.Supported(".exe"));

        Assert.True(Filetype.SupportedDocument("docx"));
        Assert.False(Filetype.SupportedDocument("png"));

        Assert.True(Filetype.SupportedImage("heif"));
        Assert.True(Filetype.SupportedAudio("wav"));
        Assert.True(Filetype.SupportedVideo("mkv"));

        // "Text" means anything text can be extracted from: documents and images (via OCR)
        Assert.True(Filetype.SupportedText("png"));
        Assert.False(Filetype.SupportedText("mp3"));
    }

    [Theory]
    [InlineData(".pdf", "application/pdf")]
    [InlineData("XLSX", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(".exe", "application/octet-stream")]
    public void GetMimeType_FallsBackToOctetStream(string extension, string expected)
        => Assert.Equal(expected, Filetype.GetMimeType(extension));

    [Theory]
    [InlineData("https://example.org/reports/plan.pdf", true)]
    [InlineData("https://example.org/reports/plan.PDF?download=1", true)]
    [InlineData("https://example.org/scan.png", true)]
    [InlineData("https://example.org/page.html", false)]
    [InlineData("https://example.org/news/article", false)]
    [InlineData("https://example.org/podcast.mp3", false)]
    public void IsDocumentUrl_OnlyForDownloadableTextDocuments(string url, bool expected)
        => Assert.Equal(expected, Filetype.IsDocumentUrl(url));
}
