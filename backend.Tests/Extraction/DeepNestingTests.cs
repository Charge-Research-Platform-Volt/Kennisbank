using System.Text;
using KnowledgeBank.Services;
using KnowledgeBank.Tests.Helpers;

namespace KnowledgeBank.Tests.Extraction;

/// <summary>
/// Broken or crafted files can nest thousands deep (Apache POI's deep-table-cell.docx nests tables 5000 deep). A
/// recursive walk would overflow the stack, which .NET can't catch, so one upload would take the whole backend down.
/// Docx and pptx parts that deep are refused before parsing; ODT is read, without recursion.
/// </summary>
public class DeepNestingTests
{
    private const int Depth = 5000;

    private static string Nest(string open, string close, string inner, int depth = Depth)
    {
        StringBuilder sb = new();
        for (int i = 0; i < depth; i++) sb.Append(open);
        sb.Append(inner);
        for (int i = 0; i < depth; i++) sb.Append(close);
        return sb.ToString();
    }

    private const string Innermost = "<w:p><w:r><w:t>innermost</w:t></w:r></w:p>";

    // The SDK takes ~25 s to parse this, so it's refused before parsing
    [Fact]
    public void DocxNestedThousandsDeep_IsRefusedBeforeParsing()
    {
        string tables = Nest("<w:tbl><w:tr><w:tc>", "<w:p/></w:tc></w:tr></w:tbl>", Innermost);

        Exception error = Assert.Throws<InvalidOperationException>(() => TextExtractionService.ExtractDocx(new MemoryStream(TestDocuments.DocxXml(tables))));
        Assert.Contains("nested", error.Message);
    }

    [Fact]
    public void DocxNestedDeepButBelowTheLimit_IsRead()
    {
        // Three XML levels per table nesting, so 250 nestings stay under the limit
        string tables = Nest("<w:tbl><w:tr><w:tc>", "<w:p/></w:tc></w:tr></w:tbl>", Innermost, depth: 250);

        Assert.Contains("innermost", TextExtractionService.ExtractDocx(new MemoryStream(TestDocuments.DocxXml(tables))));
    }

    [Fact]
    public void DocxContentControlsNestedThousandsDeep_AreRefused()
        => Assert.Throws<InvalidOperationException>(() => TextExtractionService.ExtractDocx(new MemoryStream(TestDocuments.DocxXml(Nest("<w:sdt><w:sdtContent>", "</w:sdtContent></w:sdt>", Innermost)))));

    // ODT goes through XDocument, which parses deep XML quickly, so it's read; the walk over it mustn't recurse
    [Fact]
    public void OdtSectionsAndSpansNestedThousandsDeep_AreRead()
    {
        string sections = Nest("<text:section>", "</text:section>", "<text:p>" + Nest("<text:span>", "</text:span>", "innermost") + "</text:p>");

        Assert.Equal("innermost", TextExtractionService.ExtractOdt(new MemoryStream(TestDocuments.Odt(sections))));
    }

    [Fact]
    public void OdtNoteInsideANote_IsSkipped()
    {
        string text = TextExtractionService.ExtractOdt(new MemoryStream(TestDocuments.Odt(
            "<text:p>A<text:note><text:note-body><text:p>outer<text:note><text:note-body><text:p>inner</text:p></text:note-body></text:note></text:p></text:note-body></text:note></text:p>")));

        Assert.Equal("A[^1]\n\n## Notes\n\n[^1]: outer", text);
    }
}
