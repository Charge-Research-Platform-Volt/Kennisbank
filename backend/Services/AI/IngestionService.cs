using KnowledgeBank.Models;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Vector;
using KnowledgeBank.Utils;
using Microsoft.SemanticKernel.Text;
using Serilog;
using System.Text;

namespace KnowledgeBank.Services.AI;

#pragma warning disable SKEXP0050, SKEXP0001

public class IngestionService(IVectorStore vectorStore, TextExtractionService textExtractionService, ResourceService resourceService, PersonService personService, OrganisationService organisationService, EnvironmentConfig environmentConfig)
{
    private readonly Serilog.ILogger logger = Log.ForContext<IngestionService>();
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    public async Task RunResourcePipelineAsync(Guid id, string? fileType = null, Stream? fileStream = null)
    {
        logger.Information("Resource pipeline started for ID: {Id}", id);

        var (richMetadata, resource) = await BuildRichMetadataChunkAsync(id);
        List<string> chunks = [richMetadata];

        if (fileStream != null)
        {
            string ext = fileType != null
                ? (fileType.StartsWith('.') ? fileType : $".{fileType}")
                : ".pdf";

            OcrResult ocrResult = await textExtractionService.ExtractOcrResultFromFileAsync(fileStream, ext, bucketName, id.ToString());

            if (string.IsNullOrEmpty(ocrResult.Text))
            {
                logger.Warning("No text extracted from file for resource {Id}", id);
                return;
            }

            chunks.AddRange(SplitTextIntoChunks(ocrResult.Text, markdownSplit: true));
        }
        else if (resource?.FileType == "website" && !string.IsNullOrEmpty(resource.SourceUrl))
        {
            ReadabilityResult result = await textExtractionService.ExtractTextFromWebAsync(resource.SourceUrl);

            if (!string.IsNullOrWhiteSpace(result.TextContent))
                chunks.AddRange(SplitTextIntoChunks(result.TextContent, markdownSplit: false));
            else
                logger.Warning("No text extracted from website for resource {Id}", id);
        }

        await vectorStore.DeletePointsByResourceIdAsync(id);

        var chunkData = chunks.Select((text, index) => (
            Text: text,
            Type: index == 0 ? ChunkType.MetaData : ChunkType.ContentText,
            Part: index
        )).ToList();

        await vectorStore.CreateResourcePointsAsync(id, chunkData);

        logger.Information("Resource pipeline completed for ID: {Id}", id);
    }

    public async Task RunEntityPipelineAsync(Guid id, string chunk)
    {
        logger.Information("Entity pipeline started for entity ID: {Id}", id);
        await vectorStore.DeletePointsByEntityIdAsync(id);
        await vectorStore.CreateEntityPointsAsync(id, [(chunk, ChunkType.MetaData, 0)]);
        logger.Information("Entity pipeline completed for entity ID: {Id}", id);
    }

    public Task RunEntityPipelineAsync(Person person)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(person.Description)) sb.AppendLine($"Description: {person.Description}").AppendLine();
        sb.AppendLine($"Person: {person.Name}");
        if (person.Aliases.Count > 0) sb.AppendLine($"Also known as: {string.Join(", ", person.Aliases)}");
        if (!string.IsNullOrWhiteSpace(person.Occupation)) sb.AppendLine($"Occupation: {person.Occupation}");
        if (!string.IsNullOrWhiteSpace(person.EmailAddress)) sb.AppendLine($"Email: {person.EmailAddress}");
        
        return RunEntityPipelineAsync(person.Id, sb.ToString());
    }

    public Task RunEntityPipelineAsync(Organisation organisation)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(organisation.Description)) sb.AppendLine($"Description: {organisation.Description}").AppendLine();
        sb.AppendLine($"Organisation: {organisation.Name}");
        if (organisation.Aliases.Count > 0) sb.AppendLine($"Also known as: {string.Join(", ", organisation.Aliases)}");
        if (!string.IsNullOrWhiteSpace(organisation.Website)) sb.AppendLine($"Website: {organisation.Website}");
        if (!string.IsNullOrWhiteSpace(organisation.EmailAddress)) sb.AppendLine($"Email: {organisation.EmailAddress}");
        
        return RunEntityPipelineAsync(organisation.Id, sb.ToString());
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
    
    private static List<string> SplitTextIntoChunks(string text, bool markdownSplit = false, int chunkSize = 512, int overlapSize = 128)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        if (overlapSize >= chunkSize) { chunkSize = 512; overlapSize = 128; }

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
}