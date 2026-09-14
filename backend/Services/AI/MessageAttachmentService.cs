using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Services.AI;

public class MessageAttachmentService(DatabaseContext db, IStorageService storageService, TextExtractionService textExtractionService, EmbeddingService embeddingService, AttachmentChunkSearchIndexService attachmentChunkSearchIndexService, MistralHttpClient mistralClient, EnvironmentConfig environmentConfig)
{
    private const int MaxInlineAttachmentTokens = 4000;
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);
    private readonly string smallModelName = environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_SMALL_MODEL_NAME);
    private static int EstimateTokens(string text) => Math.Max(1, text.Length / 3);

    public async Task<MessageAttachments> CreatePendingAttachmentAsync(Guid chatId, Guid objectId, string fileName)
    {
        string extension = Path.GetExtension(fileName);

        ObjectDownloadResponse response = await storageService.DownloadObjectAsync(bucketName, objectId.ToString());
        await using var stream = response.Stream;

        string extractedText = await textExtractionService.ExtractTextFromFileAsync(stream, extension);
        bool isChunked = EstimateTokens(extractedText) > MaxInlineAttachmentTokens;

        MessageAttachments attachment = new()
        {
            Id = objectId,
            ChatId = chatId,
            MessageId = null,
            FileName = fileName,
            Extension = extension,
            ExtractedText = isChunked ? null : extractedText,
            IsChunked = isChunked,
            CreatedOn = DateTime.UtcNow
        };

        await db.MessageAttachments.AddAsync(attachment);
        await db.SaveChangesAsync();

        if (isChunked)
        {
            await IndexAttachmentChunksAsync(attachment, extractedText);
        }
        else
        {
            attachment.Description = await GenerateDescriptionAsync([extractedText]);
            await db.SaveChangesAsync();
        }

        return attachment;
    }

    public async Task<bool> DetachAttachmentAsync(Guid chatId, Guid attachmentId)
    {
        MessageAttachments? attachment = await db.MessageAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId && a.ChatId == chatId);
        if (attachment == null) return false;

        attachment.Detached = true;
        await db.SaveChangesAsync();

        return true;
    }

    private async Task IndexAttachmentChunksAsync(MessageAttachments attachment, string extractedText)
    {
        List<string> chunks = IngestionService.SplitTextIntoChunks(extractedText, markdownSplit: true);
        if (chunks.Count == 0) return;

        Task<float[][]> embedTask = embeddingService.GenerateEmbeddings(chunks);
        Task<string> descriptionTask = GenerateDescriptionAsync(chunks);
        await Task.WhenAll(embedTask, descriptionTask);

        float[][] embeddings = embedTask.Result;
        var documents = chunks.Select((chunkText, i) => new AttachmentChunkDocument
        {
            Id = $"{attachment.Id}-{i}",
            AttachmentId = attachment.Id.ToString(),
            ChatId = attachment.ChatId.ToString(),
            ChunkPart = i,
            ChunkText = chunkText,
            Vectors = new Dictionary<string, float[]> { ["default"] = embeddings[i] }
        }).ToList();

        await attachmentChunkSearchIndexService.IndexChunksAsync(documents);

        attachment.Description = descriptionTask.Result;
        await db.SaveChangesAsync();
    }

    private async Task<string> GenerateDescriptionAsync(List<string> textSnippets)
    {
        string excerpt = string.Join("\n\n", textSnippets.Take(2));

        MistralCompletion result = await mistralClient.CompleteAsync(new MistralChatRequest
        {
            Messages = [new { role = "user", content = $"Based on this excerpt from the start of a document, write a one-sentence description of what the document is likely about:\n\n{excerpt}" }],
            Temperature = 0f,
            ReasoningEffort = MistralReasoningEffort.None,
            MaxTokens = 100
        }, modelOverride: smallModelName);

        return result.Content?.Trim() ?? "";
    }
}