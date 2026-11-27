using System.IO.Compression;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using P = DocumentFormat.OpenXml.Presentation;
using Dgm = DocumentFormat.OpenXml.Drawing.Diagrams;

namespace KnowledgeBank.Tests.Helpers;

/// <summary>Small real documents built in code, so tests don't depend on binary fixture files.</summary>
public static class TestDocuments
{
    /// <summary>A minimal OpenDocument text file: the zip with a content.xml around <paramref name="body"/>.</summary>
    public static byte[] Odt(string body)
    {
        string xml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <office:document-content
                xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
                xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0"
                xmlns:table="urn:oasis:names:tc:opendocument:xmlns:table:1.0">
              <office:body><office:text>{body}</office:text></office:body>
            </office:document-content>
            """;

        using MemoryStream stream = new();
        using (ZipArchive zip = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (StreamWriter mimetype = new(zip.CreateEntry("mimetype").Open()))
                mimetype.Write("application/vnd.oasis.opendocument.text");
            using (StreamWriter content = new(zip.CreateEntry("content.xml").Open(), Encoding.UTF8))
                content.Write(xml);
        }
        return stream.ToArray();
    }

    /// <summary>A minimal Word document with one paragraph per string.</summary>
    public static byte[] Docx(params string[] paragraphs)
    {
        using MemoryStream stream = new();
        using (WordprocessingDocument document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            MainDocumentPart main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(paragraphs.Select(p => new Paragraph(new Run(new Text(p))))));
        }
        return stream.ToArray();
    }

    private const string WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    /// <summary>
    /// A Word document from raw WordprocessingML: <paramref name="body"/> goes inside w:body, and the optional styles
    /// and notes inside their own parts. Everything uses the w: prefix.
    /// </summary>
    public static byte[] DocxXml(string body, string? styles = null, string? footnotes = null, string? endnotes = null)
    {
        using MemoryStream stream = new();
        using (WordprocessingDocument document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            MainDocumentPart main = document.AddMainDocumentPart();
            main.Document = new Document($"""<w:document xmlns:w="{WordNamespace}"><w:body>{body}</w:body></w:document>""");

            if (styles != null)
                main.AddNewPart<StyleDefinitionsPart>().Styles = new Styles($"""<w:styles xmlns:w="{WordNamespace}">{styles}</w:styles>""");
            if (footnotes != null)
                main.AddNewPart<FootnotesPart>().Footnotes = new Footnotes($"""<w:footnotes xmlns:w="{WordNamespace}">{footnotes}</w:footnotes>""");
            if (endnotes != null)
                main.AddNewPart<EndnotesPart>().Endnotes = new Endnotes($"""<w:endnotes xmlns:w="{WordNamespace}">{endnotes}</w:endnotes>""");
        }
        return stream.ToArray();
    }

    private const string PresentationNamespaces =
        """xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" """;

    /// <summary>A slide for <see cref="Pptx"/>: the shapes (p:sp, p:graphicFrame) of its shape tree, and optional speaker notes</summary>
    public record Slide(string Shapes, string? Notes = null, string[]? SmartArt = null);

    /// <summary>
    /// A presentation with these slides, without masters or layouts: enough to read, not to open in PowerPoint.
    /// Notes become the body placeholder of the slide's notes page.
    /// </summary>
    public static byte[] Pptx(params Slide[] slides)
    {
        using MemoryStream stream = new();
        using (PresentationDocument document = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
        {
            PresentationPart presentation = document.AddPresentationPart();
            P.SlideIdList slideIds = new();
            presentation.Presentation = new P.Presentation(slideIds);

            for (int i = 0; i < slides.Length; i++)
            {
                SlidePart slidePart = presentation.AddNewPart<SlidePart>();
                string smartArt = "";

                // SmartArt: the slide holds a frame pointing at a diagram data part, which holds the text, one point per box
                if (slides[i].SmartArt is string[] points)
                {
                    DiagramDataPart data = slidePart.AddNewPart<DiagramDataPart>();
                    data.DataModelRoot = new Dgm.DataModelRoot(
                        $"""<dgm:dataModel xmlns:dgm="http://schemas.openxmlformats.org/drawingml/2006/diagram" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"><dgm:ptLst><dgm:pt modelId="0" type="doc"><dgm:t><a:bodyPr/><a:p/></dgm:t></dgm:pt>{string.Concat(points.Select((p, n) => $"<dgm:pt modelId=\"{n + 1}\"><dgm:t><a:bodyPr/><a:p><a:r><a:t>{p}</a:t></a:r></a:p></dgm:t></dgm:pt>"))}</dgm:ptLst><dgm:cxnLst/></dgm:dataModel>""");
                    smartArt = $"""<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id="4" name="Diagram"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr><p:xfrm/><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/diagram"><dgm:relIds xmlns:dgm="http://schemas.openxmlformats.org/drawingml/2006/diagram" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" r:dm="{slidePart.GetIdOfPart(data)}" r:lo="" r:qs="" r:cs=""/></a:graphicData></a:graphic></p:graphicFrame>""";
                }

                slidePart.Slide = new P.Slide($"<p:sld {PresentationNamespaces}><p:cSld><p:spTree>{slides[i].Shapes}{smartArt}</p:spTree></p:cSld></p:sld>");

                if (slides[i].Notes != null)
                    slidePart.AddNewPart<NotesSlidePart>().NotesSlide = new P.NotesSlide(
                        $"<p:notes {PresentationNamespaces}><p:cSld><p:spTree>{Shape("body", slides[i].Notes!)}{Shape("sldNum", "7")}</p:spTree></p:cSld></p:notes>");

                slideIds.Append(new P.SlideId { Id = (uint)(256 + i), RelationshipId = presentation.GetIdOfPart(slidePart) });
            }
        }
        return stream.ToArray();
    }

    /// <summary>A text shape, a placeholder when <paramref name="placeholder"/> is given (title, body, sldNum, ...), one a:p per paragraph</summary>
    public static string Shape(string? placeholder, params string[] paragraphs)
        => $"""<p:sp><p:nvSpPr><p:cNvPr id="2" name="Shape"/><p:cNvSpPr/><p:nvPr>{(placeholder != null ? $"<p:ph type=\"{placeholder}\"/>" : "")}</p:nvPr></p:nvSpPr><p:spPr/><p:txBody><a:bodyPr/>{string.Concat(paragraphs.Select(p => $"<a:p><a:r><a:t>{p}</a:t></a:r></a:p>"))}</p:txBody></p:sp>""";

    /// <summary>A table in a graphic frame, one string array per row</summary>
    public static string Table(params string[][] rows)
        => $"""<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id="3" name="Table"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr><p:xfrm/><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/table"><a:tbl>{string.Concat(rows.Select(r => $"<a:tr h=\"0\">{string.Concat(r.Select(c => $"<a:tc><a:txBody><a:bodyPr/><a:p><a:r><a:t>{c}</a:t></a:r></a:p></a:txBody></a:tc>"))}</a:tr>"))}</a:tbl></a:graphicData></a:graphic></p:graphicFrame>""";
}

