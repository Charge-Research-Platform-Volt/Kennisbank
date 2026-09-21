using System.Text;
using System.Text.Json;
using KnowledgeBank.Hubs;
using KnowledgeBank.Models;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Search;
using Microsoft.AspNetCore.SignalR;

namespace KnowledgeBank.Services.AI;

public class ChatToolExecutor(AiService aiService, EmbeddingService embeddingService, AttachmentChunkSearchIndexService attachmentChunkSearchIndexService, IServiceScopeFactory scopeFactory, IHubContext<AppHub, IAppHubClient> hubContext)
{
    private const float RelevanceThreshold = 0.25f;
    private readonly Serilog.ILogger logger = Serilog.Log.ForContext<ChatToolExecutor>();

    public async Task<string> ExecuteAsync(MistralToolCall toolCall, string userQuestion, string? projectId, Guid chatId, string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            using var args = JsonDocument.Parse(toolCall.Arguments);
            await using var scope = scopeFactory.CreateAsyncScope();

            return toolCall.Name switch
            {
                "search_library" => await HandleSearchAsync(
                    scope.ServiceProvider.GetRequiredService<LibraryService>(),
                    scope.ServiceProvider.GetRequiredService<ProjectService>(),
                    args, userQuestion, projectId, connectionId, cancellationToken),
                "get_item_details" => await HandleGetItemDetailsAsync(
                    scope.ServiceProvider.GetRequiredService<ResourceService>(),
                    scope.ServiceProvider.GetRequiredService<PersonService>(),
                    scope.ServiceProvider.GetRequiredService<OrganisationService>(),
                    args, connectionId, cancellationToken),
                "find_related_items" => await HandleFindRelatedItemsAsync(
                    scope.ServiceProvider.GetRequiredService<ResourceService>(),
                    scope.ServiceProvider.GetRequiredService<PersonService>(),
                    scope.ServiceProvider.GetRequiredService<OrganisationService>(),
                    args, connectionId, cancellationToken),
                "search_item_content" => await HandleSearchItemContentAsync(
                    scope.ServiceProvider.GetRequiredService<LibraryService>(),
                    args, connectionId, cancellationToken),
                "browse_library" => await HandleBrowseLibraryAsync(
                    scope.ServiceProvider.GetRequiredService<LibraryService>(),
                    scope.ServiceProvider.GetRequiredService<TaxonomySearchIndexService>(),
                    scope.ServiceProvider.GetRequiredService<ProjectService>(),
                    args, projectId, connectionId, cancellationToken),
                "find_similar_resources" => await HandleFindSimilarResourcesAsync(
                    scope.ServiceProvider.GetRequiredService<ChunkSearchIndexService>(),
                    scope.ServiceProvider.GetRequiredService<ResourceService>(),
                    args, connectionId, cancellationToken),
                "search_attachment_content" => await HandleSearchAttachmentContentAsync(
                    scope.ServiceProvider.GetRequiredService<ChatService>(),
                    args, chatId, connectionId, cancellationToken),
                _ => "Error: unknown tool"
            };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Tool call failed for {ToolName} with args: {Args}", toolCall.Name, toolCall.Arguments);
            return $"Tool call failed: {ex.Message}";
        }
    }

    public async Task<string> FormatSearchResultsAsync(List<LibraryItemWithChunks> results, string userQuestion, string searchQuery, CancellationToken ct)
    {
        var relevant = results.Where(i => i.Score >= RelevanceThreshold).ToList();

        if (relevant.Count == 0)
            return "No relevant results found";

        var summaryTasks = relevant.Select(item => item.MatchedChunks.Count > 0
            ? aiService.SummarizeChunksAsync(userQuestion, searchQuery, item.Item.Name, item.MatchedChunks, ct)
            : Task.FromResult(item.Item.Description ?? ""));

        string[] summaries = await Task.WhenAll(summaryTasks);

        StringBuilder sb = new();

        for (int i = 0; i < relevant.Count; i++)
        {
            var item = relevant[i];
            sb.AppendLine($"{item.Item.Name} ({item.Item.Type})");
            sb.AppendLine($"Cite as: [SRC:{item.Item.Id}]");
            sb.AppendLine($"Relevance: {item.Score:F2}");

            if (!string.IsNullOrWhiteSpace(summaries[i]) && summaries[i] != "NO_RELEVANT_CONTENT")
                sb.AppendLine($"Content: {summaries[i]}");

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<string> HandleSearchAsync(LibraryService libraryService, ProjectService projectService, JsonDocument args, string userQuestion, string? projectId, string connectionId, CancellationToken ct)
    {
        string query = args.RootElement.GetProperty("query").GetString() ?? userQuestion;
        string? typeFilter = args.RootElement.TryGetProperty("type", out var t) ? t.GetString() : null;
        int limit = args.RootElement.TryGetProperty("limit", out var l) ? Math.Clamp(l.GetInt32(), 1, 30) : 15;

        Guid[]? idsFilter = null;
        if (projectId != null && Guid.TryParse(projectId, out var projGuid))
        {
            Guid[] ids = await projectService.GetProjectItemIdsAsync(projGuid);
            if (ids.Length > 0) idsFilter = ids;
        }

        logger.Information("LLM searching for: {Query}", query);
        await hubContext.Clients.Client(connectionId).ChatToolStatus("search_library", $"Searching: {query}");

        List<LibraryItemWithChunks> results = await libraryService.SearchContentAsync(query, limit, idsFilter, typeFilter);
        string formatted = await FormatSearchResultsAsync(results, userQuestion, query, ct);
        logger.Debug("Search tool result: {Result}", formatted);
        return formatted;
    }

    private async Task<string> HandleBrowseLibraryAsync(LibraryService libraryService, TaxonomySearchIndexService taxonomySearchIndexService, ProjectService projectService, JsonDocument args, string? projectId, string connectionId, CancellationToken ct)
    {
        string[] tagNames = GetStringArray(args, "tagNames");
        string[] regionNames = GetStringArray(args, "regionNames");
        string[] resourceTypeNames = GetStringArray(args, "resourceTypeNames");
        string[] journalNames = GetStringArray(args, "journalNames");
        string tagFilterMode = args.RootElement.TryGetProperty("tagFilterMode", out var tfm) ? tfm.GetString() ?? "any" : "any";
        string? pubdateMin = args.RootElement.TryGetProperty("pubdateMin", out var pdMin) ? pdMin.GetString() : null;
        string? pubdateMax = args.RootElement.TryGetProperty("pubdateMax", out var pdMax) ? pdMax.GetString() : null;
        int limit = args.RootElement.TryGetProperty("limit", out var l) ? Math.Clamp(l.GetInt32(), 1, 50) : 20;

        (string Name, string Type)[] facetQueries = [
            .. tagNames.Select(n => (n, TagService.TypeTag)),
            .. regionNames.Select(n => (n, RegionService.TypeTag)),
            .. resourceTypeNames.Select(n => (n, ResourceTypeService.TypeTag)),
            .. journalNames.Select(n => (n, JournalService.TypeTag))
        ];

        (Guid? Id, string? MatchedName)[] resolved = await taxonomySearchIndexService.SearchManyAsync(facetQueries);

        List<string> unresolved = [];
        List<string> fuzzyMatches = [];
        int cursor = 0;

        string[] ExtractIds(string[] names, string label)
        {
            List<string> ids = [];
            foreach (string name in names)
            {
                (Guid? id, string? matchedName) = resolved[cursor++];

                if (!id.HasValue)
                {
                    unresolved.Add($"{label} '{name}'");
                    continue;
                }

                ids.Add(id.Value.ToString());

                if (!string.Equals(name, matchedName, StringComparison.OrdinalIgnoreCase))
                    fuzzyMatches.Add($"{label} '{name}' matched '{matchedName}'");
            }

            return [.. ids];
        }

        string[] tagIds = ExtractIds(tagNames, "tag");
        string[] regionIds = ExtractIds(regionNames, "region");
        string[] resourceTypeIds = ExtractIds(resourceTypeNames, "resource type");
        string[] journalIds = ExtractIds(journalNames, "journal");

        logger.Information(
            "LLM browsing library: tags=[{TagNames}]->{TagIds} (mode={TagFilterMode}), regions=[{RegionNames}]->{RegionIds}, resourceTypes=[{ResourceTypeNames}]->{ResourceTypeIds}, journals=[{JournalNames}]->{JournalIds}, pubdateMin={PubdateMin}, pubdateMax={PubdateMax}, unresolved=[{Unresolved}]",
            string.Join(", ", tagNames), string.Join(", ", tagIds), tagFilterMode,
            string.Join(", ", regionNames), string.Join(", ", regionIds),
            string.Join(", ", resourceTypeNames), string.Join(", ", resourceTypeIds),
            string.Join(", ", journalNames), string.Join(", ", journalIds),
            pubdateMin, pubdateMax, string.Join(", ", unresolved));

        bool anyFacetFullyUnresolved =
            (tagNames.Length > 0 && tagIds.Length == 0) ||
            (regionNames.Length > 0 && regionIds.Length == 0) ||
            (resourceTypeNames.Length > 0 && resourceTypeIds.Length == 0) ||
            (journalNames.Length > 0 && journalIds.Length == 0);

        if (anyFacetFullyUnresolved)
            return $"No matching resources found. Could not resolve {string.Join(", ", unresolved)} — no match found in the library.";

        await hubContext.Clients.Client(connectionId).ChatToolStatus("browse_library", "Browsing library...");

        LibraryRequest request = new()
        {
            Page = 1,
            PageSize = 100,
            FilterOptions = new LibraryFilterOptions
            {
                TypeFilter = ["resource"],
                TagFilter = tagIds,
                TagFilterMode = tagFilterMode,
                RegionFilter = regionIds,
                ResourceTypeFilter = resourceTypeIds,
                JournalFilter = journalIds,
                PubdateMin = pubdateMin,
                PubdateMax = pubdateMax
            }
        };

        LibraryResult result = await libraryService.GetLibraryAsync(request);
        IEnumerable<LibraryItem> items = result.Items;

        if (projectId != null && Guid.TryParse(projectId, out var projGuid))
        {
            Guid[] projectItemIds = await projectService.GetProjectItemIdsAsync(projGuid);
            HashSet<Guid> allowed = [.. projectItemIds];
            items = items.Where(i => allowed.Contains(i.Id));
        }

        List<LibraryItem> limited = items.Take(limit).ToList();

        logger.Debug("browse_library matched {TotalCount} resources ({ReturnedCount} returned)", result.TotalCount, limited.Count);

        var sb = new StringBuilder();

        if (unresolved.Count > 0)
            sb.AppendLine($"Note: could not resolve {string.Join(", ", unresolved)} — no match found in the library.");

        if (fuzzyMatches.Count > 0)
            sb.AppendLine($"Note: some names were matched approximately - {string.Join("; ", fuzzyMatches)}. Mention this to the user if the results don't look right.");

        if (limited.Count == 0)
        {
            sb.AppendLine("No matching resources found.");
            return sb.ToString();
        }

        foreach (LibraryItem item in limited)
        {
            sb.AppendLine($"{item.Name} ({item.Type})");
            sb.AppendLine($"Cite as: [SRC:{item.Id}]");
            if (item.PublicationDate.HasValue) sb.AppendLine($"Published: {item.PublicationDate.Value:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(item.Description)) sb.AppendLine($"Description: {item.Description}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<string> HandleFindSimilarResourcesAsync(ChunkSearchIndexService chunkSearchIndexService, ResourceService resourceService, JsonDocument args, string connectionId, CancellationToken ct)
    {
        if (!args.RootElement.TryGetProperty("id", out var idProp) || !Guid.TryParse(idProp.GetString(), out var resourceId))
            return "Error: invalid or missing id";

        int limit = args.RootElement.TryGetProperty("limit", out var l) ? Math.Clamp(l.GetInt32(), 1, 20) : 10;

        logger.Information("LLM finding resources similar to: {ResourceId}", resourceId);
        await hubContext.Clients.Client(connectionId).ChatToolStatus("find_similar_resources", "Finding similar resources...");

        List<(Guid ResourceId, float Score)> results = await chunkSearchIndexService.RecommendSimilarAsync(resourceId, limit, 0.65f);
        logger.Debug("find_similar_resources matched {Count} resources: {Scores}", results.Count, string.Join(", ", results.Select(r => $"{r.ResourceId}={r.Score:F2}")));
        if (results.Count == 0) return "No similar resources found.";

        Resource[] resources = await resourceService.GetByIdsAsync(results.Select(r => r.ResourceId));
        Dictionary<Guid, Resource> lookup = resources.ToDictionary(r => r.Id);

        var sb = new StringBuilder();
        foreach ((Guid id, float score) in results)
        {
            if (!lookup.TryGetValue(id, out Resource? resource)) continue;
            sb.AppendLine($"{resource.Title} (resource)");
            sb.AppendLine($"Cite as: [SRC:{resource.Id}]");
            sb.AppendLine($"Similarity: {score:F2}");
            sb.AppendLine();
        }

        return sb.Length > 0 ? sb.ToString() : "No similar resources found.";
    }

    private async Task<string> HandleSearchItemContentAsync(LibraryService libraryService, JsonDocument args, string connectionId, CancellationToken ct)
    {
        if (!args.RootElement.TryGetProperty("id", out var idProp) || !Guid.TryParse(idProp.GetString(), out var itemId))
            return "Error: invalid or missing id";

        string type = args.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";
        string query = args.RootElement.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(query))
            return "Error: query is required";

        await hubContext.Clients.Client(connectionId).ChatToolStatus("search_item_content", $"Searching item content: {query}");

        List<LibraryItemWithChunks> results = await libraryService.SearchContentAsync(query, 1, [itemId], type, chunksPerParent: 8);
        LibraryItemWithChunks? match = results.FirstOrDefault(r => r.Item.Id == itemId);

        if (match == null || match.MatchedChunks.Count == 0)
            return "No relevant content found within this item.";

        var sb = new StringBuilder();
        sb.AppendLine($"Cite as: [SRC:{match.Item.Id}]");
        sb.AppendLine();
        sb.AppendLine(string.Join("\n---\n", match.MatchedChunks));
        return sb.ToString();
    }

    private async Task<string> HandleGetItemDetailsAsync(ResourceService resourceService, PersonService personService, OrganisationService organisationService, JsonDocument args, string connectionId, CancellationToken ct = default)
    {
        if (!args.RootElement.TryGetProperty("id", out var idProp) || !Guid.TryParse(idProp.GetString(), out var guid))
            return "Error: invalid or missing ID";

        string type = args.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";

        await hubContext.Clients.Client(connectionId).ChatToolStatus("get_item_details", "Getting details...");

        var sb = new StringBuilder();

        if (type == "resource")
        {
            Resource? r = await resourceService.GetByIdAsync(guid, includeRelations: true);
            if (r == null) return "Error: resource not found";

            sb.AppendLine($"Title: {r.Title}");
            sb.AppendLine($"Cite as: [SRC:{r.Id}]");
            if (!string.IsNullOrWhiteSpace(r.Description)) sb.AppendLine($"Description: {r.Description}");
            if (r.PublicationDate.HasValue) sb.AppendLine($"Published: {r.PublicationDate.Value:yyyy-MM-dd}");
            if (r.ResourceType != null) sb.AppendLine($"Type: {r.ResourceType.Name}");
            if (r.Journal != null) sb.AppendLine($"Journal: {r.Journal.Name}");

            var authors = r.ResourceAuthorRelations?.Where(x => x.Author != null).Select(x => x.Author!.Name).ToList();
            if (authors?.Count > 0) sb.AppendLine($"Authors: {string.Join(", ", authors)}");

            var orgs = r.ResourceOrganisationRelations?.Where(x => x.Organisation != null).Select(x => x.Organisation!.Name).ToList();
            if (orgs?.Count > 0) sb.AppendLine($"Organisations: {string.Join(", ", orgs)}");

            var tags = r.ResourceTagRelations?.Where(x => x.Tag != null).Select(x => x.Tag!.Name).ToList();
            if (tags?.Count > 0) sb.AppendLine($"Tags: {string.Join(", ", tags)}");

            var persons = r.ResourceRelatedPersonRelations?.Where(x => x.Person != null).Select(x => x.Person!.Name).ToList();
            if (persons?.Count > 0) sb.AppendLine($"Related persons: {string.Join(", ", persons)}");

            var regions = r.ResourceRegionRelations?.Where(x => x.Region != null).Select(x => x.Region!.Name).ToList();
            if (regions?.Count > 0) sb.AppendLine($"Regions: {string.Join(", ", regions)}");
        }
        else if (type == "person")
        {
            Person? p = await personService.GetByIdAsync(guid, includeRelations: true);
            if (p == null) return "Error: person not found";

            sb.AppendLine($"Name: {p.Name}");
            sb.AppendLine($"Cite as: [SRC:{p.Id}]");
            if (!string.IsNullOrWhiteSpace(p.Description)) sb.AppendLine($"Description: {p.Description}");
            if (!string.IsNullOrWhiteSpace(p.Occupation)) sb.AppendLine($"Occupation: {p.Occupation}");
            if (!string.IsNullOrWhiteSpace(p.EmailAddress)) sb.AppendLine($"Email: {p.EmailAddress}");

            var orgs = p.PersonOrganisationRelations?.Where(x => x.Organisation != null).Select(x => x.Organisation!.Name).ToList();
            if (orgs?.Count > 0) sb.AppendLine($"Organisations: {string.Join(", ", orgs)}");
        }
        else if (type == "organisation")
        {
            Organisation? o = await organisationService.GetByIdAsync(guid, includeRelations: true);
            if (o == null) return "Error: organisation not found";

            sb.AppendLine($"Name: {o.Name}");
            sb.AppendLine($"Cite as: [SRC:{o.Id}]");
            if (!string.IsNullOrWhiteSpace(o.Description)) sb.AppendLine($"Description: {o.Description}");
            if (!string.IsNullOrWhiteSpace(o.Website)) sb.AppendLine($"Website: {o.Website}");

            var members = o.PersonOrganisationRelations?.Where(x => x.Person != null).Select(x => x.Person!.Name).ToList();
            if (members?.Count > 0) sb.AppendLine($"Members: {string.Join(", ", members)}");
        }
        else return "Error: unknown item type";

        return sb.ToString();
    }

    private async Task<string> HandleFindRelatedItemsAsync(ResourceService resourceService, PersonService personService, OrganisationService organisationService, JsonDocument args, string connectionId, CancellationToken ct = default)
    {
        if (!args.RootElement.TryGetProperty("id", out var idProp) || !Guid.TryParse(idProp.GetString(), out var guid))
            return "Error: invalid or missing ID";

        string type = args.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";
        await hubContext.Clients.Client(connectionId).ChatToolStatus("find_related_items", "Finding related items...");

        var sb = new StringBuilder();

        if (type == "resource")
        {
            Resource? r = await resourceService.GetByIdAsync(guid, includeRelations: true);
            if (r == null) return "Error: resource not found";

            sb.AppendLine($"Related items for resource: {r.Title}");
            foreach (var rel in r.ResourceAuthorRelations?.Where(x => x.Author != null) ?? [])
                sb.AppendLine($"- Author: {rel.Author!.Name} [SRC:{rel.Author.Id}]");
            foreach (var rel in r.ResourceOrganisationRelations?.Where(x => x.Organisation != null) ?? [])
                sb.AppendLine($"- Organisation: {rel.Organisation!.Name} [SRC:{rel.Organisation.Id}]");
            foreach (var rel in r.ResourceRelatedPersonRelations?.Where(x => x.Person != null) ?? [])
                sb.AppendLine($"- Related person: {rel.Person!.Name} [SRC:{rel.Person.Id}]");
        }
        else if (type == "person")
        {
            Person? p = await personService.GetByIdAsync(guid, includeRelations: true);
            if (p == null) return "Error: person not found";

            sb.AppendLine($"Related items for person: {p.Name}");
            foreach (var rel in p.PersonOrganisationRelations?.Where(x => x.Organisation != null) ?? [])
                sb.AppendLine($"- Organisation: {rel.Organisation!.Name} [SRC:{rel.Organisation.Id}]");

            var resources = await resourceService.GetAllAsync(predicate: r => r.ResourceAuthorRelations!.Any(a => a.AuthorId == guid));
            foreach (var res in resources)
                sb.AppendLine($"- Resource: {res.Title} [SRC:{res.Id}]");
        }
        else if (type == "organisation")
        {
            Organisation? o = await organisationService.GetByIdAsync(guid, includeRelations: true);
            if (o == null) return "Error: organisation not found";

            sb.AppendLine($"Related items for organisation: {o.Name}");
            foreach (var rel in o.PersonOrganisationRelations?.Where(x => x.Person != null) ?? [])
                sb.AppendLine($"- Member: {rel.Person!.Name} [SRC:{rel.Person.Id}]");

            var resources = await resourceService.GetAllAsync(predicate: r => r.ResourceOrganisationRelations!.Any(a => a.OrganisationId == guid));
            foreach (var res in resources)
                sb.AppendLine($"- Resource: {res.Title} [SRC:{res.Id}]");
        }
        else return "Error: unknown item type";

        return sb.Length > 0 ? sb.ToString() : "No related items found";
    }

    private async Task<string> HandleSearchAttachmentContentAsync(ChatService chatService, JsonDocument args, Guid chatId, string connectionId, CancellationToken ct)
    {
        if (!args.RootElement.TryGetProperty("attachmentId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var attachmentId))
            return "Error: invalid or missing attachmentId";

        string query = args.RootElement.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(query))
            return "Error: query is required";

        await hubContext.Clients.Client(connectionId).ChatToolStatus("search_attachment_content", $"Searching attached file: {query}");

        MessageAttachments? attachment = await chatService.GetAttachmentByIdAsync(attachmentId);
        if (attachment == null || attachment.ChatId != chatId)
            return "Error: attachment not found in this chat";

        string citeLine = $"Cite as: [ATTACH:{attachment.Id}]";

        if (!attachment.IsChunked)
            return string.IsNullOrEmpty(attachment.ExtractedText) ? "No content available." : $"{citeLine}\n\n{attachment.ExtractedText}";

        float[] queryEmbedding = await embeddingService.GenerateEmbedding(query);
        List<string> matches = await attachmentChunkSearchIndexService.SearchAsync(query, queryEmbedding, attachmentId);

        return matches.Count == 0 ? "No relevant sections found for that query." : $"{citeLine}\n\n{string.Join("\n---\n", matches)}";
    }

    private static string[] GetStringArray(JsonDocument args, string propertyName)
    {
        if (!args.RootElement.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Array)
            return [];

        return [.. prop.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s))];
    }
}