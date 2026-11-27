using System.IO.Compression;
using KnowledgeBank.Services;
using KnowledgeBank.Tests.Helpers;

namespace KnowledgeBank.Tests.Extraction;

public class OdtExtractionTests
{
    [Fact]
    public void ReadsHeadingsAsMarkdown_AndParagraphsAsBlocks()
    {
        string text = Extract("""
            <text:h text:outline-level="1">Minutes steering group</text:h>
            <text:p>Present: Anna, Bram</text:p>
            <text:p>Decision: <text:span text:style-name="Bold">go</text:span> for phase two.</text:p>
            """);

        Assert.Equal("# Minutes steering group\n\nPresent: Anna, Bram\n\nDecision: go for phase two.", text);
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
    public void ReadsFootnotesOnce_AsMarkerAndNoteAtTheEnd()
    {
        string text = Extract("""
            <text:p>Budget overrun<text:note text:note-class="footnote"><text:note-citation>1</text:note-citation><text:note-body><text:p>See appendix B</text:p></text:note-body></text:note> in Q3.</text:p>
            <text:p>Next paragraph.</text:p>
            """);

        Assert.Equal("Budget overrun[^1] in Q3.\n\nNext paragraph.\n\n## Notes\n\n[^1]: See appendix B", text);
    }

    [Fact]
    public void FootnoteWithoutCitation_IsNumberedInOrder()
    {
        string text = Extract("""
            <text:p>A<text:note><text:note-body><text:p>first</text:p></text:note-body></text:note> and B<text:note><text:note-body><text:p>second</text:p></text:note-body></text:note>.</text:p>
            """);

        Assert.Contains("A[^1] and B[^2].", text);
        Assert.EndsWith("[^1]: first\n[^2]: second", text);
    }

    [Fact]
    public void HeadingLevels_FollowTheOutlineLevel()
    {
        string text = Extract("""
            <text:h text:outline-level="2">Methods</text:h>
            <text:h text:outline-level="9">Deep</text:h>
            <text:h>No level</text:h>
            """);

        Assert.Equal("## Methods\n\n###### Deep\n\n# No level", text);
    }

    [Fact]
    public void ListItems_GetADash()
    {
        string text = Extract("""
            <text:list><text:list-item><text:p>first</text:p></text:list-item><text:list-item><text:p>second</text:p></text:list-item></text:list>
            <text:p>after</text:p>
            """);

        Assert.Equal("- first\n\n- second\n\nafter", text);
    }

    [Fact]
    public void ReadsTablesAsRows()
    {
        string text = Extract("""
            <table:table>
              <table:table-row>
                <table:table-cell><text:p>Action</text:p></table:table-cell>
                <table:table-cell><text:p>Owner</text:p></table:table-cell>
              </table:table-row>
              <table:table-rows>
                <table:table-row>
                  <table:table-cell><text:p>Send minutes</text:p><text:p>to everyone</text:p></table:table-cell>
                  <table:table-cell><text:p>Anna</text:p></table:table-cell>
                </table:table-row>
              </table:table-rows>
            </table:table>
            """);

        Assert.Equal("Action | Owner\nSend minutes to everyone | Anna", text);
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
