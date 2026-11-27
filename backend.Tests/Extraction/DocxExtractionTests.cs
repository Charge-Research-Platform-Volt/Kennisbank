using KnowledgeBank.Services;
using KnowledgeBank.Tests.Helpers;

namespace KnowledgeBank.Tests.Extraction;

public class DocxExtractionTests
{
    private static string Extract(string body, string? styles = null, string? footnotes = null, string? endnotes = null)
        => TextExtractionService.ExtractDocx(new MemoryStream(TestDocuments.DocxXml(body, styles, footnotes, endnotes)));

    private static string P(string text, string? style = null)
        => $"<w:p>{(style != null ? $"<w:pPr><w:pStyle w:val=\"{style}\"/></w:pPr>" : "")}<w:r><w:t xml:space=\"preserve\">{text}</w:t></w:r></w:p>";

    private static string HeadingStyle(string id, string name, string? basedOn = null, int? outline = null)
        => $"<w:style w:type=\"paragraph\" w:styleId=\"{id}\"><w:name w:val=\"{name}\"/>"
           + (basedOn != null ? $"<w:basedOn w:val=\"{basedOn}\"/>" : "")
           + (outline != null ? $"<w:pPr><w:outlineLvl w:val=\"{outline}\"/></w:pPr>" : "")
           + "</w:style>";

    [Fact]
    public void Paragraphs_AreSeparatedByABlankLine()
        => Assert.Equal("First.\n\nSecond.", Extract(P("First.") + P("Second.")));

    [Fact]
    public void EmptyParagraphs_AreSkipped()
        => Assert.Equal("Text", Extract("<w:p/>" + P("  ") + P("Text")));

    [Theory]
    [InlineData("Heading1")]   // English
    [InlineData("Kop1")]       // Dutch
    [InlineData("berschrift1")] // German
    [InlineData("Titre1")]     // French
    public void HeadingStyles_AreFoundByTheirBuiltInNameInAnyLanguage(string localisedId)
    {
        string text = Extract(P("Introduction", localisedId) + P("Body."), styles: HeadingStyle(localisedId, "heading 1"));

        Assert.Equal("# Introduction\n\nBody.", text);
    }

    [Fact]
    public void HeadingLevels_AndTitle()
    {
        string styles = HeadingStyle("Title", "Title") + HeadingStyle("Heading2", "heading 2") + HeadingStyle("Heading8", "heading 8");

        string text = Extract(P("Report", "Title") + P("Methods", "Heading2") + P("Deep", "Heading8"), styles);

        Assert.Equal("# Report\n\n## Methods\n\n###### Deep", text);
    }

    [Fact]
    public void CustomStyleBasedOnAHeading_IsAHeading()
    {
        string styles = HeadingStyle("Heading1", "heading 1") + HeadingStyle("CompanyHeading", "Company heading", basedOn: "Heading1");

        Assert.Equal("# Results", Extract(P("Results", "CompanyHeading"), styles));
    }

    [Fact]
    public void StyleWithBodyTextOutline_StopsTheInheritance()
    {
        string styles = HeadingStyle("Heading1", "heading 1") + HeadingStyle("Quote", "Fancy quote", basedOn: "Heading1", outline: 9);

        Assert.Equal("Not a heading", Extract(P("Not a heading", "Quote"), styles));
    }

    [Fact]
    public void StyleWithOnlyAnOutlineLevel_IsAHeading()
        => Assert.Equal("## Chapter", Extract(P("Chapter", "Custom"), HeadingStyle("Custom", "My style", outline: 1)));

    [Fact]
    public void BasedOnCycle_DoesNotHang()
    {
        string styles = HeadingStyle("A", "a", basedOn: "B") + HeadingStyle("B", "b", basedOn: "A");

        Assert.Equal("Text", Extract(P("Text", "A"), styles));
    }

    [Fact]
    public void OutlineLevelOnTheParagraph_WinsOverItsStyle()
    {
        string paragraph = "<w:p><w:pPr><w:pStyle w:val=\"Heading1\"/><w:outlineLvl w:val=\"2\"/></w:pPr><w:r><w:t>Sub</w:t></w:r></w:p>";

        Assert.Equal("### Sub", Extract(paragraph, HeadingStyle("Heading1", "heading 1")));
    }

    [Fact]
    public void ListItems_GetADash()
    {
        string item = "<w:p><w:pPr><w:numPr><w:ilvl w:val=\"0\"/><w:numId w:val=\"1\"/></w:numPr></w:pPr><w:r><w:t>point</w:t></w:r></w:p>";

        Assert.Equal("- point\n\nafter", Extract(item + P("after")));
    }

    [Fact]
    public void Tables_BecomeRowsOfCells()
    {
        string Cell(params string[] paragraphs) => $"<w:tc>{string.Concat(paragraphs.Select(p => P(p)))}</w:tc>";
        string table = $"<w:tbl><w:tr>{Cell("Action")}{Cell("Owner")}</w:tr><w:tr>{Cell("Send minutes", "to all")}{Cell("Anna")}</w:tr><w:tr>{Cell("")}{Cell("")}</w:tr></w:tbl>";

        Assert.Equal("Before\n\nAction | Owner\nSend minutes to all | Anna\n\nAfter", Extract(P("Before") + table + P("After")));
    }

    [Fact]
    public void TabsAndBreaks_AreKept()
        => Assert.Equal("a\tb\nc", Extract("<w:p><w:r><w:t>a</w:t><w:tab/><w:t>b</w:t><w:br/><w:t>c</w:t></w:r></w:p>"));

    [Fact]
    public void DeletedTextAndFieldCodes_AreLeftOut()
    {
        string paragraph = """
            <w:p>
              <w:r><w:t xml:space="preserve">See </w:t></w:r>
              <w:del w:id="1" w:author="x"><w:r><w:delText>old words </w:delText></w:r></w:del>
              <w:r><w:fldChar w:fldCharType="begin"/></w:r>
              <w:r><w:instrText xml:space="preserve"> HYPERLINK "https://example.org" </w:instrText></w:r>
              <w:r><w:fldChar w:fldCharType="separate"/></w:r>
              <w:r><w:t>the site</w:t></w:r>
              <w:r><w:fldChar w:fldCharType="end"/></w:r>
            </w:p>
            """;

        Assert.Equal("See the site", Extract(paragraph));
    }

    [Fact]
    public void ContentControls_AreReadThrough()
        => Assert.Equal("Inside", Extract($"<w:sdt><w:sdtContent>{P("Inside")}</w:sdtContent></w:sdt>"));

    [Fact]
    public void Footnotes_AreMarkedInlineAndListedAtTheEnd()
    {
        string body = "<w:p><w:r><w:t>Claim</w:t></w:r><w:r><w:footnoteReference w:id=\"1\"/></w:r><w:r><w:t xml:space=\"preserve\"> holds.</w:t></w:r></w:p>";
        string footnotes = """
            <w:footnote w:type="separator" w:id="-1"><w:p><w:r><w:separator/></w:r></w:p></w:footnote>
            <w:footnote w:type="continuationSeparator" w:id="0"><w:p><w:r><w:continuationSeparator/></w:r></w:p></w:footnote>
            <w:footnote w:id="1"><w:p><w:r><w:footnoteRef/></w:r><w:r><w:t xml:space="preserve"> Smith 2020, p. 4.</w:t></w:r></w:p></w:footnote>
            """;

        Assert.Equal("Claim[^1] holds.\n\n## Footnotes\n\n[^1]: Smith 2020, p. 4.", Extract(body, footnotes: footnotes));
    }

    [Fact]
    public void Endnotes_GetTheirOwnPrefix()
    {
        string body = "<w:p><w:r><w:t>Claim</w:t></w:r><w:r><w:endnoteReference w:id=\"2\"/></w:r></w:p>";
        string endnotes = "<w:endnote w:id=\"2\"><w:p><w:r><w:t>Source.</w:t></w:r></w:p></w:endnote>";

        Assert.Equal("Claim[^e2]\n\n## Endnotes\n\n[^e2]: Source.", Extract(body, endnotes: endnotes));
    }

    [Fact]
    public void SimpleDocument_StillWorks()
        => Assert.Equal("One\n\nTwo", TextExtractionService.ExtractDocx(new MemoryStream(TestDocuments.Docx("One", "Two"))));

    // Found in real files (Apache POI's test corpus): Word stores a text box twice, as DrawingML and as a VML fallback
    [Fact]
    public void TextBoxes_AreReadOnce_OneParagraphPerLine()
    {
        string paragraph = """
            <w:p><w:r>
              <mc:AlternateContent xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" xmlns:wps="http://schemas.microsoft.com/office/word/2010/wordprocessingShape">
                <mc:Choice Requires="wps"><w:drawing><wp:anchor xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"><a:graphic xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"><a:graphicData uri="http://schemas.microsoft.com/office/word/2010/wordprocessingShape"><wps:wsp><wps:txbx><w:txbxContent>
                  <w:p><w:r><w:t>Box line one</w:t></w:r></w:p><w:p><w:r><w:t>Box line two</w:t></w:r></w:p>
                </w:txbxContent></wps:txbx></wps:wsp></a:graphicData></a:graphic></wp:anchor></w:drawing></mc:Choice>
                <mc:Fallback><w:pict><v:shape xmlns:v="urn:schemas-microsoft-com:vml"><v:textbox><w:txbxContent>
                  <w:p><w:r><w:t>Box line one</w:t></w:r></w:p><w:p><w:r><w:t>Box line two</w:t></w:r></w:p>
                </w:txbxContent></v:textbox></v:shape></w:pict></mc:Fallback>
              </mc:AlternateContent>
            </w:r></w:p>
            """;

        Assert.Equal("Box line one\nBox line two", Extract(paragraph));
    }

    // Smart tags (old Word) are unknown to the SDK; their text was skipped
    [Fact]
    public void SmartTags_KeepTheirText()
    {
        string paragraph = "<w:p><w:r><w:t xml:space=\"preserve\">Studied at </w:t></w:r><w:smartTag w:uri=\"urn:schemas-microsoft-com:office:smarttags\" w:element=\"place\"><w:r><w:t>Carnegie Mellon University</w:t></w:r></w:smartTag><w:r><w:t>.</w:t></w:r></w:p>";

        Assert.Equal("Studied at Carnegie Mellon University.", Extract(paragraph));
    }

    [Fact]
    public void TableRowsAndCellsInsideContentControls_AreRead()
    {
        string table = $"<w:tbl><w:sdt><w:sdtContent><w:tr><w:sdt><w:sdtContent><w:tc>{P("sdt cell")}</w:tc></w:sdtContent></w:sdt><w:tc>{P("plain")}</w:tc></w:tr></w:sdtContent></w:sdt></w:tbl>";

        Assert.Equal("sdt cell | plain", Extract(table));
    }

    [Fact]
    public void NestedTable_IsPartOfItsCellOnly()
    {
        string inner = $"<w:tbl><w:tr><w:tc>{P("inner")}</w:tc></w:tr></w:tbl>";
        string table = $"<w:tbl><w:tr><w:tc>{P("outer")}{inner}{P("")}</w:tc><w:tc>{P("next")}</w:tc></w:tr></w:tbl>";

        Assert.Equal("outer inner | next", Extract(table));
    }
}

