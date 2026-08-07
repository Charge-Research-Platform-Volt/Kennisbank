using System.Text;
using System.Text.Json;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Search;
using Microsoft.AspNetCore.SignalR;

namespace Hubs;

public partial class Chat
{
    private static readonly MistralFunction SearchTool = new(
        "search_library",
        "Search the personal library for resources, people, or organisations",
        new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string", description = "Search query" },
                type = new { type = "string", @enum = new[] { "resource", "person", "organisation" }, description = "Filter by item type. Omit to search all." },
                limit = new { type = "integer", description = "Number of results to return (default 15, max 30)" }
            },
            required = new[] { "query" },
            additionalProperties = false
        }
    );

    private static readonly MistralFunction GetItemDetailsTool = new(
        "get_item_details",
        "Get full details of a specific library item by ID, including authors, tags, organisations, related persons, and description",
        new
        {
            type = "object",
            properties = new
            {
                id = new { type = "string", description = "Item UUID" },
                type = new { type = "string", @enum = new[] { "resource", "person", "organisation" }, description = "Item type" }
            },
            required = new[] { "id", "type" },
            additionalProperties = false
        }
    );

    private static readonly MistralFunction FindRelatedItemsTool = new(
        "find_related_items",
        "Find items connected to a given library item — e.g. all resources by a person, all persons in an organisation, all organisations linked to a resource",
        new
        {
            type = "object",
            properties = new
            {
                id = new { type = "string", description = "Item UUID" },
                type = new { type = "string", @enum = new[] { "resource", "person", "organisation" }, description = "Item type" }
            },
            required = new[] { "id", "type" },
            additionalProperties = false
        }
    );

    private static readonly MistralFunction SearchItemContentTool = new(
        "search_item_content",
        "Search within a specific library item's full indexed content for more detail than the initial search_library result summary provided. Use this after search_library or get_item_details has identified a relevant item and you need deeper or more specific information from it than the summary gave you. Most useful for resources with full document text — persons and organisations have limited indexed content beyond their description.",
        new
        {
            type = "object",
            properties = new
            {
                id = new { type = "string", description = "Item UUID" },
                type = new { type = "string", @enum = new[] { "resource", "person", "organisation" }, description = "Item type" },
                query = new { type = "string", description = "What to search for within the item's content" }
            },
            required = new[] { "id", "type", "query" },
            additionalProperties = false
        }
    );

    private static readonly MistralFunction BrowseLibraryTool = new(
        "browse_library",
        "Browse resources using structured filters — tags, regions, resource types, journals, publication date range — instead of semantic text search. Use this when the user asks for items matching specific facets (e.g. \"what do I have tagged X from 2024?\") rather than a topical question. Facet values are names, not IDs — they are resolved automatically; unresolved names are reported back. Only matches resources, not people or organisations.",
        new
        {
            type = "object",
            properties = new
            {
                tagNames = new { type = "array", items = new { type = "string" }, description = "Filter by tag names" },
                tagFilterMode = new { type = "string", @enum = new[] { "any", "all" }, description = "Match any or all of tagNames (default any)" },
                regionNames = new { type = "array", items = new { type = "string" }, description = "Filter by region names" },
                resourceTypeNames = new { type = "array", items = new { type = "string" }, description = "Filter by resource type names" },
                journalNames = new { type = "array", items = new { type = "string" }, description = "Filter by journal names" },
                pubdateMin = new { type = "string", description = "Earliest publication date, e.g. 2024-01-01" },
                pubdateMax = new { type = "string", description = "Latest publication date, e.g. 2024-12-31" },
                limit = new { type = "integer", description = "Number of results to return (default 20, max 50)" }
            },
            additionalProperties = false
        }
    );

    private static readonly MistralFunction FindSimilarResourcesTool = new(
        "find_similar_resources",
        "Find resources similar in content to a given resource, based on vector similarity of their full text — not the same as find_related_items, which uses explicit tags/authors/organisations. Use this for \"more like this\" requests.",
        new
        {
            type = "object",
            properties = new
            {
                id = new { type = "string", description = "Resource UUID to find similar resources for" },
                limit = new { type = "integer", description = "Number of results to return (default 10, max 20)" }
            },
            required = new[] { "id" },
            additionalProperties = false
        }
    );

    private static readonly MistralFunction SearchAttachmentContentTool = new (
        "search_attachment_content",
        "Search within a specific attached file for relevant sections. The list of files attached to this chat, including their ids, is always provided as a system message. For small files this returns the full content, for large files this returns only the most relevant excerpts, not the full text — you may need multiple targeted queries to build a complete picture. Provide a focused query describing what you're looking for.",
        new
        {
            type = "object",
            properties = new
            {
                attachmentId = new { type = "string", description = "The attachment's ID" },
                query = new { type = "string", description = "What to search for within the file" }
            },
            required = new[] { "attachmentId", "query" },
            additionalProperties = false
        }
    );

    private async Task<string> HandleSearchAsync(LibraryService libraryService, ProjectService projectService, JsonDocument args, string userQuestion, string? projectId, CancellationToken ct)
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
        await Clients.Caller.SendAsync("ToolStatus", "search_library", $"Searching: {query}", ct);

        List<LibraryItemWithChunks> results = await libraryService.SearchContentAsync(query, limit, idsFilter, typeFilter);
        string formatted = await FormatSearchResultsAsync(results, userQuestion, query, ct);
        logger.Debug("Search tool result: {Result}", formatted);
        return formatted;
    }

    private async Task<string> HandleBrowseLibraryAsync(LibraryService libraryService, TagService tagService, RegionService regionService, JournalService journalService, ResourceTypeService resourceTypeService, ProjectService projectService, JsonDocument args, string? projectId, CancellationToken ct)
    {
        string[] tagNames = GetStringArray(args, "tagNames");
        string[] regionNames = GetStringArray(args, "regionNames");
        string[] resourceTypeNames = GetStringArray(args, "resourceTypeNames");
        string[] journalNames = GetStringArray(args, "journalNames");
        string tagFilterMode = args.RootElement.TryGetProperty("tagFilterMode", out var tfm) ? tfm.GetString() ?? "any" : "any";
        string? pubdateMin = args.RootElement.TryGetProperty("pubdateMin", out var pdMin) ? pdMin.GetString() : null;
        string? pubdateMax = args.RootElement.TryGetProperty("pubdateMax", out var pdMax) ? pdMax.GetString() : null;
        int limit = args.RootElement.TryGetProperty("limit", out var l) ? Math.Clamp(l.GetInt32(), 1, 50) : 20;

        List<string> unresolved = [];

        async Task<string[]> ResolveAsync(string[] names, string label, Func<string, Task<(object[] Items, int Total)>> search)
        {
            List<string> ids = [];
            foreach (string name in names)
            {
                (object[] items, _) = await search(name);
                if (items.Length > 0) ids.Add(((dynamic)items[0]).Id.ToString());
                else unresolved.Add($"{label} '{name}'");
            }
            return [.. ids];
        }

        string[] tagIds = await ResolveAsync(tagNames, "tag", async name =>
        {
            var (items, total) = await tagService.SearchAsync(name, 1, 1);
            return (items.Cast<object>().ToArray(), total);
        });
        string[] regionIds = await ResolveAsync(regionNames, "region", async name =>
        {
            var (items, total) = await regionService.SearchAsync(name, 1, 1);
            return (items.Cast<object>().ToArray(), total);
        });
        string[] resourceTypeIds = await ResolveAsync(resourceTypeNames, "resource type", async name =>
        {
            var (items, total) = await resourceTypeService.SearchAsync(name, 1, 1);
            return (items.Cast<object>().ToArray(), total);
        });
        string[] journalIds = await ResolveAsync(journalNames, "journal", async name =>
        {
            var (items, total) = await journalService.SearchAsync(name, 1, 1);
            return (items.Cast<object>().ToArray(), total);
        });

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

        await Clients.Caller.SendAsync("ToolStatus", "browse_library", "Browsing library...", ct);

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

    private async Task<string> HandleFindSimilarResourcesAsync(ChunkSearchIndexService chunkSearchIndexService, ResourceService resourceService, JsonDocument args, CancellationToken ct)
    {
        if (!args.RootElement.TryGetProperty("id", out var idProp) || !Guid.TryParse(idProp.GetString(), out var resourceId))
            return "Error: invalid or missing id";

        int limit = args.RootElement.TryGetProperty("limit", out var l) ? Math.Clamp(l.GetInt32(), 1, 20) : 10;

        logger.Information("LLM finding resources similar to: {ResourceId}", resourceId);
        await Clients.Caller.SendAsync("ToolStatus", "find_similar_resources", "Finding similar resources...", ct);

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

    private static string[] GetStringArray(JsonDocument args, string propertyName)
    {
        if (!args.RootElement.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Array)
            return [];

        return [.. prop.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s))];
    }

    private async Task<string> HandleSearchItemContentAsync(LibraryService libraryService, JsonDocument args, CancellationToken ct)
    {
        if (!args.RootElement.TryGetProperty("id", out var idProp) || !Guid.TryParse(idProp.GetString(), out var itemId))
            return "Error: invalid or missing id";

        string type = args.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";
        string query = args.RootElement.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(query))
            return "Error: query is required";

        await Clients.Caller.SendAsync("ToolStatus", "search_item_content", $"Searching item content: {query}", ct);

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

    private async Task<string> HandleGetItemDetailsAsync(ResourceService resourceService, PersonService personService, OrganisationService organisationService, JsonDocument args, CancellationToken ct = default)
    {
        if (!args.RootElement.TryGetProperty("id", out var idProp) || !Guid.TryParse(idProp.GetString(), out var guid))
            return "Error: invalid or missing ID";

        string type = args.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";

        await Clients.Caller.SendAsync("ToolStatus", "get_item_details", $"Getting details...", ct);

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

    private async Task<string> HandleFindRelatedItemsAsync(ResourceService resourceService, PersonService personService, OrganisationService organisationService, JsonDocument args, CancellationToken ct = default)
    {
        if (!args.RootElement.TryGetProperty("id", out var idProp) || !Guid.TryParse(idProp.GetString(), out var guid))
            return "Error: invalid or missing ID";

        string type = args.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";
        await Clients.Caller.SendAsync("ToolStatus", "find_related_items", "Finding related items...", ct);

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

    private async Task<string> HandleSearchAttachmentContentAsync(ChatService chatService, JsonDocument args, Guid chatId, CancellationToken ct)
    {
        if (!args.RootElement.TryGetProperty("attachmentId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var attachmentId))
            return "Error: invalid or missing attachmentId";

        string query = args.RootElement.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(query))
            return "Error: query is required";

        await Clients.Caller.SendAsync("ToolStatus", "search_attachment_content", $"Searching attached file: {query}", ct);

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
}