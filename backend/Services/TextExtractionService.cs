using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Presentation;
using ClosedXML.Excel;
using System.Text;
using SmartReader;
using PuppeteerSharp;
using KnowledgeBank.Utils;
using KnowledgeBank.Data;
using System.Text.Json;

namespace KnowledgeBank.Services;

/// <summary>
/// Service for extracting text from various document formats
/// Uses free methods when possible, falls back to OCR
/// </summary>
public class TextExtractionService(ILogger<TextExtractionService> logger, EnvironmentConfig environmentConfig, BrowserService browserService)
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
                ".pdf" => await ExtractWithMistralOCR(stream, fileExtension),
                ".txt" => await ExtractFromPlainText(stream),
                ".docx" => ExtractWithOpenXmlDocx(stream),
                ".pptx" => ExtractWithOpenXmlPptx(stream),
                ".xlsx" => ExtractWithClosedXmlXlsx(stream),
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
    
    private async Task<string> ExtractWithMistralOCR(Stream stream, string fileExtension)
    {
        logger.LogInformation("Using Mistral OCR for {FileType}", fileExtension);

        stream.Position = 0;
        using MemoryStream ms = new();
        await stream.CopyToAsync(ms);
        string base64 = Convert.ToBase64String(ms.ToArray());
        string mimeType = Filetype.GetMimeType(fileExtension);
        string dataUri = $"data:{mimeType};base64,{base64}";

        bool isPdf = string.Equals(fileExtension, ".pdf", StringComparison.OrdinalIgnoreCase);

        string apiKey = environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_API_KEY);

        var requestBody = isPdf
            ? (object)new { model = "mistral-ocr-latest", document = new { type = "document_url", document_url = dataUri } }
            : new { model = "mistral-ocr-latest", document = new { type = "image_url", image_url = dataUri } };

        string json = JsonSerializer.Serialize(requestBody);

        using HttpClient httpClient = new();
        using HttpRequestMessage request = new(HttpMethod.Post, environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_ENDPOINT) + "/ocr");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(responseBody);

        StringBuilder sb = new();
        foreach (JsonElement page in doc.RootElement.GetProperty("pages").EnumerateArray())
            sb.AppendLine(page.GetProperty("markdown").GetString());

        string result = sb.ToString();
        logger.LogInformation("Mistral OCR extracted {Length} characters.", result.Length);
        
        return result;
    }

    private async Task<string> ExtractFromPlainText(Stream stream)
    {
        logger.LogInformation("Extracting from plain text document");

        stream.Position = 0;
        using var reader = new StreamReader(stream, leaveOpen: true);
        return await reader.ReadToEndAsync();
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

    /// <summary>
    /// Checks if extracted text has poor spacing quality (e.g., words running together)
    /// </summary>
    /// <param name="text">The extracted text to check</param>
    /// <returns>True if spacing quality is poor, false if acceptable</returns>
    private bool HasPoorSpacing(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        // Remove newlines for analysis
        var textWithoutNewlines = text.Replace("\n", " ").Replace("\r", "");

        // Calculate space ratio (spaces should be at least 10% of text in normal documents)
        int spaceCount = textWithoutNewlines.Count(c => c == ' ');
        double spaceRatio = (double)spaceCount / textWithoutNewlines.Length;

        if (spaceRatio < 0.10)
        {
            logger.LogWarning("Low space ratio detected: {SpaceRatio:P2} (expected > 10%)", spaceRatio);
            return true;
        }

        // Check for abnormally long words (> 50 characters without spaces suggests missing spaces)
        var words = textWithoutNewlines.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int longWordCount = words.Count(w => w.Length > 50);
        double longWordRatio = words.Length > 0 ? (double)longWordCount / words.Length : 0;

        if (longWordRatio > 0.30)
        {
            logger.LogWarning("High long-word ratio detected: {LongWordRatio:P2} (expected < 30%)", longWordRatio);
            return true;
        }

        return false;
    }
    
    #endregion
    
    #region Web Text Extraction

    public async Task<ReadabilityResult> ExtractTextFromWebAsync(string url)
    {
        logger.LogInformation("Extracting text from webpage with url '{url}'", url);

        // Handle direct document URLs - download and extract as file instead of using a headless browser
        if (Filetype.IsDocumentUrl(url))
        {
            logger.LogInformation("URL points to a document file, downloading and extracting directly");
            return await ExtractFromDocumentUrlAsync(url, Filetype.GetDocumentUrlExtension(url));
        }

        ReadabilityResult result = await ExtractWithSmartReaderAsync(url);
        
        if (result.Title != null) 
        {
            logger.LogInformation("Extracted successfully.");
            return result;
        }

        logger.LogInformation("SmartReader failed, trying with Puppeteer Headless Browser");
        result = await ExtractWithPuppeteerAsync(url);
        
        if (result.Title != null) 
        {
            logger.LogInformation("Extracted successfully.");
            return result;
        }

        logger.LogInformation("Extraction failed.");
        return new ReadabilityResult();
    }
    
    private async Task<ReadabilityResult> ExtractWithSmartReaderAsync(string url) 
    {
        logger.LogInformation("Extracting webpage with SmartReader...");

        try 
        {
            // Fetch article using SmartReader (Mozilla Readability wrapper)
            Article article = await Reader.ParseArticleAsync(url);

            logger.LogInformation("Article readable: {readable}", article.IsReadable);
            
            if (article.IsReadable) 
            {
                ReadabilityResult result = new()
                {
                    Title = article.Title,
                    TextContent = article.TextContent,
                    Byline = article.Byline,
                    Excerpt = article.Excerpt,
                    SiteName = article.SiteName ?? new Uri(url).Host.Replace("www.", "")
                };
                
                return result;
            }
        }
        catch (Exception e) 
        {
            logger.LogWarning("SmartReader threw an exception: {message}", e.Message);
        }

        return new ReadabilityResult();
    }
    
    private async Task<ReadabilityResult> ExtractWithPuppeteerAsync(string url) 
    {
        logger.LogInformation("Extracting webpage with Puppeteer...");

        // Get shared headless browser instance and create new page
        IBrowser browser = await browserService.GetBrowserAsync();
        IPage page = await browser.NewPageAsync();
        
        try 
        {
            // Go to the url
            await page.GoToAsync(url, new NavigationOptions
            {
                WaitUntil = [ WaitUntilNavigation.Networkidle0 ],
                Timeout = 15000 // 15 seconds
            });

            try 
            {
                // Add Mozilla readability directly in the browser
                await page.EvaluateExpressionAsync(await browserService.GetReadabilityScriptAsync());

                ReadabilityResult? result = await page.EvaluateFunctionAsync<ReadabilityResult>(@"
                    () => {
                        const article = new Readability(document.cloneNode(true)).parse();
                        if (!article) return null;
                        return {
                            title: article.title,
                            textContent: article.textContent,
                            byline: article.byline,
                            excerpt: article.excerpt
                        };
                    }
                ");
                
                if (result != null) 
                {
                    result.SiteName = new Uri(url).Host.Replace("www.", "");
                    return result;
                }
            }
            catch (Exception e) 
            {
                logger.LogInformation("Failed to inject Readability.js: {message}", e.Message);
            }
            
            try 
            {
                // Retrieve inner text as fallback, which has a bit more noise, but is better then raw HTML
                logger.LogInformation("Retrieving inner text...");

                return new ReadabilityResult
                {
                    Title = await page.EvaluateFunctionAsync<string>("() => document.title"),
                    TextContent = await page.EvaluateFunctionAsync<string>("() => document.body.innerText"),
                    SiteName = new Uri(url).Host.Replace("www.", ""),
                    Byline = await page.EvaluateFunctionAsync<string>(@"
                        () => document.querySelector('meta[name=""author""]')?.content
                            || document.querySelector('meta[property=""article:author""]')?.content
                            || null"),
                    Excerpt = await page.EvaluateFunctionAsync<string>(@"
                        () => document.querySelector('meta[name=""description""]')?.content
                            || document.querySelector('meta[property=""og:description""]')?.content
                            || null")
                };
            }
            catch (Exception e) 
            {
                logger.LogWarning("Failed to retrieve inner text using puppeteer: {message}", e.Message);
            }

            return new ReadabilityResult();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task<ReadabilityResult> ExtractFromDocumentUrlAsync(string url, string extension)
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
                return new ReadabilityResult();
            }

            string fileName = Path.GetFileNameWithoutExtension(new Uri(url).LocalPath);
            string siteName = new Uri(url).Host.Replace("www.", "");

            return new ReadabilityResult
            {
                Title = fileName,
                TextContent = text,
                SiteName = siteName
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to download and extract document from URL '{url}'", url);
            return new ReadabilityResult();
        }
    }
    #endregion
}

public class ReadabilityResult
{
    public string? Title { get; set; }
    public string? TextContent { get; set; }
    public string? Byline { get; set; }
    public string? Excerpt { get; set; }
    public string? SiteName { get; set; }
}
