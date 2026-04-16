using KnowledgeBank.Models;
using KnowledgeBank.Data;
using KnowledgeBank.Services.Vector;
using Serilog;
using System.Text;

namespace KnowledgeBank.Services.AI;

public class IngestionService(AiClientProvider aiClientProvider, IVectorStore vectorStore, TextExtractionService textExtractionService, ResourceManager resourceManager)
{
    private readonly Serilog.ILogger logger = Log.ForContext<IngestionService>();

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

            string extracted = await textExtractionService.ExtractTextFromFileAsync(fileStream, ext);

            if (string.IsNullOrEmpty(extracted))
            {
                logger.Warning("No text extracted from file for resource {Id}", id);
                return;
            }

            chunks.AddRange(aiClientProvider.Toolbox.SplitTextIntoChunks(extracted, logChunks: false, markdownSplit: true));
        }
        else if (resource?.FileType == "website" && !string.IsNullOrEmpty(resource.SourceUrl))
        {
            ReadabilityResult result = await textExtractionService.ExtractTextFromWebAsync(resource.SourceUrl);

            if (!string.IsNullOrWhiteSpace(result.TextContent))
                chunks.AddRange(aiClientProvider.Toolbox.SplitTextIntoChunks(result.TextContent, logChunks: false, markdownSplit: false));
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
        if (!string.IsNullOrWhiteSpace(person.Occupation)) sb.AppendLine($"Occupation: {person.Occupation}");
        if (!string.IsNullOrWhiteSpace(person.EmailAddress)) sb.AppendLine($"Email: {person.EmailAddress}");
        
        return RunEntityPipelineAsync(person.Id, sb.ToString());
    }

    public Task RunEntityPipelineAsync(Organisation organisation)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(organisation.Description)) sb.AppendLine($"Description: {organisation.Description}").AppendLine();
        sb.AppendLine($"Organisation: {organisation.Name}");
        if (!string.IsNullOrWhiteSpace(organisation.Website)) sb.AppendLine($"Website: {organisation.Website}");
        if (!string.IsNullOrWhiteSpace(organisation.EmailAddress)) sb.AppendLine($"Email: {organisation.EmailAddress}");
        
        return RunEntityPipelineAsync(organisation.Id, sb.ToString());
    }

    public async Task RunPersonEntityPipelineAsync(Guid id)
    {
        Person? person = await resourceManager.GetPersonAsync(id) ?? throw new InvalidOperationException($"Person {id} not found");
        await RunEntityPipelineAsync(person);
    }

    public async Task RunOrganisationEntityPipelineAsync(Guid id)
    {
        Organisation? organisation = await resourceManager.GetOrganisationAsync(id) ?? throw new InvalidOperationException($"Organisation {id} not found");
        await RunEntityPipelineAsync(organisation);
    }

    public async Task<bool> UpdateResourceMetadataAsync(Guid id)
    {
        logger.Information("Updating metadata for resource {Id}", id);

        var (richMetadata, _) = await BuildRichMetadataChunkAsync(id);

        if (string.IsNullOrEmpty(richMetadata)) return false;

        return await vectorStore.UpdateMetadataPointAsync(id, richMetadata);
    }
    
    private async Task<(string Metadata, Resource? Resource)> BuildRichMetadataChunkAsync(Guid id)
    {
        try
        {
            var resource = await resourceManager.GetResourceAsync(r => r.Id == id, includeProperties: new[]
            {
                "ResourceAuthorRelations.Author",
                "ResourceTagRelations.Tag",
                "ResourceOrganisationRelations.Organisation",
                "ResourceRegionRelations.Region",
                "ResourceType",
                "Journal"
            });

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