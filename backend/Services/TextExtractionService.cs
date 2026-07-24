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
using System.Text.RegularExpressions;
using KnowledgeBank.Services.Storage;

namespace KnowledgeBank.Services;

/// <summary>
/// Service for extracting text from various document formats
/// Uses free methods when possible, falls back to OCR
/// </summary>
public class TextExtractionService(ILogger<TextExtractionService> logger, EnvironmentConfig environmentConfig, BrowserService browserService, IStorageService storageService)
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
    private const string ConfiguredOcrModel = "mistral-ocr-latest";

    private async Task<string> ResolveOcrModelIdAsync()
    {
        if (ConfiguredOcrModel != "mistral-ocr-latest")
            return ConfiguredOcrModel;

        try
        {
            string apiKey = environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_API_KEY);
            string mistralEndpoint = environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_ENDPOINT);

            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            using HttpResponseMessage response = await httpClient.GetAsync(mistralEndpoint + "/models");
            response.EnsureSuccessStatusCode();
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            foreach (JsonElement model in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                if (model.TryGetProperty("aliases", out JsonElement aliases) &&
                    aliases.EnumerateArray().Any(a => a.GetString() == ConfiguredOcrModel))
                {
                    return model.GetProperty("id").GetString() ?? ConfiguredOcrModel;
                }
            }

            logger.LogWarning("Could not find a model with alias {Alias} in Mistral's model list, falling back to alias", ConfiguredOcrModel);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve {Alias} to a pinned model id, falling back to alias", ConfiguredOcrModel);
        }

        return ConfiguredOcrModel;
    }

    private async Task<OcrResult> ExtractWithMistralOCR(Stream stream, string fileExtension, string? model = null)
    {
        logger.LogInformation("Using Mistral OCR for {FileType}", fileExtension);
        string resolvedModel = model ?? await ResolveOcrModelIdAsync();

        stream.Position = 0;
        using MemoryStream ms = new();
        await stream.CopyToAsync(ms);

        bool isPdf = string.Equals(fileExtension, ".pdf", StringComparison.OrdinalIgnoreCase);
        string apiKey = environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_API_KEY);
        string mistralEndpoint = environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_ENDPOINT);

        using HttpClient httpClient = new() { Timeout = TimeSpan.FromMinutes(5) };
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        // Step 1: Upload file to Mistral Files API
        ms.Position = 0;
        using MultipartFormDataContent uploadForm = new();
        uploadForm.Add(new StreamContent(ms), "file", $"document{fileExtension}");
        uploadForm.Add(new StringContent("ocr"), "purpose");

        using HttpRequestMessage uploadRequest = new(HttpMethod.Post, mistralEndpoint + "/files");
        uploadRequest.Content = uploadForm;

        using HttpResponseMessage uploadResponse = await httpClient.SendAsync(uploadRequest);
        string uploadResponseBody = await uploadResponse.Content.ReadAsStringAsync();
        logger.LogInformation("Mistral file upload response ({Status}): {Body}", (int)uploadResponse.StatusCode, uploadResponseBody);
        uploadResponse.EnsureSuccessStatusCode();

        string mistralFileId = JsonDocument.Parse(uploadResponseBody).RootElement.GetProperty("id").GetString()!;
        logger.LogInformation("Uploaded file to Mistral Files API: {FileId}", mistralFileId);

        // Step 2: Get signed download URL
        using HttpRequestMessage signedUrlRequest = new(HttpMethod.Get, $"{mistralEndpoint}/files/{mistralFileId}/url?expiry=1");
        using HttpResponseMessage signedUrlResponse = await httpClient.SendAsync(signedUrlRequest);
        string signedUrlResponseBody = await signedUrlResponse.Content.ReadAsStringAsync();
        logger.LogInformation("Mistral signed URL response ({Status}): {Body}", (int)signedUrlResponse.StatusCode, signedUrlResponseBody);
        signedUrlResponse.EnsureSuccessStatusCode();
        string fileUrl = JsonDocument.Parse(signedUrlResponseBody).RootElement.GetProperty("url").GetString()!;
        logger.LogInformation("Got signed URL for OCR");

        // Step 3: Run OCR using signed URL
        var requestBody = isPdf
            ? (object)new { model = resolvedModel, document = new { type = "document_url", document_url = fileUrl }, extract_header = true, extract_footer = true }
            : new { model = resolvedModel, document = new { type = "image_url", image_url = fileUrl }, extract_header = true, extract_footer = true };

        using HttpRequestMessage ocrRequest = new(HttpMethod.Post, mistralEndpoint + "/ocr");
        ocrRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await httpClient.SendAsync(ocrRequest);
        string responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            logger.LogError("Mistral OCR failed ({Status}): {Body}", (int)response.StatusCode, responseBody);
        response.EnsureSuccessStatusCode();
        using JsonDocument doc = JsonDocument.Parse(responseBody);

        StringBuilder sb = new();
        HashSet<string> headerFooterLines = new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement page in doc.RootElement.GetProperty("pages").EnumerateArray())
        {
            string? pageText = page.GetProperty("markdown").GetString();

            if (!string.IsNullOrWhiteSpace(pageText))
                sb.AppendLine(pageText);

            if (page.TryGetProperty("header", out JsonElement header) && header.ValueKind == JsonValueKind.String)
            {
                string? text = header.GetString();
                if (!string.IsNullOrWhiteSpace(text)) headerFooterLines.Add(text.Trim());
            }

            if (page.TryGetProperty("footer", out JsonElement footer) && footer.ValueKind == JsonValueKind.String)
            {
                string? text = footer.GetString();
                if (!string.IsNullOrWhiteSpace(text)) headerFooterLines.Add(text.Trim());
            }
        }

        string result = CleanOcrMarkdown(sb.ToString());
        string responseModel = doc.RootElement.GetProperty("model").GetString() ?? "";
        logger.LogInformation("Mistral OCR extracted {Length} characters.", result.Length);

        // Step 4: Delete uploaded file from Mistral (avoid storage charges)
        try
        {
            using HttpRequestMessage deleteRequest = new(HttpMethod.Delete, $"{mistralEndpoint}/files/{mistralFileId}");
            await httpClient.SendAsync(deleteRequest);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete Mistral file {FileId} after OCR", mistralFileId);
        }

        return new OcrResult { Text = result, HeaderFooterText = string.Join("\n", headerFooterLines), Model = responseModel };
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

public class OcrResult
{
    public string Text { get; set; } = "";
    public string HeaderFooterText { get; set; } = "";
    public string Model { get; set; } = "";
}
