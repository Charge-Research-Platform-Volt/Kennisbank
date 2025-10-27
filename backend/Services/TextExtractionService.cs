using Azure.AI.DocumentIntelligence;
using Azure;
using UglyToad.PdfPig;
using System.Text;
using System.Reflection.Metadata.Ecma335;

namespace KnowledgeBank.Services;

/// <summary>
/// Service for extracting text from various document formats
/// Uses free methods (PdfPig) when possible, falls back to Azure Document Intelligence (read)
/// </summary>
public class TextExtractionService 
{
    private readonly ILogger<TextExtractionService> logger;
    private readonly DocumentIntelligenceClient docIntelligenceClient;
    
    public TextExtractionService(ILogger<TextExtractionService> logger, DocumentIntelligenceClient docIntelligenceClient) 
    {
        this.logger = logger;
        this.docIntelligenceClient = docIntelligenceClient;
    }
    
    /// <summary>
    /// Extracts text from a document stream, choosing the best extraction method based on file type
    /// </summary>
    /// <param name="stream">Document file stream</param>
    /// <param name="fileExtension">File extension (e.g. ".pdf", ".docx")</param>
    /// <returns>Extracted text content</returns>
    public async Task<string> ExtractTextAsync(Stream stream, string fileExtension) 
    {
        fileExtension = fileExtension.ToLowerInvariant();

        logger.LogInformation("Extracting text from {FileType} document", fileExtension);
        
        try 
        {
            return fileExtension switch
            {
                ".pdf" => await ExtractFromPdf(stream),
                ".txt" => await ExtractFromPlainText(stream),
                _      => await ExtractWithAzureDI(stream, fileExtension),
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to extract text from {FileType}", fileExtension);
            throw;
        }
    }
    
    /// <summary>
    /// Extracts text from PDF using free PdfPig. Falls back to Azure DI Read if needed.
    /// </summary>
    /// <param name="stream">The document stream</param>
    /// <returns>Document content</returns>
    private async Task<string> ExtractFromPdf(Stream stream) 
    {
        long originalPosition = stream.Position;
        
        try 
        {
            // Attempt free extraction with PdfPig
            logger.LogInformation("Attempting free PDF text extraction with PdfPig");

            stream.Position = 0;
            var text = ExtractWithPdfPig(stream);

            // Validate extraction quality
            if (!string.IsNullOrWhiteSpace(text) && text.Length > 50)
            {
                // Check for proper spacing - if text has poor spacing, fallback to Azure DI
                if (HasPoorSpacing(text))
                {
                    logger.LogWarning("PdfPig extracted text has poor spacing quality, falling back to Azure DI Read");
                    stream.Position = 0;
                    return await ExtractWithAzureDI(stream, ".pdf");
                }

                logger.LogInformation("PdfPig extraction successful: {Length} characters", text.Length);
                return text;
            }

            // If extraction resulted in very little text, might be scanned/image PDF
            logger.LogWarning("PdfPig extracted minimal text ({Length} chars), falling back to Azure DI Read (OCR)", text?.Length ?? 0);

            stream.Position = 0;
            return await ExtractWithAzureDI(stream, ".pdf");
        }
        catch (Exception e) 
        {
            // PdfPig failed (corrupted PDF, encrypted, etc.) - fallback to Azure DI
            logger.LogWarning(e, "PdfPig extraction failed, falling back to Azure DI Read");

            stream.Position = 0;
            return await ExtractWithAzureDI(stream, ".pdf");
        }
        finally 
        {
            stream.Position = originalPosition;
        }
    }
    
    /// <summary>
    /// Extracts text from PDF using free PdfPig Library
    /// Uses word-based extraction for better spacing handling
    /// </summary>
    /// <param name="stream">The document stream</param>
    /// <returns>Document content</returns>
    private string ExtractWithPdfPig(Stream stream)
    {
        StringBuilder textBuilder = new();

        using (PdfDocument document = PdfDocument.Open(stream))
        {
            foreach (var page in document.GetPages())
            {
                try
                {
                    // Try word-based extraction which can handle spacing better
                    var words = page.GetWords();
                    var pageText = string.Join(" ", words.Select(w => w.Text));

                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        textBuilder.AppendLine(pageText);
                        textBuilder.AppendLine();
                    }
                }
                catch
                {
                    // Fallback to simple text extraction if GetWords() fails
                    var pageText = page.Text;
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        textBuilder.AppendLine(pageText);
                        textBuilder.AppendLine();
                    }
                }
            }
        }

        return textBuilder.ToString();
    }
    
    /// <summary>
    /// Extracts text using Azure Document Intelligence "prebuilt-read" model
    /// Supports PDF, DOCX, XLSX, PPTX, images (with OCR), and more
    /// </summary>
    /// <param name="stream">The document stream</param>
    /// <param name="fileExtension">The document file extension</param>
    /// <returns>Document content</returns>
    private async Task<string> ExtractWithAzureDI(Stream stream, string fileExtension)
    {
        logger.LogInformation("Using Azure DI Read model for {FileType}", fileExtension);

        stream.Position = 0;
        var binaryData = await BinaryData.FromStreamAsync(stream);

        var operation = await docIntelligenceClient.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-read",
            binaryData);

        var extractedText = operation.Value.Content ?? string.Empty;

        logger.LogInformation("Azure DI extracted {Length} characters from {FileType}", extractedText.Length, fileExtension);

        return extractedText;
    }
    
    private async Task<string> ExtractFromPlainText(Stream stream)
    {
        logger.LogInformation("Extracting from plain text document");

        stream.Position = 0;
        using var reader = new StreamReader(stream, leaveOpen: true);
        return await reader.ReadToEndAsync();
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
}