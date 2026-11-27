using DocumentFormat.OpenXml;
using W = DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using Dgm = DocumentFormat.OpenXml.Drawing.Diagrams;
using ClosedXML.Excel;
using System.Text;
using KnowledgeBank.Utils;
using KnowledgeBank.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Background;
using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;

namespace KnowledgeBank.Services;

/// <summary>
/// Service for extracting text from various document formats
/// Uses free methods when possible, falls back to OCR
/// </summary>
public class TextExtractionService(ILogger<TextExtractionService> logger, EnvironmentConfig environmentConfig, WebscrapeClient webscrapeClient, IStorageService storageService, MistralHttpClient mistralClient, UsageMetrics usageMetrics)
{
    #region File Text Extraction

    /// <summary>
    /// Extracts text from a document stream, choosing the best extraction method based on file type
    /// </summary>
    /// <param name="stream">Document file stream</param>
    /// <param name="fileExtension">File extension (e.g. ".pdf", ".docx")</param>
    /// <returns>Extracted text content</returns>
    public async Task<string> ExtractTextFromFileAsync(Stream stream, string fileExtension)
        => (await ExtractFileAsync(stream, fileExtension)).Text;

    /// <summary>
    /// Extracts a document with the best method for its time. PDFs and images (OCR) and pptx (slides) also say where
    /// each page or slide starts; other formats only have text.
    /// </summary>
    private async Task<OcrResult> ExtractFileAsync(Stream stream, string fileExtension)
    {
        fileExtension = fileExtension.ToLowerInvariant();

        logger.LogInformation("Extracting text from {FileType} document", fileExtension);

        // Ensure stream is seekable -- HTTP streams are not
        if (!stream.CanSeek)
        {
            MemoryStream ms = new();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            stream = ms;
        }

        try
        {
            return fileExtension switch
            {
                ".pdf" => await ExtractWithMistralOCR(stream, fileExtension),
                ".txt" => new OcrResult { Text = await ExtractFromPlainText(stream) },
                ".docx" => new OcrResult { Text = ExtractDocx(stream) },
                ".odt" => new OcrResult { Text = ExtractOdt(stream) },
                ".pptx" => ExtractPptx(stream),
                ".xlsx" => new OcrResult { Text = ExtractWithClosedXmlXlsx(stream) },
                ".html" => new OcrResult { Text = await ExtractHtmlFileAsync(stream) },
                _ when Filetype.SupportedImage(fileExtension) => await ExtractWithMistralOCR(stream, fileExtension),
                _ => throw new NotSupportedException($"Unsupported file type: {fileExtension}")
            };
        }
        catch (FileFormatException e)
        {
            logger.LogWarning(e, "File is corrupted or not a valid {FileType} document", fileExtension);
            throw new InvalidOperationException($"The file appears to be corrupted or is not a valid {fileExtension} document.", e);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to extract text from {FileType}", fileExtension);
            throw;
        }
    }
    
    public async Task<OcrResult> ExtractOcrResultFromFileAsync(Stream stream, string fileExtension, string? bucketName = null, string? fileId = null)
    {
        bool isOcr = string.Equals(fileExtension, ".pdf", StringComparison.OrdinalIgnoreCase) || Filetype.SupportedImage(fileExtension);
        if (!isOcr) return await ExtractFileAsync(stream, fileExtension);

        bool canCache = bucketName != null && fileId != null;
        string modelId = await ResolveOcrModelIdAsync();

        if (canCache)
        {
            OcrResult? cached = await TryReadOcrCacheAsync(bucketName!, fileId!, modelId);
            if (cached != null)
            {
                usageMetrics.Record("ocr.cache_hit");
                return cached;
            }
        }

        OcrResult result = await ExtractWithMistralOCR(stream, fileExtension, modelId);

        if (canCache)
            await WriteOcrCacheAsync(bucketName!, fileId!, result);

        return result;
    }

    /// <summary>
    /// The OCR model this app is configured to use. Leave as "mistral-ocr-latest" to auto-track
    /// whatever Mistral currently resolves that alias to; set to a specific dated model id to pin it
    /// and skip the resolution lookup entirely.
    /// </summary>
    private async Task<string> ResolveOcrModelIdAsync()
    {
        string configuredOcrModel = environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_OCR_MODEL_NAME);

        return configuredOcrModel == "mistral-ocr-latest"
            ? await mistralClient.ResolveModelAliasAsync(configuredOcrModel)
            : configuredOcrModel;
    }

    private async Task<OcrResult> ExtractWithMistralOCR(Stream stream, string fileExtension, string? model = null)
    {
        logger.LogInformation("Using Mistral OCR for {FileType}", fileExtension);
        string resolvedModel = model ?? await ResolveOcrModelIdAsync();

        stream.Position = 0;
        using MemoryStream ms = new();
        await stream.CopyToAsync(ms);

        bool isPdf = string.Equals(fileExtension, ".pdf", StringComparison.OrdinalIgnoreCase);

        Stopwatch stopwatch = Stopwatch.StartNew();
        MistralOcrResult ocrResult;
        try
        {
            ocrResult = await mistralClient.RunOcrAsync(ms.ToArray(), fileExtension, resolvedModel, isPdf);
        }
        catch
        {
            usageMetrics.Record("ocr.document", success: false, duration: stopwatch.Elapsed);
            throw;
        }

        usageMetrics.Record("ocr.document", duration: stopwatch.Elapsed);
        usageMetrics.Record("ocr.pages", count: ocrResult.Pages.Count);

        HashSet<string> headerFooterLines = new(StringComparer.OrdinalIgnoreCase);

        foreach (MistralOcrPage page in ocrResult.Pages)
        {
            if (!string.IsNullOrWhiteSpace(page.Header)) headerFooterLines.Add(page.Header.Trim());
            if (!string.IsNullOrWhiteSpace(page.Footer)) headerFooterLines.Add(page.Footer.Trim());
        }

        OcrResult result = BuildOcrResult([.. ocrResult.Pages.Select(p => p.Markdown ?? "")], string.Join("\n", headerFooterLines), ocrResult.Model);
        logger.LogInformation("Mistral OCR extracted {Length} characters from {Pages} pages.", result.Text.Length, ocrResult.Pages.Count);

        return result;
    }

    private async Task<OcrResult?> TryReadOcrCacheAsync(string bucketName, string fileId, string currentModelId)
    {
        try
        {
            ObjectDownloadResponse response = await storageService.DownloadObjectAsync(bucketName, $"{fileId}-ocr");
            using StreamReader reader = new(response.Stream);
            string json = await reader.ReadToEndAsync();
            OcrResult? cached = JsonSerializer.Deserialize<OcrResult>(json);

            if (cached == null) return null;

            if (cached.Model != currentModelId)
            {
                logger.LogInformation("OCR cache stale for {FileId} (cached model: {CachedModel}, current model: {CurrentModel}), re-running OCR", fileId, cached.Model, currentModelId);
                return null;
            }

            if (cached.Pages == null)
            {
                logger.LogInformation("OCR cache for {FileId} predates per-page storage, re-running OCR", fileId);
                return null;
            }

            logger.LogInformation("OCR cache hit for {FileId} (model: {Model})", fileId, cached.Model);
            return BuildOcrResult(cached.Pages, cached.HeaderFooterText, cached.Model);
        }
        catch
        {
            return null;
        }
    }

    private async Task WriteOcrCacheAsync(string bucketName, string fileId, OcrResult result)
    {
        try
        {
            string json = JsonSerializer.Serialize(result);
            using MemoryStream ms = new(Encoding.UTF8.GetBytes(json));
            await storageService.UploadObjectAsync(bucketName, $"{fileId}-ocr", ms, contentType: "application/json");
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to write OCR cache for {FileId}", fileId);
        }
    }

    private async Task<string> ExtractFromPlainText(Stream stream)
    {
        logger.LogInformation("Extracting from plain text document");

        stream.Position = 0;
        using var reader = new StreamReader(stream, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private async Task<string> ExtractHtmlFileAsync(Stream stream)
    {
        logger.LogInformation("Extracting text from HTML document");

        stream.Position = 0;
        using StreamReader reader = new(stream, leaveOpen: true);
        string html = await reader.ReadToEndAsync();

        WebscrapeResult result = await webscrapeClient.ExtractFromHtmlAsync(html);
        return result.TextContent ?? "";
    }

    /// <summary>
    /// Word documents as markdown-like text: headings as # (found by the style's built-in name, which Word keeps in
    /// every language, so Dutch "Kop 1" counts too), list items as "- ", table rows as cells joined by " | ", and
    /// footnotes and endnotes at the end, referenced inline as [^n]. Blocks are separated by a blank line, so chunks
    /// and passages break between paragraphs. Only visible text is read: deleted tracked changes and field codes are
    /// their own element types and are left out.
    /// </summary>
    internal static string ExtractDocx(Stream stream)
    {
        stream.Position = 0;
        using WordprocessingDocument document = WordprocessingDocument.Open(stream, false);
        MainDocumentPart? main = document.MainDocumentPart;
        EnsureNotTooDeep(main, main?.FootnotesPart, main?.EndnotesPart);
        W.Body? body = main?.Document?.Body;
        if (main == null || body == null) return string.Empty;

        Dictionary<string, int> headingLevels = DocxHeadingLevels(main);
        List<string> blocks = [];
        AppendDocxBlocks(body, headingLevels, blocks);

        AppendDocxNotes("Footnotes", main.FootnotesPart?.Footnotes?.Elements<W.Footnote>(), "", blocks);
        AppendDocxNotes("Endnotes", main.EndnotesPart?.Endnotes?.Elements<W.Endnote>(), "e", blocks);

        return string.Join("\n\n", blocks);
    }

    // Every walk over document content in this file uses an explicit stack instead of recursion: tables, text boxes and
    // content controls can nest thousands deep (in broken or crafted files), and a stack overflow can't be caught, so it
    // would take the whole process down
    private static void AppendDocxBlocks(OpenXmlElement container, Dictionary<string, int> headingLevels, List<string> blocks)
    {
        Stack<OpenXmlElement> stack = new(container.ChildElements.Reverse());

        while (stack.Count > 0)
        {
            switch (stack.Pop())
            {
                case W.Paragraph paragraph:
                    string text = DocxText(paragraph).Trim();
                    if (text.Length == 0) break;

                    if (DocxHeadingLevel(paragraph, headingLevels) is int level)
                        blocks.Add($"{new string('#', level)} {text.ReplaceLineEndings(" ")}");
                    else if (paragraph.ParagraphProperties?.NumberingProperties != null)
                        blocks.Add($"- {text}");
                    else
                        blocks.Add(text);
                    break;

                case W.Table table:
                    string rows = DocxTable(table);
                    if (rows.Length > 0) blocks.Add(rows);
                    break;

                // Content controls, custom XML and the like wrap ordinary paragraphs and tables
                case OpenXmlElement wrapper:
                    foreach (OpenXmlElement child in wrapper.ChildElements.Reverse()) stack.Push(child);
                    break;
            }
        }
    }

    /// <summary>
    /// Paragraph styles that are headings, by style id. Matched on the built-in name ("heading 1", "Title"), which
    /// Word stores in English whatever the UI language (only the style id is localised, e.g. "Kop1" or "berschrift1"),
    /// else on the style's outline level. A custom style based on a heading is a heading too.
    /// </summary>
    private static Dictionary<string, int> DocxHeadingLevels(MainDocumentPart main)
    {
        Dictionary<string, W.Style> styles = (main.StyleDefinitionsPart?.Styles?.Elements<W.Style>() ?? [])
            .Where(s => s.StyleId?.Value != null)
            .GroupBy(s => s.StyleId!.Value!)
            .ToDictionary(g => g.Key, g => g.First());

        Dictionary<string, int> levels = [];
        foreach (string id in styles.Keys)
            if (StyleHeadingLevel(id, styles, depth: 0) is int level)
                levels[id] = level;

        return levels;
    }

    // The depth guards against a basedOn cycle in a broken file
    private static int? StyleHeadingLevel(string id, Dictionary<string, W.Style> styles, int depth)
    {
        if (depth > 10 || !styles.TryGetValue(id, out W.Style? style)) return null;

        string name = style.StyleName?.Val?.Value?.ToLowerInvariant() ?? "";
        if (name == "title") return 1;
        if (name.StartsWith("heading ") && int.TryParse(name["heading ".Length..], out int level) && level is >= 1 and <= 9)
            return Math.Min(level, 6);

        // Its own outline level wins over the parent's; 9 means body text, so that stops the inheritance
        if (style.StyleParagraphProperties?.OutlineLevel?.Val?.Value is int outline)
            return outline < 9 ? Math.Min(outline + 1, 6) : null;

        return style.BasedOn?.Val?.Value is string parent ? StyleHeadingLevel(parent, styles, depth + 1) : null;
    }

    // An outline level set on the paragraph itself wins over its style; level 9 means body text
    private static int? DocxHeadingLevel(W.Paragraph paragraph, Dictionary<string, int> headingLevels)
    {
        W.ParagraphProperties? properties = paragraph.ParagraphProperties;

        if (properties?.OutlineLevel?.Val?.Value is int outline)
            return outline < 9 ? Math.Min(outline + 1, 6) : null;

        return properties?.ParagraphStyleId?.Val?.Value is string styleId && headingLevels.TryGetValue(styleId, out int level) ? level : null;
    }

    private const string WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    // Real documents stay well under 100 levels; a table in a table in a text box is still only a few dozen
    internal const int MaxXmlDepth = 1000;

    /// <summary>
    /// The OpenXML SDK parses absurdly deep XML very slowly: a 17 KB file nesting tables 5000 deep takes it ~25 s.
    /// Parts are parsed on first access, so this reads their raw XML first (linear, and only once) and refuses a part
    /// that nests too deep before the SDK starts on it.
    /// </summary>
    private static void EnsureNotTooDeep(params OpenXmlPart?[] parts)
    {
        foreach (OpenXmlPart? part in parts)
        {
            if (part == null) continue;

            using Stream stream = part.GetStream(FileMode.Open, FileAccess.Read);
            using System.Xml.XmlReader reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit });

            while (reader.Read())
                if (reader.Depth > MaxXmlDepth)
                    throw new InvalidOperationException($"The document is nested more than {MaxXmlDepth} levels deep, which no real document does, so it isn't read");
        }
    }

    private static string DocxText(OpenXmlElement root)
    {
        StringBuilder sb = new();
        Stack<OpenXmlElement> stack = new(root.ChildElements.Reverse());

        while (stack.Count > 0)
        {
            OpenXmlElement element = stack.Pop();

            switch (element)
            {
                case W.Text text: sb.Append(text.Text); continue;
                case W.TabChar: sb.Append('\t'); continue;
                case W.Break or W.CarriageReturn: sb.Append('\n'); continue;
                case W.NoBreakHyphen: sb.Append('-'); continue;
                case W.FootnoteReference note: sb.Append($"[^{note.Id?.Value}]"); continue;
                case W.EndnoteReference note: sb.Append($"[^e{note.Id?.Value}]"); continue;

                // Text boxes and shapes are stored twice: as DrawingML, and as a VML fallback for old Word. Read only the first
                case AlternateContentFallback: continue;

                // Elements the SDK doesn't know (smart tags, from old Word) are read as unknown, text included
                case OpenXmlUnknownElement unknown when unknown.LocalName == "t" && unknown.NamespaceUri == WordNamespace:
                    sb.Append(unknown.InnerText);
                    continue;

                // A paragraph inside this one is a text box's or a cell's: it goes on its own line
                case W.Paragraph:
                    if (sb.Length > 0 && sb[^1] != '\n') sb.Append('\n');
                    break;
            }

            foreach (OpenXmlElement child in element.ChildElements.Reverse()) stack.Push(child);
        }

        return sb.ToString();
    }

    private static string DocxTable(W.Table table)
        => string.Join("\n", OwnDescendants<W.TableRow>(table)
            .Select(row => string.Join(" | ", OwnDescendants<W.TableCell>(row).Select(cell => OneLine(DocxText(cell)))))
            .Where(line => line.Replace("|", "").Trim().Length > 0));

    /// <summary>
    /// Rows and cells can be wrapped in content controls (w:sdt), so they're found below any wrapper, but never inside
    /// a nested table: that one belongs to its cell's text.
    /// </summary>
    private static List<T> OwnDescendants<T>(OpenXmlElement container) where T : OpenXmlElement
    {
        List<T> found = [];
        Stack<OpenXmlElement> stack = new(container.ChildElements.Reverse());

        while (stack.Count > 0)
        {
            OpenXmlElement element = stack.Pop();
            if (element is T match) found.Add(match);
            else if (element is not W.Table)
                foreach (OpenXmlElement child in element.ChildElements.Reverse()) stack.Push(child);
        }

        return found;
    }

    private static void AppendDocxNotes(string heading, IEnumerable<W.FootnoteEndnoteType>? notes, string prefix, List<string> blocks)
    {
        // Ids below 1 are the separator lines Word puts between the text and the notes
        List<string> lines = [.. (notes ?? [])
            .Where(n => n.Id?.Value > 0)
            .Select(n => (Id: n.Id!.Value, Text: OneLine(DocxText(n))))
            .Where(n => n.Text.Length > 0)
            .Select(n => $"[^{prefix}{n.Id}]: {n.Text}")];

        if (lines.Count > 0)
            blocks.Add($"## {heading}\n\n{string.Join("\n", lines)}");
    }

    private static string OneLine(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static readonly XNamespace OdfText = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
    private static readonly XNamespace OdfOffice = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
    private static readonly XNamespace OdfTable = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";

    /// <summary>
    /// OpenDocument text (LibreOffice's own format) is a zip whose content.xml holds the text, so like .docx it's
    /// read directly: free and exact, no conversion or OCR. The same shape as Word documents: headings as #, list
    /// items as "- ", table rows as cells joined by " | ", notes at the end referenced inline as [^n], blocks
    /// separated by a blank line.
    /// </summary>
    internal static string ExtractOdt(Stream stream)
    {
        stream.Position = 0;
        using ZipArchive zip = new(stream, ZipArchiveMode.Read, leaveOpen: true);
        ZipArchiveEntry? entry = zip.GetEntry("content.xml");
        if (entry == null) return string.Empty;

        XDocument content;
        using (Stream entryStream = entry.Open())
            content = XDocument.Load(entryStream, System.Xml.Linq.LoadOptions.PreserveWhitespace);

        XElement? body = content.Descendants(OdfOffice + "text").FirstOrDefault();
        if (body == null) return string.Empty;

        List<string> blocks = [];
        List<string> notes = [];
        AppendOdtBlocks(body, blocks, notes);

        if (notes.Count > 0)
            blocks.Add($"## Notes\n\n{string.Join("\n", notes)}");

        return string.Join("\n\n", blocks);
    }

    private static void AppendOdtBlocks(XElement container, List<string> blocks, List<string> notes)
    {
        Stack<(XElement Element, bool InList)> stack = new(container.Elements().Reverse().Select(e => (e, false)));

        while (stack.Count > 0)
        {
            (XElement element, bool inList) = stack.Pop();

            if (element.Name == OdfText + "h" || element.Name == OdfText + "p")
            {
                string text = OdtText(element, notes).Trim();
                if (text.Length == 0) continue;

                if (element.Name == OdfText + "h")
                {
                    int level = Math.Clamp((int?)element.Attribute(OdfText + "outline-level") ?? 1, 1, 6);
                    blocks.Add($"{new string('#', level)} {text.ReplaceLineEndings(" ")}");
                }
                else blocks.Add(inList ? $"- {text}" : text);
            }
            else if (element.Name == OdfTable + "table")
            {
                string rows = string.Join("\n", element.Descendants(OdfTable + "table-row")
                    .Select(row => string.Join(" | ", row.Elements(OdfTable + "table-cell").Select(cell => OneLine(OdtText(cell, notes)))))
                    .Where(line => line.Replace("|", "").Trim().Length > 0));
                if (rows.Length > 0) blocks.Add(rows);
            }
            else
            {
                bool childrenInList = inList || element.Name == OdfText + "list";
                foreach (XElement child in element.Elements().Reverse()) stack.Push((child, childrenInList));
            }
        }
    }

    /// <summary>
    /// The text of a paragraph or cell. A footnote becomes a [^n] marker, and its text is collected into notes.
    /// A note inside a note isn't valid ODF and is skipped, so reading a note's text never goes deeper than one level.
    /// </summary>
    private static string OdtText(XElement root, List<string> notes, bool inNote = false)
    {
        StringBuilder line = new();
        Stack<XNode> stack = new(root.Nodes().Reverse());

        while (stack.Count > 0)
        {
            XNode node = stack.Pop();

            // Runs of whitespace in ODF text count as one space; extra spaces are written as <text:s>
            if (node is XText text)
            {
                line.Append(Regex.Replace(text.Value, @"\s+", " "));
                continue;
            }
            if (node is not XElement element) continue;

            if (element.Name == OdfText + "s")
                line.Append(' ', Math.Clamp((int?)element.Attribute(OdfText + "c") ?? 1, 0, 100));
            else if (element.Name == OdfText + "tab")
                line.Append('\t');
            else if (element.Name == OdfText + "line-break")
                line.Append('\n');
            else if (element.Name == OdfText + "note")
            {
                if (inNote) continue;

                string marker = element.Element(OdfText + "note-citation")?.Value.Trim() is { Length: > 0 } citation ? citation : (notes.Count + 1).ToString();
                string noteText = element.Element(OdfText + "note-body") is XElement noteBody ? OneLine(OdtText(noteBody, notes, inNote: true)) : "";
                line.Append($"[^{marker}]");
                if (noteText.Length > 0) notes.Add($"[^{marker}]: {noteText}");
            }
            else
            {
                // Paragraphs inside a cell or note follow each other on one line
                if ((element.Name == OdfText + "p" || element.Name == OdfText + "h") && line.Length > 0 && !char.IsWhiteSpace(line[^1]))
                    line.Append(' ');

                foreach (XNode child in element.Nodes().Reverse()) stack.Push(child);
            }
        }

        return line.ToString();
    }

    /// <summary>
    /// Each slide becomes a section, "## Slide n: title", followed by its text one paragraph per line, its tables and
    /// its speaker notes. Where each slide starts goes in PageStarts, so passages show the slide number as their page.
    /// A slide without text still gets a start, so the numbers stay aligned with the deck.
    /// </summary>
    internal static OcrResult ExtractPptx(Stream stream)
    {
        stream.Position = 0;
        using PresentationDocument presentation = PresentationDocument.Open(stream, false);
        PresentationPart? part = presentation.PresentationPart;
        List<P.SlideId> slideIds = [.. part?.Presentation?.SlideIdList?.Elements<P.SlideId>() ?? []];

        StringBuilder sb = new();
        int[] slideStarts = new int[slideIds.Count];

        for (int i = 0; i < slideIds.Count; i++)
        {
            slideStarts[i] = sb.Length;

            if (slideIds[i].RelationshipId?.Value is not string relationshipId || part!.GetPartById(relationshipId) is not SlidePart slidePart)
                continue;

            EnsureNotTooDeep(slidePart, slidePart.NotesSlidePart);
            if (slidePart.Slide is not P.Slide slide)
                continue;

            List<string> titles = [];
            List<string> blocks = [];

            // Document order, so the text follows the slide's shape tree
            foreach (OpenXmlElement element in slide.Descendants().Where(e => e is P.Shape or A.Table or Dgm.RelationshipIds))
            {
                // SmartArt keeps its text in a diagram data part of its own, one point per box
                if (element is Dgm.RelationshipIds diagram)
                {
                    if (diagram.DataPart?.Value is string dataId && slidePart.TryGetPartById(dataId, out OpenXmlPart? dataPart) && dataPart is DiagramDataPart data)
                    {
                        string points = string.Join("\n", (data.DataModelRoot?.Descendants<Dgm.TextBody>() ?? []).Select(PptxText).Where(t => t.Length > 0));
                        if (points.Length > 0) blocks.Add(points);
                    }
                    continue;
                }

                if (element is A.Table table)
                {
                    string rows = string.Join("\n", table.Elements<A.TableRow>()
                        .Select(row => string.Join(" | ", row.Elements<A.TableCell>().Select(cell => OneLine(PptxText(cell.TextBody)))))
                        .Where(line => line.Replace("|", "").Trim().Length > 0));
                    if (rows.Length > 0) blocks.Add(rows);
                    continue;
                }

                P.Shape shape = (P.Shape)element;
                P.PlaceholderValues? type = PlaceholderType(shape);
                if (type == P.PlaceholderValues.SlideNumber || type == P.PlaceholderValues.Footer || type == P.PlaceholderValues.DateAndTime) continue;

                string text = PptxText(shape.TextBody);
                if (text.Length == 0) continue;

                if (type == P.PlaceholderValues.Title || type == P.PlaceholderValues.CenteredTitle) titles.Add(text);
                else blocks.Add(text);
            }

            string notes = string.Join("\n", (slidePart.NotesSlidePart?.NotesSlide?.Descendants<P.Shape>() ?? [])
                .Where(s => PlaceholderType(s) == P.PlaceholderValues.Body)
                .Select(s => PptxText(s.TextBody))
                .Where(t => t.Length > 0));

            if (titles.Count == 0 && blocks.Count == 0 && notes.Length == 0) continue;

            sb.Append($"## Slide {i + 1}");
            if (titles.Count > 0) sb.Append($": {string.Join(" ", titles).ReplaceLineEndings(" ")}");
            sb.Append("\n\n");
            foreach (string block in blocks) sb.Append(block).Append("\n\n");
            if (notes.Length > 0) sb.Append("Speaker notes:\n").Append(notes).Append("\n\n");
        }

        // Without any text there's nothing to index, not even the slide headings
        return sb.Length == 0 ? new OcrResult() : new OcrResult { Text = sb.ToString(), PageStarts = slideStarts };
    }

    private static P.PlaceholderValues? PlaceholderType(P.Shape shape)
        => shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.PlaceholderShape?.Type?.Value;

    /// <summary>One line per paragraph; a line break inside a paragraph stays a line break</summary>
    private static string PptxText(OpenXmlElement? textBody)
        => textBody == null ? "" : string.Join("\n", textBody.Elements<A.Paragraph>()
            .Select(p => string.Concat(p.Elements().Select(e => e switch
            {
                A.Run run => run.Text?.Text ?? "",
                A.Field field => field.Text?.Text ?? "",
                A.Break => "\n",
                _ => ""
            })).Trim())
            .Where(p => p.Length > 0));
    
    private static string ExtractWithClosedXmlXlsx(Stream stream)
    {
        stream.Position = 0;
        using XLWorkbook wb = new(stream);

        StringBuilder sb = new();

        foreach (var sheet in wb.Worksheets)
        {
            sb.AppendLine($"Sheet: {sheet.Name}");

            foreach (var row in sheet.RowsUsed())
            {
                string line = string.Join("\t", row.CellsUsed().Select(c => c.Value.ToString()).Where(v => !string.IsNullOrWhiteSpace(v)));

                if (!string.IsNullOrWhiteSpace(line))
                    sb.AppendLine(line);
            }
        }

        return sb.ToString();
    }

    internal static string CleanOcrMarkdown(string text)
    {
        text = Regex.Replace(text, @"!\[[^\]]*\]\([^\)]*\)", "");
        text = Regex.Replace(text, @"\^\{\}\[\]", "");
        text = Regex.Replace(text, @"(?m)^#+\s*$", "");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");

        text = System.Net.WebUtility.HtmlDecode(text);

        return text;
    }

    /// <summary>
    /// Scraped pages come as markdown. Images go, and links keep their text without the URL (one level of parentheses
    /// inside a URL is allowed, as in Wikipedia links). The blank lines left behind collapse.
    /// </summary>
    internal static string CleanWebMarkdown(string text)
    {
        text = Regex.Replace(text, @"!\[[^\]]*\]\([^\)]*\)", "");
        text = Regex.Replace(text, @"\[([^\]]*)\]\((?:[^()\s]|\([^()\s]*\))*(?:\s+""[^""]*"")?\)", "$1");
        text = Regex.Replace(text, @"[ \t]+\n", "\n");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }

    /// <summary>
    /// Builds the text from raw OCR pages: each page cleaned and followed by a line break, recording where each page
    /// starts. A page without text still gets a start, so page numbers stay aligned with the document.
    /// </summary>
    internal static OcrResult BuildOcrResult(List<string> pages, string headerFooterText, string model)
    {
        StringBuilder sb = new();
        int[] pageStarts = new int[pages.Count];

        for (int i = 0; i < pages.Count; i++)
        {
            pageStarts[i] = sb.Length;

            string cleaned = CleanOcrMarkdown(pages[i]);
            if (!string.IsNullOrWhiteSpace(cleaned))
                sb.Append(cleaned).Append('\n');
        }

        return new OcrResult { Text = sb.ToString(), HeaderFooterText = headerFooterText, Model = model, Pages = pages, PageStarts = pageStarts };
    }
    
    #endregion
    
    #region Web Text Extraction

    public async Task<WebscrapeResult> ExtractTextFromWebAsync(string url)
    {
        logger.LogInformation("Extracting text from webpage with url '{url}'", url);

        // Handle direct document URLs (download and extract as file instead of using webscrape)
        if (Filetype.IsDocumentUrl(url))
        {
            logger.LogInformation("URL points to a document file, downloading and extracting directly");
            return await ExtractFromDocumentUrlAsync(url, Filetype.GetDocumentUrlExtension(url));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        WebscrapeResult result;
        try
        {
            result = await webscrapeClient.ExtractFromUrlAsync(url);
        }
        catch
        {
            usageMetrics.Record("webscrape.page", success: false, duration: stopwatch.Elapsed);
            throw;
        }

        usageMetrics.Record("webscrape.page", success: !string.IsNullOrWhiteSpace(result.TextContent), duration: stopwatch.Elapsed);
        logger.LogInformation(result.Title != null ? "Extracted successfully!" : "Extraction failed.");

        return result;
    }

    private async Task<WebscrapeResult> ExtractFromDocumentUrlAsync(string url, string extension)
    {
        try
        {
            using HttpClient httpClient = new();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            using HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using Stream stream = await response.Content.ReadAsStreamAsync();
            using MemoryStream memoryStream = new();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            string text = await ExtractTextFromFileAsync(memoryStream, extension);

            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning("No text could be extracted from document URL '{url}'", url);
                return new WebscrapeResult();
            }

            string fileName = Path.GetFileNameWithoutExtension(new Uri(url).LocalPath);
            string siteName = new Uri(url).Host.Replace("www.", "");

            return new WebscrapeResult
            {
                Title = fileName,
                TextContent = text,
                SiteName = siteName
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to download and extract document from URL '{url}'", url);
            return new WebscrapeResult();
        }
    }
    #endregion
}

public class WebscrapeResult
{
    public string? Title { get; set; }
    public string? TextContent { get; set; }
    public string? Byline { get; set; }
    public string? Excerpt { get; set; }
    public string? SiteName { get; set; }
}

public class OcrResult
{
    public string Text { get; set; } = "";
    public string HeaderFooterText { get; set; } = "";
    public string Model { get; set; } = "";

    /// <summary>
    /// Markdown per page exactly as OCR returned it, so a later change to the cleaning never needs a new OCR run.
    /// Null for text that didn't come from OCR, and in caches written before pages were kept.
    /// </summary>
    public List<string>? Pages { get; set; }

    /// <summary>Where each page starts in Text: page n starts at PageStarts[n - 1]. Null when Pages is</summary>
    public int[]? PageStarts { get; set; }
}
