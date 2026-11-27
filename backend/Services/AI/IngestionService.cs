using KnowledgeBank.Models;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Vector;
using KnowledgeBank.Utils;
using Microsoft.SemanticKernel.Text;
using Serilog;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using KnowledgeBank.Hubs;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Models.ProjectDocuments;
using System.Text.RegularExpressions;

namespace KnowledgeBank.Services.AI;

#pragma warning disable SKEXP0050, SKEXP0001

public class IngestionService(
    IVectorStore vectorStore, 
    TextExtractionService textExtractionService, 
    ResourceService resourceService, 
    PersonService personService, 
    OrganisationService organisationService, 
    EnvironmentConfig environmentConfig, 
    MistralStatusService mistralStatusService, 
    IHubContext<AppHub, IAppHubClient> hubContext, 
    IStorageService storageService,
    ProjectDocumentService projectDocumentService,
    DocumentConversionService documentConversionService
    )
{
    private readonly Serilog.ILogger logger = Log.ForContext<IngestionService>();
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    public const int MaxChunkTokens = 512;
    private const int MaxContextLineChars = 200;
    private static readonly Regex MarkdownHeading = new(@"^#{1,6}[ \t]+(.+?)[ \t#]*$", RegexOptions.Multiline | RegexOptions.Compiled);

    public async Task RunResourcePipelineAsync(Guid id, string? fileType = null, Stream? fileStream = null)
    {
        logger.Information("Resource pipeline started for ID: {Id}", id);

        try
        {
            await SetResourceEmbeddingStatusAsync(id, EmbeddingStatus.Processing);

            var (richMetadata, resource) = await BuildRichMetadataChunkAsync(id);

            if (string.IsNullOrWhiteSpace(richMetadata) || resource == null)
                throw new InvalidOperationException($"Resource {id} could not be found or has no metadata to embed.");

            ResourceText? content = await GetContentAsync(resource, fileType, fileStream);

            List<ResourceChunkInput> chunks = [new(richMetadata, ChunkType.MetaData, 0)];
            if (content != null)
                chunks.AddRange(BuildContentChunks(resource.Title, content.Text, firstPart: 1));

            await vectorStore.ReplaceResourcePointsAsync(id, chunks, content);

            await SetResourceEmbeddingStatusAsync(id, EmbeddingStatus.Completed);
            logger.Information("Resource pipeline completed for ID: {Id}", id);
        } catch (Exception ex)
        {
            logger.Error(ex, "Resource pipeline failed for ID: {Id}", id);
            await SetResourceEmbeddingStatusAsync(id, EmbeddingStatus.Failed, DescribeFailure(ex));
        }
    }

    /// <summary>
    /// The resource's full text, or null when it has none (metadata only). A file is extracted again (OCR through its
    /// cache). A website reuses its stored text and is only scraped when there is none yet, so a re-embed doesn't depend
    /// on the page still being online and unchanged. When extraction comes back empty, the stored text is kept, so a
    /// failed extraction never wipes content that was indexed before.
    /// </summary>
    private async Task<ResourceText?> GetContentAsync(Resource resource, string? fileType, Stream? fileStream)
    {
        bool isWebsite = resource.FileType == "website" && !string.IsNullOrEmpty(resource.SourceUrl);
        if (fileStream == null && !isWebsite) return null;

        ResourceText? stored = await vectorStore.GetResourceTextAsync(resource.Id);
        if (fileStream == null && stored != null) return stored;

        string? text = null;
        int[]? pageStarts = null;

        if (fileStream != null)
        {
            string ext = fileType != null ? (fileType.StartsWith('.') ? fileType : $".{fileType}") : ".pdf";
            OcrResult ocrResult = await textExtractionService.ExtractOcrResultFromFileAsync(fileStream, ext, bucketName, resource.Id.ToString());
            text = ocrResult.Text;
            pageStarts = ocrResult.PageStarts;
        }
        else
        {
            WebscrapeResult result = await textExtractionService.ExtractTextFromWebAsync(resource.SourceUrl!);
            text = result.TextContent;
        }

        if (!string.IsNullOrWhiteSpace(text))
            return new ResourceText { ResourceId = resource.Id, Text = text, PageStarts = pageStarts, ExtractedAt = DateTime.UtcNow };

        if (stored != null)
        {
            logger.Warning("No text extracted for resource {Id}, keeping its stored text", resource.Id);
            return stored;
        }

        // Indexed before full texts were stored: rebuild a text from the old chunks, for the same reason.
        // Can go once every resource has a stored text
        List<string> oldChunks = await vectorStore.GetContentChunkTextsAsync(resource.Id);
        if (oldChunks.Count > 0)
        {
            logger.Warning("No text extracted for resource {Id}, rebuilding it from {Count} stored chunks", resource.Id, oldChunks.Count);
            return new ResourceText { ResourceId = resource.Id, Text = string.Join("\n\n", oldChunks), ExtractedAt = DateTime.UtcNow };
        }

        logger.Warning("No text extracted for resource {Id}", resource.Id);
        return null;
    }

    /// <summary>
    /// Cuts the text into chunks and gives each one context for embedding and keyword search: the document title and
    /// the nearest markdown heading at or above where the chunk's own text starts (after its overlap with the previous
    /// chunk). The positions are those of the bare chunk in the text, so what's shown later can come straight from the
    /// stored text, without this context.
    /// </summary>
    internal static List<ResourceChunkInput> BuildContentChunks(string title, string text, int firstPart)
    {
        List<(int Position, string Heading)> headings = [.. MarkdownHeading.Matches(text).Select(m => (m.Index, m.Groups[1].Value.Trim()))];
        string? documentLine = string.IsNullOrWhiteSpace(title) ? null : $"Document: {Truncate(title.Trim(), MaxContextLineChars)}";

        List<ResourceChunkInput> chunks = [];
        int headingIndex = -1;
        int previousEnd = 0;

        foreach (ChunkSpan span in DocumentChunker.Split(text))
        {
            // The section is taken where the chunk's own text starts, just after the previous chunk, not at its overlap.
            // Otherwise the first chunk of a section, which usually opens in the previous section's tail, gets that heading
            int anchor = previousEnd;
            while (anchor < span.End && char.IsWhiteSpace(text[anchor])) anchor++;
            anchor = Math.Clamp(anchor, span.Start, span.End - 1);
            previousEnd = span.End;

            while (headingIndex + 1 < headings.Count && headings[headingIndex + 1].Position <= anchor)
                headingIndex++;

            string? sectionLine = headingIndex >= 0 ? $"Section: {Truncate(headings[headingIndex].Heading, MaxContextLineChars)}" : null;
            string context = string.Join("\n", new[] { documentLine, sectionLine }.Where(l => l != null));
            string chunkText = text[span.Start..span.End];

            chunks.Add(new ResourceChunkInput(
                context.Length > 0 ? $"{context}\n\n{chunkText}" : chunkText,
                ChunkType.ContentText,
                firstPart + chunks.Count,
                span.Start,
                span.End
            ));
        }

        return chunks;
    }

    private static string Truncate(string value, int maxChars) => value.Length <= maxChars ? value : value[..maxChars].TrimEnd() + "...";

    /// <summary>
    /// Runs the resource pipeline for an existing resource, fetching its file from storage when it has one.
    /// A failure to load the resource or its file is recorded as a failed attempt, same as a pipeline failure.
    /// </summary>
    public async Task RunResourcePipelineFromStorageAsync(Guid id)
    {
        try
        {
            Resource? resource = await resourceService.GetByIdAsync(id);
            if (resource == null) return;

            if (resource.FileType == "document" && resource.FileExt != null)
            {
                ObjectDownloadResponse download = await storageService.DownloadObjectAsync(bucketName, id.ToString());
                await using Stream downloadStream = download.Stream;
                using MemoryStream memoryStream = new();
                await downloadStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                await RunResourcePipelineAsync(id, resource.FileExt, memoryStream);
            }
            else
            {
                await RunResourcePipelineAsync(id);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to load resource {Id} for its pipeline", id);
            await SetResourceEmbeddingStatusAsync(id, EmbeddingStatus.Failed, ex.Message);
        }
    }

    public async Task RunEntityPipelineAsync(Guid id, string entityType, string chunk)
    {
        logger.Information("Entity pipeline started for entity ID: {Id}", id);

        try
        {
            await SetEntityEmbeddingStatusAsync(id, entityType, EmbeddingStatus.Processing);

            await vectorStore.ReplaceEntityPointsAsync(id, entityType, [(chunk, ChunkType.MetaData, 0)]);

            await SetEntityEmbeddingStatusAsync(id, entityType, EmbeddingStatus.Completed);
            logger.Information("Entity pipeline completed for entity ID: {Id}", id);
        } catch (Exception ex)
        {
            logger.Error(ex, "Entity pipeline failed for ID: {Id}", id);
            await SetEntityEmbeddingStatusAsync(id, entityType, EmbeddingStatus.Failed, ex.Message);
        }
    }

    public Task RunEntityPipelineAsync(Person person)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(person.Description)) sb.AppendLine($"Description: {person.Description}").AppendLine();
        sb.AppendLine($"Person: {person.Name}");
        if (person.Aliases.Count > 0) sb.AppendLine($"Also known as: {string.Join(", ", person.Aliases)}");
        if (!string.IsNullOrWhiteSpace(person.Occupation)) sb.AppendLine($"Occupation: {person.Occupation}");
        if (!string.IsNullOrWhiteSpace(person.EmailAddress)) sb.AppendLine($"Email: {person.EmailAddress}");

        return RunEntityPipelineAsync(person.Id, "person", sb.ToString());
    }

    public Task RunEntityPipelineAsync(Organisation organisation)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(organisation.Description)) sb.AppendLine($"Description: {organisation.Description}").AppendLine();
        sb.AppendLine($"Organisation: {organisation.Name}");
        if (organisation.Aliases.Count > 0) sb.AppendLine($"Also known as: {string.Join(", ", organisation.Aliases)}");
        if (!string.IsNullOrWhiteSpace(organisation.Website)) sb.AppendLine($"Website: {organisation.Website}");
        if (!string.IsNullOrWhiteSpace(organisation.EmailAddress)) sb.AppendLine($"Email: {organisation.EmailAddress}");

        return RunEntityPipelineAsync(organisation.Id, "organisation", sb.ToString());
    }

    public async Task RunPersonEntityPipelineAsync(Guid id)
    {
        Person? person = await personService.GetByIdAsync(id) ?? throw new InvalidOperationException($"Person {id} not found");
        await RunEntityPipelineAsync(person);
    }

    public async Task RunOrganisationEntityPipelineAsync(Guid id)
    {
        Organisation? organisation = await organisationService.GetByIdAsync(id) ?? throw new InvalidOperationException($"Organisation {id} not found");
        await RunEntityPipelineAsync(organisation);
    }

    public async Task<bool> UpdateResourceMetadataAsync(Guid id)
    {
        logger.Information("Updating metadata for resource {Id}", id);

        var (richMetadata, _) = await BuildRichMetadataChunkAsync(id);

        if (string.IsNullOrEmpty(richMetadata)) return false;

        return await vectorStore.UpdateMetadataPointAsync(id, richMetadata);
    }
    
    public static List<string> SplitTextIntoChunks(string text, bool markdownSplit = false, int chunkSize = MaxChunkTokens, int overlapSize = 128)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        if (overlapSize >= chunkSize) { chunkSize = MaxChunkTokens; overlapSize = 128; }

        try
        {
            return markdownSplit
                ? TextChunker.SplitMarkdownParagraphs([text], maxTokensPerParagraph: chunkSize, overlapTokens: overlapSize)
                : TextChunker.SplitPlainTextParagraphs([text], maxTokensPerParagraph: chunkSize, overlapTokens: overlapSize);
        }
        catch
        {
            return markdownSplit
                ? TextChunker.SplitMarkDownLines(text, maxTokensPerLine: chunkSize)
                : TextChunker.SplitPlainTextLines(text, maxTokensPerLine: chunkSize);
        }
    }

    private async Task<(string Metadata, Resource? Resource)> BuildRichMetadataChunkAsync(Guid id)
    {
        try
        {
            var resource = await resourceService.GetByIdAsync(id, includeRelations: true);

            if (resource == null)
            {
                logger.Warning("Resource {Id} not found for metadata enrichtment", id);
                return (string.Empty, null);
            }

            var authors = resource.ResourceAuthorRelations?.Where(r => r.Author != null).Select(r => r.Author!.Name).ToList();
            var tags = resource.ResourceTagRelations?.Where(r => r.Tag != null).Select(r => r.Tag!.Name).ToList();
            var orgs = resource.ResourceOrganisationRelations?.Where(r => r.Organisation != null).Select(r => r.Organisation!.Name).ToList();
            var regions = resource.ResourceRegionRelations?.Where(r => r.Region != null).Select(r => r.Region!.Name).ToList();

            var sb = new StringBuilder();

            // Lead with description since it is the most semantically rich field, anchors the embedding
            if (!string.IsNullOrEmpty(resource.Description)) sb.AppendLine(resource.Description).AppendLine();

            // Named entities: high retrieval value
            if (!string.IsNullOrEmpty(resource.Title)) sb.AppendLine($"Title: {resource.Title}");
            if (authors?.Count > 0) sb.AppendLine($"Authors: {string.Join(", ", authors)}");
            if (orgs?.Count > 0) sb.AppendLine($"Organizations: {string.Join(", ", orgs)}");
            if (tags?.Count > 0) sb.AppendLine($"Tags: {string.Join(", ", tags)}");
            if (regions?.Count > 0) sb.AppendLine($"Regions: {string.Join(", ", regions)}");
            if (resource.Journal != null) sb.AppendLine($"Journal: {resource.Journal.Name}");

            // Supporting context
            if (resource.ResourceType != null) sb.AppendLine($"Type: {resource.ResourceType.Name}");
            if (resource.PublicationDate.HasValue) sb.AppendLine($"Publication Date: {resource.PublicationDate.Value:yyyy-MM-dd}");
            if (!string.IsNullOrEmpty(resource.Note)) sb.AppendLine($"Note: {resource.Note}");

            return (sb.ToString(), resource);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to build rich metadata for resource {Id}", id);
            return (string.Empty, null);
        }
    }

    private async Task SetResourceEmbeddingStatusAsync(Guid id, EmbeddingStatus status, string? error = null)
    {
        await resourceService.UpdateAsync(id, r =>
        {
            r.EmbeddingStatus = status;
            r.EmbeddingError = error;

            if (status is EmbeddingStatus.Processing or EmbeddingStatus.Failed) r.EmbeddingLastAttempt = DateTime.UtcNow;
            if (status == EmbeddingStatus.Failed) r.EmbeddingFailures++;
            if (status == EmbeddingStatus.Completed) r.EmbeddingFailures = 0;
        });

        await BroadcastEmbeddingStatusAsync(id, status);
    }

    private async Task SetEntityEmbeddingStatusAsync(Guid id, string entityType, EmbeddingStatus status, string? error = null)
    {
        void Apply(Entity e)
        {
            e.EmbeddingStatus = status;
            e.EmbeddingError = error;

            if (status is EmbeddingStatus.Processing or EmbeddingStatus.Failed) e.EmbeddingLastAttempt = DateTime.UtcNow;
            if (status == EmbeddingStatus.Failed) e.EmbeddingFailures++;
            if (status == EmbeddingStatus.Completed) e.EmbeddingFailures = 0;
        }

        if (entityType == "person")
            await personService.UpdateAsync(id, Apply);
        else if (entityType == "organisation")
            await organisationService.UpdateAsync(id, Apply);

        await BroadcastEmbeddingStatusAsync(id, status);
    }

    private async Task BroadcastEmbeddingStatusAsync(Guid id, EmbeddingStatus status)
    {
        try
        {
            await hubContext.Clients.All.EmbeddingStatusChanged(id, status.ToString());
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to broadcast embedding status change for {Id}", id);
        }
    }

    /// <summary>
    /// Processes an uploaded version: records its size, converts Word files to a PDF preview, and makes the
    /// latest version searchable for the project chat. The flags let a retry redo only the step that failed.
    /// Never throws; failures end up in the version's statuses.
    /// </summary>
    public async Task RunProjectDocumentPipelineAsync(Guid versionId, bool preview = true, bool embedding = true)
    {
        ProjectDocumentVersion? version = await projectDocumentService.GetVersionAsync(versionId);
        if (version == null) return; // Document deleted while this waited in the queue

        // A PDF is its own preview, so that step needs neither the file nor Gotenberg
        if (preview && version.Extension == "pdf")
        {
            await projectDocumentService.UpdateVersionAsync(versionId, v => ApplyPreviewStatus(v, EmbeddingStatus.Completed));
            preview = false;
        }

        if (!preview && !embedding) return;

        logger.Information("Project document pipeline started for version {VersionId}", versionId);

        byte[] file;
        try
        {
            ObjectDownloadResponse download = await storageService.DownloadObjectAsync(bucketName, ProjectDocumentService.StorageKey(versionId));
            await using Stream downloadStream = download.Stream;
            using MemoryStream memoryStream = new();
            await downloadStream.CopyToAsync(memoryStream);
            file = memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to load the file of project document version {VersionId}", versionId);
            await projectDocumentService.UpdateVersionAsync(versionId, v =>
            {
                if (preview) ApplyPreviewStatus(v, EmbeddingStatus.Failed);
                if (embedding) ApplyEmbeddingStatus(v, EmbeddingStatus.Failed, ex.Message);
            });
            return;
        }

        await projectDocumentService.UpdateVersionAsync(versionId, v => v.SizeBytes = file.LongLength);

        // Preview first: that's what the upload is waiting to see. Neither step depends on the other
        if (preview) await CreateProjectDocumentPreviewAsync(version, file);
        if (embedding) await EmbedProjectDocumentVersionAsync(version, file);

        logger.Information("Project document pipeline finished for version {VersionId}", versionId);
    }

    private async Task CreateProjectDocumentPreviewAsync(ProjectDocumentVersion version, byte[] file)
    {
        try
        {
            await projectDocumentService.UpdateVersionAsync(version.Id, v => ApplyPreviewStatus(v, EmbeddingStatus.Processing));

            // The conversion disposes the stream it sends, so it gets its own copy of the file
            using MemoryStream pdf = await documentConversionService.ConvertToPdfAsync(new MemoryStream(file), $"document.{version.Extension}");
            await storageService.UploadObjectAsync(bucketName, ProjectDocumentService.PreviewKey(version.Id), pdf, contentType: "application/pdf");

            await projectDocumentService.UpdateVersionAsync(version.Id, v => ApplyPreviewStatus(v, EmbeddingStatus.Completed));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Preview conversion failed for project document version {VersionId}", version.Id);
            await projectDocumentService.UpdateVersionAsync(version.Id, v => ApplyPreviewStatus(v, EmbeddingStatus.Failed));
        }
    }

    private async Task EmbedProjectDocumentVersionAsync(ProjectDocumentVersion version, byte[] file)
    {
        // Only the latest version is searchable. Checking up front saves an OCR run when a newer version arrived
        // while this one waited; ReplaceProjectDocumentPointsAsync checks again atomically for the race during processing.
        if (!await projectDocumentService.IsLatestVersionAsync(version.DocumentId, version.Id))
        {
            await projectDocumentService.UpdateVersionAsync(version.Id, v=> ApplyEmbeddingStatus(v, EmbeddingStatus.Completed));
            return;
        }

        try
        {
            await projectDocumentService.UpdateVersionAsync(version.Id, v => ApplyEmbeddingStatus(v, EmbeddingStatus.Processing));

            // The fileId makes OCR cache to project-docs/{id}-ocr, next to the file, where delete and cleanup expect it
            OcrResult ocrResult = await textExtractionService.ExtractOcrResultFromFileAsync(
                new MemoryStream(file), $".{version.Extension}", bucketName, ProjectDocumentService.StorageKey(version.Id)
            );

            string? text = string.IsNullOrWhiteSpace(ocrResult.Text) ? null : ocrResult.Text;
            await projectDocumentService.UpdateVersionAsync(version.Id, v => v.ExtractedText = text);

            if (text == null) logger.Warning("No text extracted from project document version {VersionId}", version.Id);

            // No text still replaces: the previous version's chunks must not answer for this one
            List<(string Text, int Part)> chunks = [.. SplitTextIntoChunks(text ?? "", markdownSplit: true).Select((chunk, index) => (chunk, index))];
            bool replaced = await vectorStore.ReplaceProjectDocumentPointsAsync(version.DocumentId, version.Id, chunks);

            if (!replaced)
                logger.Information("Project document version {VersionId} was superseded or deleted during processing, chunks not indexed", version.Id);

            await projectDocumentService.UpdateVersionAsync(version.Id, v => ApplyEmbeddingStatus(v, EmbeddingStatus.Completed));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Embedding failed for project document version {VersionId}", version.Id);
            await projectDocumentService.UpdateVersionAsync(version.Id, v => ApplyEmbeddingStatus(v, EmbeddingStatus.Failed, DescribeFailure(ex)));
        }
    }

    private static void ApplyEmbeddingStatus(ProjectDocumentVersion version, EmbeddingStatus status, string? error = null)
    {
        version.EmbeddingStatus = status;
        version.EmbeddingError = error;

        if (status is EmbeddingStatus.Processing or EmbeddingStatus.Failed) version.EmbeddingLastAttempt = DateTime.UtcNow;
        if (status == EmbeddingStatus.Failed) version.EmbeddingFailures++;
        if (status == EmbeddingStatus.Completed) version.EmbeddingFailures = 0;
    }

    private static void ApplyPreviewStatus(ProjectDocumentVersion version, EmbeddingStatus status)
    {
        version.PreviewStatus = status;

        if (status is EmbeddingStatus.Processing or EmbeddingStatus.Failed) version.PreviewLastAttempt = DateTime.UtcNow;
        if (status == EmbeddingStatus.Failed) version.PreviewFailures++;
        if (status == EmbeddingStatus.Completed) version.PreviewFailures = 0;
    }

    private string DescribeFailure(Exception ex)
        => mistralStatusService.Current.Status == MistralAvailability.Down
            ? "Mistral's AI service currently appears to be down or degraded, this isn't a bug on our end. Check status.mistral.ai for the latest status and try again once it recovers."
            : ex.Message;
}