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

namespace KnowledgeBank.Services;

/// <summary>
/// Service for extracting text from various document formats
/// Uses free methods when possible, falls back to OCR
/// </summary>
public class TextExtractionService(ILogger<TextExtractionService> logger, EnvironmentConfig environmentConfig, WebscrapeClient webscrapeClient, IStorageService storageService, MistralHttpClient mistralClient)
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
            if (cached != null) return cached;
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

        MistralOcrResult ocrResult = await mistralClient.RunOcrAsync(ms.ToArray(), fileExtension, resolvedModel, isPdf);

        StringBuilder sb = new();
        HashSet<string> headerFooterLines = new(StringComparer.OrdinalIgnoreCase);

        foreach (MistralOcrPage page in ocrResult.Pages)
        {
            if (!string.IsNullOrWhiteSpace(page.Markdown))
                sb.AppendLine(page.Markdown);
            
            if (!string.IsNullOrWhiteSpace(page.Header)) headerFooterLines.Add(page.Header.Trim());
            if (!string.IsNullOrWhiteSpace(page.Footer)) headerFooterLines.Add(page.Footer.Trim());
        }

        string result = CleanOcrMarkdown(sb.ToString());
        logger.LogInformation("Mistral OCR extracted {Length} characters.", result.Length);

        return new OcrResult { Text = result, HeaderFooterText = string.Join("\n", headerFooterLines), Model = ocrResult.Model };
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

            logger.LogInformation("OCR cache hit for {FileId} (model: {Model})", fileId, cached.Model);
            return cached;
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

    private static string CleanOcrMarkdown(string text)
    {
        text = Regex.Replace(text, @"!\[[^\]]*\]\([^\)]*\)", "");
        text = Regex.Replace(text, @"\^\{\}\[\]", "");
        text = Regex.Replace(text, @"(?m)^#+\s*$", "");

        text = System.Net.WebUtility.HtmlDecode(text);

        return text;
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

        WebscrapeResult result = await webscrapeClient.ExtractFromUrlAsync(url);
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
}
