using System.Text;
using KnowledgeBank.Data;
using KnowledgeBank.Services.Storage;
using Microsoft.SemanticKernel.Text;
using Serilog;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace KnowledgeBank.Services;

#pragma warning disable SKEXP0050, SKEXP0001 // 'Microsoft.SemanticKernel.Text.TextChunker' is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.SKEXP0050

public class Tools
{
    private readonly Serilog.ILogger _logger;

    public Tools()
    {
        _logger = Log.ForContext<Tools>();
        _logger.Information("Tools successfully initialized");
    }



    /// <summary>
    /// Splits a large text into smaller chunks for processing with overlap for better context preservation.
    /// </summary>
    /// <param name="extractedText">The text to be split into chunks.</param>
    /// <param name="logChunks">Optional parameter that determines whether to log each chunk. Default is false.</param>
    /// <param name="markdownSplit">Optional parameter that determines whether to split the text as Markdown. Default is false.</param>
    /// <param name="chunkSize">Maximum number of tokens per chunk. Default is 512 for better semantic coherence.</param>
    /// <param name="overlapSize">Number of overlapping tokens between chunks. Default is 128 to preserve context at boundaries.</param>
    /// <returns>A list of strings, where each string represents a chunk of the original text with overlaps.</returns>
    /// <remarks>
    /// This improved chunking strategy:
    /// - Uses larger chunk sizes (512 tokens) for better semantic coherence
    /// - Adds overlap between chunks (128 tokens) to preserve context at boundaries
    /// - Prevents information loss at chunk boundaries
    /// - Improves retrieval quality by ensuring related content stays together
    /// When logging is enabled, each chunk will be logged using the debug level.
    /// </remarks>
    public List<string> SplitTextIntoChunks(string extractedText, bool logChunks = false, bool markdownSplit = false, int chunkSize = 512, int overlapSize = 128)
    {
        // Validate parameters
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            _logger.Warning("Empty text provided for chunking");
            return new List<string>();
        }

        if (overlapSize >= chunkSize)
        {
            _logger.Warning("Overlap size ({Overlap}) must be less than chunk size ({ChunkSize}). Using default values.", overlapSize, chunkSize);
            chunkSize = 512;
            overlapSize = 128;
        }

        List<string> chunks = new List<string>();

        try
        {
            if (markdownSplit)
            {
                // Use Markdown-aware chunking with overlap
                chunks = TextChunker.SplitMarkdownParagraphs(
                    [extractedText],
                    maxTokensPerParagraph: chunkSize,
                    overlapTokens: overlapSize
                );
            }
            else
            {
                // Use plain text chunking with overlap
                chunks = TextChunker.SplitPlainTextParagraphs(
                    [extractedText],
                    maxTokensPerParagraph: chunkSize,
                    overlapTokens: overlapSize
                );
            }

            _logger.Information("Split text into {ChunkCount} chunks (size: {ChunkSize}, overlap: {Overlap})",
                chunks.Count, chunkSize, overlapSize);

            // Log the chunks if required
            if (logChunks)
            {
                for (int i = 0; i < chunks.Count; i++)
                {
                    _logger.Debug("Chunk {Index}/{Total} (length: {Length}): {Preview}...",
                        i + 1, chunks.Count, chunks[i].Length,
                        chunks[i].Length > 100 ? chunks[i].Substring(0, 100) : chunks[i]);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during text chunking. Falling back to simple chunking.");

            // Fallback to simple chunking without overlap
            chunks = markdownSplit
                ? TextChunker.SplitMarkDownLines(extractedText, maxTokensPerLine: chunkSize)
                : TextChunker.SplitPlainTextLines(extractedText, maxTokensPerLine: chunkSize);
        }

        return chunks;
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)