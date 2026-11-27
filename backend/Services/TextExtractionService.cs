using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Presentation;
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
    {
        fileExtension = fileExtension.ToLowerInvariant();

        logger.LogInformation("Extracting text from {FileType} document", fileExtension);

        // Ensure stream is seekable — HTTP streams are not
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
                ".pdf" => (await ExtractWithMistralOCR(stream, fileExtension)).Text,
                ".txt" => await ExtractFromPlainText(stream),
                ".docx" => ExtractWithOpenXmlDocx(stream),
                ".odt" => ExtractOdt(stream),
                ".pptx" => ExtractWithOpenXmlPptx(stream),
                ".xlsx" => ExtractWithClosedXmlXlsx(stream),
                ".html" => await ExtractHtmlFileAsync(stream),
                _ when Filetype.SupportedImage(fileExtension) => (await ExtractWithMistralOCR(stream, fileExtension)).Text,
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
        if (!isOcr) return new OcrResult { Text = await ExtractTextFromFileAsync(stream, fileExtension) };

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

    private static string ExtractWithOpenXmlDocx(Stream stream)
    {
        stream.Position = 0;
        using WordprocessingDocument doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;

        if (body == null) return string.Empty;

        StringBuilder sb = new();

        foreach (var para in body.Descendants<Paragraph>())
        {
            string text = para.InnerText;

            if (!string.IsNullOrWhiteSpace(text))
                sb.AppendLine(text);
        }

        return sb.ToString();
    }

    private static readonly XNamespace OdfText = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

    /// <summary>
    /// OpenDocument text (LibreOffice's own format) is a zip whose content.xml holds the text, so like .docx it's
    /// read directly: free and exact, no conversion or OCR. One line per paragraph or heading, table cells included.
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

        StringBuilder sb = new();

        foreach (XElement block in content.Descendants().Where(e => e.Name == OdfText + "p" || e.Name == OdfText + "h"))
        {
            StringBuilder line = new();
            AppendOdtText(block, line);
            string text = line.ToString().Trim();

            if (!string.IsNullOrWhiteSpace(text))
                sb.AppendLine(text);
        }

        return sb.ToString();
    }

    private static void AppendOdtText(XElement element, StringBuilder line)
    {
        foreach (XNode node in element.Nodes())
        {
            switch (node)
            {
                // Runs of whitespace in ODF text count as one space; extra spaces are written as <text:s>
                case XText text:
                    line.Append(Regex.Replace(text.Value, @"\s+", " "));
                    break;
                case XElement child when child.Name == OdfText + "s":
                    line.Append(' ', (int?)child.Attribute(OdfText + "c") ?? 1);
                    break;
                case XElement child when child.Name == OdfText + "tab":
                    line.Append('\t');
                    break;
                case XElement child when child.Name == OdfText + "line-break":
                    line.Append('\n');
                    break;
                // A footnote's paragraphs sit inside this one; they're read as their own blocks, so skip them here
                case XElement child when child.Name == OdfText + "note":
                    break;
                case XElement child:
                    AppendOdtText(child, line);
                    break;
            }
        }
    }

    private static string ExtractWithOpenXmlPptx(Stream stream)
    {
        stream.Position = 0;
        using PresentationDocument pres = PresentationDocument.Open(stream, false);
        var slideIds = pres.PresentationPart?.Presentation?.SlideIdList?.ChildElements;

        if (slideIds == null) return string.Empty;

        StringBuilder sb = new();

        foreach (SlideId slideId in slideIds.Cast<SlideId>())
        {
            var slidePart = (SlidePart?)pres.PresentationPart?.GetPartById(slideId.RelationshipId!);

            if (slidePart == null || slidePart.Slide == null) continue;

            foreach (var text in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                if (!string.IsNullOrWhiteSpace(text.Text))
                    sb.AppendLine(text.Text);
        }

        return sb.ToString();
    }
    
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
