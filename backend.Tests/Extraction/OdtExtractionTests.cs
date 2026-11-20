using System.IO.Compression;
using KnowledgeBank.Services;
using KnowledgeBank.Tests.Helpers;

namespace KnowledgeBank.Tests.Extraction;

public class OdtExtractionTests
{
    [Fact]
    public void ReadsHeadingsAndParagraphs_OnePerLine()
    {
        string text = Extract("""
            <text:h text:outline-level="1">Minutes steering group</text:h>
            <text:p>Present: Anna, Bram</text:p>
            <text:p>Decision: <text:span text:style-name="Bold">go</text:span> for phase two.</text:p>
            """);

        Assert.Equal(["Minutes steering group", "Present: Anna, Bram", "Decision: go for phase two."], Lines(text));
    }

    [Fact]
    public void KeepsOdfWhitespaceElements_AndCollapsesPlainWhitespace()
    {
        string text = Extract("""
            <text:p>a<text:s text:c="3"/>b<text:tab/>c<text:line-break/>d</text:p>
            <text:p>loose
                wrapped    text</text:p>
            """);

        Assert.Equal("a   b\tc\nd", Lines(text)[0] + "\n" + Lines(text)[1]);
        Assert.Equal("loose wrapped text", Lines(text)[2]);
    }

    [Fact]
    public void ReadsFootnotesOnce_AsTheirOwnLine()
    {
        string text = Extract("""
            <text:p>Budget overrun<text:note text:note-class="footnote"><text:note-body><text:p>See appendix B</text:p></text:note-body></text:note> in Q3.</text:p>
            """);

        Assert.Equal(["Budget overrun in Q3.", "See appendix B"], Lines(text));
    }

    [Fact]
    public void ReadsTableCells()
    {
        string text = Extract("""
            <table:table>
              <table:table-row>
                <table:table-cell><text:p>Action</text:p></table:table-cell>
                <table:table-cell><text:p>Owner</text:p></table:table-cell>
              </table:table-row>
            </table:table>
            """);

        Assert.Equal(["Action", "Owner"], Lines(text));
    }

    [Fact]
    public void SkipsEmptyParagraphs()
        => Assert.Equal(["Text"], Lines(Extract("<text:p/><text:p>  </text:p><text:p>Text</text:p>")));

    [Fact]
    public void WithoutContent_ReturnsNothing()
    {
        using MemoryStream stream = new();
        using (ZipArchive zip = new(stream, ZipArchiveMode.Create, leaveOpen: true))
            zip.CreateEntry("mimetype");

        Assert.Equal(string.Empty, TextExtractionService.ExtractOdt(stream));
    }

    [Fact]
    public void NotAZip_Throws()
        => Assert.ThrowsAny<InvalidDataException>(() => TextExtractionService.ExtractOdt(new MemoryStream("not an odt"u8.ToArray())));

    private static string Extract(string body) => TextExtractionService.ExtractOdt(new MemoryStream(TestDocuments.Odt(body)));

    private static string[] Lines(string text) => text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
}
