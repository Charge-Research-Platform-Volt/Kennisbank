namespace KnowledgeBank.Services.AI;

public static class ChatToolDefinitions
{
    public static readonly MistralFunction Search = new(
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

    public static readonly MistralFunction GetItemDetails = new(
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

    public static readonly MistralFunction FindRelatedItems = new(
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

    public static readonly MistralFunction SearchItemContent = new(
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

    public static readonly MistralFunction BrowseLibrary = new(
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

    public static readonly MistralFunction FindSimilarResources = new(
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

    public static readonly MistralFunction SearchAttachmentContent = new (
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

    public static readonly MistralFunction[] All = [Search, GetItemDetails, FindRelatedItems, SearchItemContent, BrowseLibrary, FindSimilarResources, SearchAttachmentContent];
}