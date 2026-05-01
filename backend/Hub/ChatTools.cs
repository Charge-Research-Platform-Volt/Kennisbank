using System.Text;
using System.Text.Json;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Search.Models;
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

    private async Task<string> HandleSearchAsync(JsonDocument args, string userQuestion, CancellationToken ct)
    {
        string query = args.RootElement.GetProperty("query").GetString() ?? userQuestion;
        string? typeFilter = args.RootElement.TryGetProperty("type", out var t) ? t.GetString() : null;
        int limit = args.RootElement.TryGetProperty("limit", out var l) ? Math.Clamp(l.GetInt32(), 1, 30) : 15;

        var filters = new Dictionary<string, object?>();
        if (typeFilter != null) filters["type"] = new[] { typeFilter };

        logger.Information("LLM searching for: {Query}", query);
        await Clients.Caller.SendAsync("ToolStatus", "search_library", $"Searching: {query}", ct);

        HybridSearchResult result = await hybridSearchService.SearchAsync(query, 1, limit, filters, includeMetadataChunks: true);
        string formatted = await FormatSearchResultsAsync(result, userQuestion, query, ct);
        logger.Debug("Search tool result: {Result}", formatted);
        return formatted;
    }

    private async Task<string> HandleGetItemDetailsAsync(JsonDocument args, CancellationToken ct = default)
    {
        if (!args.RootElement.TryGetProperty("id", out var idProp) || !Guid.TryParse(idProp.GetString(), out var guid))
            return "Error: invalid or missing ID";

        string type = args.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";

        await Clients.Caller.SendAsync("ToolStatus", "get_item_details", $"Getting details...", ct);

        var sb = new StringBuilder();

        if (type == "resource")
        {
            Resource? r = await resourceManager.GetResourceAsync(guid,
                "ResourceAuthorRelations.Author",
                "ResourceTagRelations.Tag",
                "ResourceOrganisationRelations.Organisation",
                "ResourceRegionRelations.Region",
                "ResourceRelatedPersonRelations.Person",
                "ResourceType", "Journal");

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
            Person? p = await resourceManager.GetPersonAsync(guid, "PersonOrganisationRelations.Organisation");
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
            Organisation? o = await resourceManager.GetOrganisationAsync(guid, "PersonOrganisationRelations.Person");
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

    private async Task<string> HandleFindRelatedItemsAsync(JsonDocument args, CancellationToken ct = default)
    {
        if (!args.RootElement.TryGetProperty("id", out var idProp) || !Guid.TryParse(idProp.GetString(), out var guid))
            return "Error: invalid or missing ID";

        string type = args.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";
        await Clients.Caller.SendAsync("ToolStatus", "find_related_items", "Finding related items...", ct);

        var sb = new StringBuilder();

        if (type == "resource")
        {
            Resource? r = await resourceManager.GetResourceAsync(guid,
                "ResourceAuthorRelations.Author",
                "ResourceOrganisationRelations.Organisation",
                "ResourceRelatedPersonRelations.Person");

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
            Person? p = await resourceManager.GetPersonAsync(guid, "PersonOrganisationRelations.Organisation");
            if (p == null) return "Error: person not found";

            sb.AppendLine($"Related items for person: {p.Name}");
            foreach (var rel in p.PersonOrganisationRelations?.Where(x => x.Organisation != null) ?? [])
                sb.AppendLine($"- Organisation: {rel.Organisation!.Name} [SRC:{rel.Organisation.Id}]");

            var resources = await resourceManager.GetAllResourcesAsync(
                predicate: r => r.ResourceAuthorRelations!.Any(a => a.AuthorId == guid));
            foreach (var res in resources)
                sb.AppendLine($"- Resource: {res.Title} [SRC:{res.Id}]");
        }
        else if (type == "organisation")
        {
            Organisation? o = await resourceManager.GetOrganisationAsync(guid, "PersonOrganisationRelations.Person");
            if (o == null) return "Error: organisation not found";

            sb.AppendLine($"Related items for organisation: {o.Name}");
            foreach (var rel in o.PersonOrganisationRelations?.Where(x => x.Person != null) ?? [])
                sb.AppendLine($"- Member: {rel.Person!.Name} [SRC:{rel.Person.Id}]");

            var resources = await resourceManager.GetAllResourcesAsync(
                predicate: r => r.ResourceOrganisationRelations!.Any(a => a.OrganisationId == guid));
            foreach (var res in resources)
                sb.AppendLine($"- Resource: {res.Title} [SRC:{res.Id}]");
        }
        else return "Error: unknown item type";

        return sb.Length > 0 ? sb.ToString() : "No related items found";
    }
}
