using System.Text;
using KnowledgeBank.Data;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Text;
using Serilog;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace KnowledgeBank.Services;

#pragma warning disable SKEXP0050, SKEXP0001 // 'Microsoft.SemanticKernel.Text.TextChunker' is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.SKEXP0050


interface ITools
{
    Task<string> ExtractTextAsync(string fileType, Guid resourceId, IAzureBlobService blobService);
    List<string> SplitTextIntoChunks(string extractedText, bool logChunks = false);
}


public class Tools : ITools
{
    private readonly Serilog.ILogger _logger;

    public Tools()
    {
        _logger = Log.ForContext<Tools>();
        _logger.Information("Tools successfully initialized");
    }



    /// <summary>
    /// Extracts text content from a PDF document stream.
    /// </summary>
    /// <param name="pdfStream">The stream containing PDF data to extract text from.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the extracted text as a string.</returns>
    /// <remarks>
    /// This method preserves the original stream position by restoring it after reading.
    /// </remarks>
    /// <exception cref="Exception">Thrown when text extraction fails. The error is logged before being rethrown.</exception>
    private Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
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
            _logger.Error(ex, "Error extracting text from PDF"); throw;
        }
        finally
        {
            // Restore stream position for potential reuse
            pdfStream.Position = originalPosition;
        }
    }



    /// <summary>
    /// Extracts text content from a file stored in Azure Blob Storage.
    /// </summary>
    /// <param name="fileType">The type of file to be processed (e.g., "pdf").</param>
    /// <param name="resourceId">The unique identifier for the file in blob storage.</param>
    /// <param name="blobService">The Azure Blob Service implementation used to retrieve the file.</param>
    /// <returns>A string containing the extracted text from the document.</returns>
    /// <exception cref="Exception">
    /// Thrown when:
    /// - The document cannot be found in blob storage.
    /// - The text extraction process encounters an error.
    /// </exception>
    /// <remarks>
    /// This method currently handles PDF documents using the ExtractTextFromPdfAsync method.
    /// </remarks>
    public async Task<string> ExtractTextAsync(string fileType, Guid resourceId, IAzureBlobService blobService)
    {
        try
        {
            _logger.Information("Processing Text Extraction for PDF document with ID: {Id}", resourceId);

            // Download the blob
            var blobResponse = await blobService.DownloadBlobAsync(fileType, resourceId.ToString());
            if (blobResponse == null)
            {
                _logger.Warning("Document with ID {Id} not found in blob storage", resourceId);
                throw new Exception($"Document with ID {resourceId} not found in blob storage");
            }


            // Extract text from the PDF
            _logger.Information("Successfully extracted text from PDF document with ID: {Id}", resourceId);
            return await ExtractTextFromPdfAsync(blobResponse.Value.FileStream);
        }

        catch (Exception exception)
        {
            _logger.Error(exception, "Error processing PDF document with ID: {Id}", resourceId);
            throw new Exception($"Error processing PDF document with ID: {resourceId}", exception);
        }
    }



    /// <summary>
    /// Splits a large text into smaller chunks for processing.
    /// </summary>
    /// <param name="extractedText">The text to be split into chunks.</param>
    /// <param name="logChunks">Optional parameter that determines whether to log each chunk. Default is false.</param>
    /// <returns>A list of strings, where each string represents a chunk of the original text.</returns>
    /// <remarks>
    /// This method uses TextChunker to split the text with a maximum of 100 tokens per line.
    /// When logging is enabled, each chunk will be logged using the debug level.
    /// </remarks>
    public List<string> SplitTextIntoChunks(string extractedText, bool logChunks = false)
    {
        // Split the extracted text into chunks
        List<string> data = TextChunker.SplitPlainTextLines(extractedText, maxTokensPerLine: 100);

        // Log the chunks if required
        if (logChunks)
        {
            foreach (var chunk in data)
            {
                _logger.Debug("Chunk: {Chunk}", chunk);
            }
        }

        return data;
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)