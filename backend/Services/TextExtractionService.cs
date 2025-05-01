using System.Text;
using KnowledgeBank.Data;
using Microsoft.SemanticKernel.Text;
using Serilog;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace KnowledgeBank.Services;

#pragma warning disable SKEXP0050
// 'Microsoft.SemanticKernel.Text.TextChunker' is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.SKEXP0050


public interface ITextExtractionService
{
    Task<string> ExtractTextFromPdfAsync(Stream pdfStream);
    Task ProcessDocumentAsync(string id, string fileType);
}

public class TextExtractionService : ITextExtractionService
{
    private readonly IAzureBlobService _blobService;
    private readonly ResourceManager _resourceManager;
    private readonly Serilog.ILogger _logger;

    public TextExtractionService(IAzureBlobService blobService, ResourceManager resourceManager)
    {
        _blobService = blobService;
        _resourceManager = resourceManager;
        _logger = Log.ForContext<TextExtractionService>();
    }

    public Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        StringBuilder textBuilder = new();

        // Save original position to restore it later
        long originalPosition = pdfStream.Position;
        pdfStream.Position = 0;

        try
        {
            using (PdfDocument document = PdfDocument.Open(pdfStream))
            {
                foreach (Page page in document.GetPages())
                {
                    string pageText = page.Text;
                    textBuilder.AppendLine(pageText);
                }
            }

            return Task.FromResult(textBuilder.ToString());
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error extracting text from PDF");
            throw;
        }
        finally
        {
            // Restore stream position for potential reuse
            pdfStream.Position = originalPosition;
        }
    }


    public async Task ProcessDocumentAsync(string id, string fileType)
    {
        try
        {
            _logger.Information("Processing PDF document with ID: {Id}", id);

            // Download the blob
            var blobResponse = await _blobService.DownloadBlobAsync(fileType, id);
            if (blobResponse == null)
            {
                _logger.Warning("PDF document with ID {Id} not found in blob storage", id);
                return;
            }

            // Extract text from the PDF
            string extractedText = await ExtractTextFromPdfAsync(blobResponse.Value.FileStream);

            // Chuck the extracted text
            List<string> data = TextChunker.SplitPlainTextLines(extractedText, maxTokensPerLine: 200);

            // Print the chunks
            foreach (var chunk in data)
            {
                _logger.Information("Chunk: {Chunk}", chunk);
            }

            _logger.Information("Successfully extracted text from PDF document with ID: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing PDF document with ID: {Id}", id);
        }
    }
}
