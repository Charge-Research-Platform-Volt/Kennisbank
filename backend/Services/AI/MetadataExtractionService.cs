using KnowledgeBank.Models;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Data;
using KnowledgeBank.Utils;
using Lingua;
using Serilog;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace KnowledgeBank.Services.AI;

public class MetadataExtractionService(MistralHttpClient mistralHttpClient, HybridSearchService searchService, EnvironmentConfig environmentConfig, ResourceTypeService resourceTypeService, IDbContextFactory<DatabaseContext> dbFactory)
{
    private readonly Serilog.ILogger logger = Log.ForContext<MetadataExtractionService>();
    private static readonly LanguageDetector languageDetector = LanguageDetectorBuilder.FromAllLanguages().WithPreloadedLanguageModels().Build();


    private const string RoleProduction = "production";
    private const string RoleSubject = "subject";


    private readonly string smallModelName = environmentConfig.GetVariableValue(EnvironmentVariable.SMALL_MODEL_NAME);
    private readonly string mediumModelName = environmentConfig.GetVariableValue(EnvironmentVariable.MEDIUM_MODEL_NAME);


    private const int maxRetries = 3;
    private const int EntityChunkSize = 16_000;
    private const int EntityChunkOverlap = 500;
    private const int EntityChunkRequestIntervalMs = 600;


    private static readonly SemaphoreSlim AiSemaphore = new(5, 5);
    private static readonly SemaphoreSlim SearchSemaphore = new(10, 10);
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };


    private static string TrimTextForFile(string text) => TrimText(text, firstChars: 8000, lastChars: 2000);
    private static string TrimTextForWeb(string text) => TrimText(text, firstChars: 8000, lastChars: 0);


#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
    private static readonly Regex DoiRegex = new(@"10\.\d{4,}/[^\s,;)""\]]+", RegexOptions.Compiled);
    private static readonly Regex ArxivRegex = new(@"arXiv:\s*(\d{4}\.\d{4,5})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PmidRegex = new(@"PMID[:\s]*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex IssnRegex = new(@"ISSN[:\s]*(\d{4}-\d{3}[\dX])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Isbn13Regex = new(@"978[-\s]?\d[-\s]?\d{1,5}[-\s]?\d{1,7}[-\s]?\d{1,7}[-\s]?\d", RegexOptions.Compiled);
    private static readonly Regex Isbn10Regex = new(@"\b\d{9}[\dX]\b", RegexOptions.Compiled);

    private static readonly Regex HeadingRegex = new(@"^#{1,3}\s+.+", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex UnicodeSuperscriptLineRegex = new(@"(?m)^[\u00B9\u00B2\u00B3\u2074-\u2079\u2070].+$", RegexOptions.Compiled);
    private static readonly Regex LatexFootnoteMarkerRegex = new(@"\$\^\{?\d+\}?\$", RegexOptions.Compiled);
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.


    #region Prompts and Schemes

    private const string BiblioSystemPrompt = """
        You are a bibliographic metadata extraction assistant for a political research library.

        General rules:
        - Only extract information explicitly present in the docuemnt. Do not infer or assume.
        - Use empty string for any field you cannot confidently fill, these will be filled by the researcher after.
        - Person names: Given name(s) FIRST, Family name LAST. Reorder any "Last, First" formatted names.

        TITLE
        The document title as it appears. Empty string if not found.

        DESCRIPTION
        Write a neutral, informative summary of what this document is about, what it argues or covers, and what conclusions it draws (50-300 words).
        This is not extracted text, instead you write it based on the document content.
        Write for a researcher who has not read the document.

        ABSTRACT
        Only extract if the document is a scientific paper with an explicit abstract section.
        Copy it verbatim or near-verbatim. Empty string for all other document types or when abstract is not present.

        PUBLICATIONDATE
        Format: YYYY, YYYY-MM, or YYYY-MM-DD — use the most specific format available.
        If a relative date is used ("today", "vandaag", etc.), calculate the absolute date using the current date provided.
        Empty string if not found.

        RESOURCETYPE
        Classify the document using exactly one of the provided resource types.
        Pick the closest match. Leave empty if nothing fits.

        JOURNAL
        The name of the journal or periodical if this is a journal article or academic paper published in one.
        Empty string for reports, books, news articles, and all other types.

        LICENSE
        The license or copyright statement as it appears in the document (e.g. "CC BY 4.0", "All Rights Reserved", "Open Government License v3.0").
        Empty string if not stated.

        AUTHORS
        The specific persons or organisations directly credited as writing or creating this document.
        - Look for bylines, "by", "door", author lists, or explicit authorship credits near the title or at the start/end of the document.
        - Do NOT include publishers, commissioners, funders, or supporting institutions but only those credited as writers or creators.
        - If attribution refers to an internal team or staff ("our editorial team", "onze redactie"), use the name of the publishing organisation as the author.
        - If no authorship is found, leave empty.

        For each author also extract if present near the authorship credits:
        - OCCUPATION: Persons only — job title or role as stated (e.g. "Professor", "CEO", "Digital Minister"). Empty string if not mentioned.
        - EMAIL: Email address if explicitly listed next to the author's name. Empty string if not stated.

        PRODUCTIONS
        Persons and organisations explicitly named as having commissioned, published, funded, led, reviewed, edited, or contributed to this document.
        - Do NOT include document authors, since those should go in AUTHORS.
        - Do NOT include entities merely discussed in the content.
        - Do NOT include laws, regulations, directives, or funding programmes.
        - Leave empty if nothing qualifies.

        For each production entity also extract if present:
        - OCCUPATION: Persons only — job title or role as stated (e.g. "Professor", "CEO", "Digital Minister"). Empty string if not mentioned.
        - WEBSITE: Organisations only — URL to website of organisation as stated. Empty string if not mentioned.
        - EMAIL: Email address if explicitly stated for this entity. Empty string if not stated.

        TAGS
        5-15 topical keywords relevant to the document's subject matter.
        Capitalised but not full-caps: "Machine Learning", not "machine learning" or "MACHINE LEARNING".
        Do NOT include geographic regions, countries, or places — those go in REGIONS.

        REGIONS
        Geographic regions that this document primarily concerns.
        Include countries, supranational regions (e.g. "European Union", "ASEAN"), continents, or broad areas (e.g. "Global South", "Western Europe").
        Do NOT include regions merely mentioned in passing or as examples.
        Leave empty if the document has no clear geographic scope.
    """;

    private static string GetBiblioJsonSchema(IEnumerable<string> resourceTypes) => $$"""
        {
            "type": "object",
            "properties": {
                "title": { "type": "string" },
                "description": { "type": "string" },
                "abstract": { "type": "string" },
                "publicationDate": { "type": "string" },
                "resourceType": { "type": "string", "enum": [{{string.Join(", ", resourceTypes.Select(t => $"\"{t}\""))}}] },
                "journal": { "type": "string" },
                "license": { "type": "string" },
                "authors": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": { "type": "string" },
                            "type": { "type": "string", "enum": ["person", "organisation"] },
                            "occupation": { "type": "string" },
                            "email": { "type": "string" }
                        },
                        "required": ["name", "type", "occupation", "email"],
                        "additionalProperties": false
                    }
                },
                "productions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": { "type": "string" },
                            "type": { "type": "string", "enum": ["person", "organisation"] },
                            "occupation": { "type": "string" },
                            "website": { "type": "string" },
                            "email": { "type": "string" }
                        },
                        "required": ["name", "type", "occupation", "website", "email"],
                        "additionalProperties": false
                    }
                },
                "tags": { "type": "array", "items": { "type": "string" } },
                "regions": { "type": "array", "items": {"type": "string" } }
            },
            "required": ["title", "description", "abstract", "publicationDate", "resourceType", "journal", "license", "authors", "productions", "tags", "regions"],
            "additionalProperties": false
        }
    """;



    private const string ContentSystemPrompt = """
        You are extracting entities substantively discussed in this document section for a political research library.
        The goal is to surface people and organisations relevant for political and policy research — who argued what,
        which organisations played a meaningful role, who is responsible for decisions or outcomes.

        Extract persons and organisations that:
        - Are the primary subject of analysis, reporting, or critique in this section.
        - Are substantively analyzed, discussed, or reported on — their actions, positions, decisions, or findings are examined in depth.

        Do NOT extract:
        - Entities appearing in citations or references. An APA-style citation like "(Smith, 2019)" does NOT qualify — the cited author is NOT a subject.
        - Entities mentioned only as an illustrative example. Signal phrases like "an example is", "such as", "for instance", "een voorbeeld hiervan is", "bijvoorbeeld" immediately before an entity name mean it is an example — do not extract it.
        - Entities appearing in footnotes, endnotes, or marginal annotations (typically marked with a number like "¹" or "1 " at the start of a line).
        - Entities mentioned only once, in passing, or peripheral to the main argument.
        - Laws, regulations, directives, acts, policy frameworks, funding programmes, or initiatives (e.g. GDPR, Horizon Europe, Gaia-X)
        - Fictional examples or illustrative scenario personas
        - Document authors or producers (extracted separately)
        - Tech companies named only as market examples (e.g. "Amazon, Google, and Microsoft dominate cloud")
        - Participant codes, pseudonyms, or anonymized identifiers (e.g. "P08", "P14", "Participant 3", "Interviewee A")

        The key distinction: a subject is WHO the text is ABOUT, not WHO the text cites, quotes as a source, or uses as a passing example.

        Rules:
        - Only extract what is explicitly present. Do not infer.
        - Person names: Given name(s) FIRST, Family name LAST. Reorder any "Last, First" formatted names.
        - Use full official names, not abbreviations.
        - Leave subjects empty if nothing qualifies

        For each extracted entity also provide:
        - REASON: One sentence explaining why this entity was extracted — what action, decision, or position makes them relevant to this section.
        - OCCUPATION: Persons only — job title or role as stated (e.g. "Professor", "CEO", "Digital Minister"). Empty string if not mentioned.
        - WEBSITE: Organisations only — website URL if mentioned in this section. Empty string if not mentioned.

        KEYINSIGHT
        One sentence describing what this section covers that likely does not appear in the document's introduction or conclusion.
        Empty string if nothing notable or if this is the first section.
    """;

    private const string ContentJsonSchema = """
        {
            "type": "object",
            "properties": {
                "subjects": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": { "type": "string" },
                            "type": { "type": "string", "enum": ["person", "organisation"] },
                            "reason": { "type": "string" },
                            "occupation": { "type": "string" },
                            "website": { "type": "string" }
                        },
                        "required": ["name", "type", "reason", "occupation", "website"],
                        "additionalProperties": false
                    }
                },
                "keyInsight": { "type": "string" }
            },
            "required": ["subjects", "keyInsight"],
            "additionalProperties": false
        }
    """;

    private const string EnrichmentSystemPrompt = """
        You are a research librarian assistant.
        Using the document title, original description, and key section insights provided, write an enriched description (100-400 words) covering the full document.
        Be neutral and informative, written for a researcher who has not read the document.
        Incorporate insights from all sections, not just the introduction or conclusion.
        If no key insights are provided, reproduce and lightly improve the original description if necessary.
        ONLY state information provided, DO NOT infer.
    """;

    private static string GetEnrichmentJsonSchema() => """
        {
            "type": "object",
            "properties": {
                "enrichedDescription": { "type": "string" }
            },
            "required": ["enrichedDescription"],
            "additionalProperties": false
        }
    """;

    private const string EntityQcSystemPrompt = """
        You are a quality control assistant for a political research library.
        For each extracted entity you receive the name, type, reason it was extracted, and any database candidates.

        For each entity decide:
        1. isValid: true if this entity genuinely belongs in this document's subject matter given its reason and the document context. Mark false only when clearly wrong — e.g. an author of a cited work, a passing example, or completely unrelated to this document. Default to true when uncertain.
        2. confirmedMatchId: UUID of the candidate that is the exact same real-world entity — same person, same organisation. Use your general knowledge about the entity to verify: if you know what the extracted entity is and the candidate clearly refers to something different, reject it regardless of similarity score. Being in the same field, sector, or country is NOT sufficient. Do not pick the least-bad option — if no candidate is clearly the same entity, return empty string. A missed match is far better than a wrong one.
        3. suggestedAlias: If the extracted name is an alias or abbreviation of the confirmed match (e.g. "EU" for "European Union"), return that string. Otherwise empty string.
    """;

    private static string GetEntityQcJsonSchema() => """
        {
            "type": "object",
            "properties": {
                "decisions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": { "type": "string" },
                            "isValid": { "type": "boolean" },
                            "confirmedMatchId": { "type": "string" },
                            "suggestedAlias": { "type": "string" }
                        },
                        "required": ["name", "isValid", "confirmedMatchId", "suggestedAlias"]
                    }
                }
            },
            "required": ["decisions"],
            "additionalProperties": false
        }
    """;

    #endregion



    #region Public functions

    public async Task<ExtractedMetadata?> ExtractMetadataFromFileAsync(string text, string fileName, string headerFooterText = "", Action<string, int>? progress = null)
    {
        logger.Information("Starting metadata extraction for file: {FileName}", fileName);

        string trimmed = TrimTextForFile(text);
        string contextHint = $"Current date: {DateTime.UtcNow:yyyy-MM-dd}\nFileName: {fileName}";

        return await RunPipelineAsync(text, trimmed, contextHint, progress, headerFooterText);
    }

    public async Task<ExtractedMetadata?> ExtractMetadataFromWebAsync(ReadabilityResult readabilityResult, string url, Action<string, int>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(readabilityResult.TextContent))
        {
            logger.Warning("No text content to extract metadata from for URL: {Url}", url);
            return null;
        }

        logger.Information("Starting metadata extraction for URL: {Url}", url);

        bool readabilitySucceeded = !string.IsNullOrWhiteSpace(readabilityResult.Title)
            || !string.IsNullOrWhiteSpace(readabilityResult.Byline)
            || !string.IsNullOrWhiteSpace(readabilityResult.SiteName);

        string trimmed = readabilitySucceeded
            ? TrimTextForWeb(readabilityResult.TextContent)
            : TrimTextForFile(readabilityResult.TextContent);

        string contextHint = $"""
            Current date: {DateTime.UtcNow:yyyy-MM-dd}
            URL: {url}
            Pre-extracted fields (prefer these over body text):
            - Title: {readabilityResult.Title ?? "not extracted"}
            - Byline: {readabilityResult.Byline ?? "not extracted"}
            - Excerpt: {readabilityResult.Excerpt ?? "not extracted"}
            - SiteName: {readabilityResult.SiteName ?? "not extracted"}
        """;

        return await RunPipelineAsync(readabilityResult.TextContent, trimmed, contextHint, progress);
    }

    #endregion



    #region Helper functions

    private async Task<ExtractedMetadata?> RunPipelineAsync(string fullText, string trimmedText, string contextHint, Action<string, int>? progress, string headerFooterText = "")
    {
        // Fetch resource types
        ResourceType[] resourceTypes = await resourceTypeService.GetAllAsync();
        string biblioSchema = GetBiblioJsonSchema(resourceTypes.Select(r => r.Name));

        // PHASE 1: Extract bibliographic info (includes production entities)
        progress?.Invoke("Extracting bibliographic metadata...", 50);
        string biblioText = string.IsNullOrWhiteSpace(headerFooterText)
            ? trimmedText
            : trimmedText + "\n\n[Page headers/footers]:\n" + headerFooterText;
        string biblioPrompt = $"{contextHint}\n\nDocument text:\n{biblioText}";
        TempBiblio? biblio = await CallLLMAsync<TempBiblio>(BiblioSystemPrompt, biblioPrompt, biblioSchema);

        if (biblio == null)
        {
            logger.Warning("Biblio extraction returned null");
            return null;
        }

        // Strip back matter for subject extraction and publication code
        string strippedText = await StripBackMatterAsync(fullText);

        // PHASE 2: Extract subjects and key insights
        List<TempEntity> allSubjects = [];
        List<string> keyInsights = [];
        string[] chunks = ChunkText(strippedText, EntityChunkSize, EntityChunkOverlap);
        string docContext = $"Document title: {biblio.Title}\nDocument description: {biblio.Description}";

        // Search on each chunk
        for (int i = 0; i < chunks.Length; i++)
        {
            progress?.Invoke($"Extracting entities (chunk {i + 1}/{chunks.Length})...", 55 + (i * 20 / chunks.Length));

            if (i > 0) await Task.Delay(EntityChunkRequestIntervalMs);

            string chunkPrompt = $"{docContext}\n\n{chunks[i]}";
            TempSubjectList? result = await CallLLMAsync<TempSubjectList>(ContentSystemPrompt, chunkPrompt, ContentJsonSchema);

            if (result == null) continue;
            allSubjects.AddRange(result.Subjects);
            if (!string.IsNullOrWhiteSpace(result.KeyInsight))
                keyInsights.Add(result.KeyInsight);
        }

        // PHASE 3: Quality assurance and deduplication
        progress?.Invoke("Filtering duplicates...", 76);

        // Remove author duplicates and filter on common mistakes
        HashSet<string> authorNames = [.. biblio.Authors.Select(a => a.Name.Trim().ToLowerInvariant())];
        List<TempEntity> productions = GroundAndFilter(biblio.Productions, fullText, authorNames);
        List<TempEntity> subjects = GroundAndFilter(allSubjects, fullText, authorNames);

        // Remove all subjects already in production
        HashSet<string> productionNames = [.. productions.Select(e => e.Name.Trim().ToLowerInvariant())];
        subjects = [.. subjects.Where(e => !productionNames.Contains(e.Name.Trim().ToLowerInvariant()))];

        // PHASE 4: Find similars and build results
        progress?.Invoke("Matching against library...", 85);

        ExtractedMetadata extractedMetadata = await BuildMetadataAsync(biblio, productions, subjects);
        logger.Information("Metadata before QC: {Metadata}", JsonSerializer.Serialize(extractedMetadata, IndentedJson));
        progress?.Invoke("Running entity quality check...", 92);
        await RunEntityQcAsync(extractedMetadata, keyInsights);
        extractedMetadata.RelatedPersons.RemoveAll(e => !e.IsValid);
        extractedMetadata.Organisations.RemoveAll(e => !e.IsValid);
        logger.Information("Metadata after QC: {Metadata}", JsonSerializer.Serialize(extractedMetadata, IndentedJson));
        extractedMetadata.PublicationCode = ExtractPublicationCode(string.IsNullOrWhiteSpace(headerFooterText) ? strippedText : strippedText + "\n" + headerFooterText);
        extractedMetadata.LanguageCode = DetectLanguage(strippedText);

        return extractedMetadata;
    }

    private async Task<T?> CallLLMAsync<T>(string systemPrompt, string userPrompt, string jsonSchema, string? modelOverride = null) where T : class
    {
        // Prepare request
        MistralChatRequest request = new()
        {
            Messages = [new { role = "system", content = systemPrompt }, new { role = "user", content = userPrompt }],
            Temperature = 0.1f,
            ResponseFormat = MistralResponseFormat.JsonSchema,
            JsonSchema = JsonSerializer.Deserialize<object>(jsonSchema)
        };

        // Attempt multiple times (rate limits and unavailability happen)
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Wait for semaphore availability
                await AiSemaphore.WaitAsync();

                // Try LLM call
                try
                {
                    MistralCompletion completion = await mistralHttpClient.CompleteAsync(request, modelOverride: modelOverride);
                    if (string.IsNullOrWhiteSpace(completion.Content)) return null;
                    return JsonSerializer.Deserialize<T>(completion.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                finally { AiSemaphore.Release(); }
            }
            // Rate limit or unavailability request error
            catch (HttpRequestException ex) when (ex.Message.Contains("429") || ex.Message.Contains("503") || ex.Message.Contains("ServiceUnavailable"))
            {
                if (attempt == maxRetries) throw;
                int waitSeconds = ex.Message.Contains("429") ? 60 * attempt : 5 * attempt;
                logger.Warning("Transient error on LLM call. Waiting {Wait}s before retry {Attempt}/{Max}", waitSeconds, attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
            }
            // Other errors
            catch (Exception ex)
            {
                logger.Error(ex, "LLM call error (attempt {Attempt}/{Max})", attempt, maxRetries);
                if (attempt == maxRetries) throw;
                await Task.Delay(TimeSpan.FromSeconds(10 * attempt));
            }
        }

        return null;
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private async Task<ExtractedMetadata> BuildMetadataAsync(TempBiblio biblio, List<TempEntity> productions, List<TempEntity> subjects)
    {
        ExtractedMetadata metadata = new()
        {
            Title = NullIfEmpty(biblio.Title),
            Abstract = NullIfEmpty(biblio.Abstract),
            Description = NullIfEmpty(biblio.Description),
            Journal = NullIfEmpty(biblio.Journal),
            License = NullIfEmpty(biblio.License),
            ResourceTypeName = NullIfEmpty(biblio.ResourceType),
            Tags = [.. biblio.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim())],
            Regions = [.. biblio.Regions.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim())]
        };

        (metadata.PublicationDate, metadata.PublicationDatePrecision) = ParsePublicationDate(biblio.PublicationDate);

        HashSet<string> authorNames = [.. biblio.Authors.Select(a => a.Name.Trim().ToLowerInvariant())];

        var personItems = productions
            .Where(e => e.Type.Equals("person", StringComparison.OrdinalIgnoreCase))
            .Select(e => (e, RoleProduction))
            .Concat(subjects
                .Where(e => e.Type.Equals("person", StringComparison.OrdinalIgnoreCase))
                .Select(e => (e, RoleSubject)))
            .Where(x => !authorNames.Contains(x.e.Name.Trim().ToLowerInvariant()))
            .ToList();

        var orgItems = productions
            .Where(e => e.Type.Equals("organisation", StringComparison.OrdinalIgnoreCase))
            .Select(e => (e, RoleProduction))
            .Concat(subjects
                .Where(e => e.Type.Equals("organisation", StringComparison.OrdinalIgnoreCase))
                .Select(e => (e, RoleSubject)))
            .Where(x => !authorNames.Contains(x.e.Name.Trim().ToLowerInvariant()))
            .ToList();

        metadata.Authors = await ProcessAuthorsAsync(biblio.Authors);
        metadata.RelatedPersons = await ProcessEntitiesAsync(personItems, "person");
        metadata.Organisations = await ProcessEntitiesAsync(orgItems, "organisation");

        return metadata;
    }

    private async Task<List<AuthorWithSimilars>> ProcessAuthorsAsync(IEnumerable<TempEntity> authors)
    {
        var tasks = authors
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Select(async author =>
            {
                var similars = await FindSimilarsAsync(author.Name, ["person", "organisation"]);
                return new AuthorWithSimilars
                {
                    Name = author.Name.Trim(),
                    Type = author.Type.ToLowerInvariant(),
                    Reason = "Author of this document",
                    Occupation = NullIfEmpty(author.Occupation),
                    Email = NullIfEmpty(author.Email),
                    Similars = similars
                };
            });
        return [.. await Task.WhenAll(tasks)];
    }

    private async Task<List<EntityWithSimilars>> ProcessEntitiesAsync(IEnumerable<(TempEntity Entity, string Role)> items, string type)
    {
        var tasks = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Entity.Name))
            .Select(async item =>
            {
                var similars = await FindSimilarsAsync(item.Entity.Name, [type]);
                return new EntityWithSimilars
                {
                    Name = item.Entity.Name.Trim(),
                    Type = type,
                    Role = item.Role,
                    Reason = NullIfEmpty(item.Entity.Reason) ?? (item.Role == RoleProduction ? "Production contributor of this document" : null),
                    Occupation = NullIfEmpty(item.Entity.Occupation),
                    Website = NullIfEmpty(item.Entity.Website),
                    Email = NullIfEmpty(item.Entity.Email),
                    Similars = similars
                };
            });
        return [.. await Task.WhenAll(tasks)];
    }

    private static (DateTime? Date, PublicationDatePrecision? Precision) ParsePublicationDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (null, null);

        var parts = raw.Trim().Split('-');
        try
        {
            return parts.Length switch
            {
                1 when parts[0].Length == 4 => (new DateTime(int.Parse(parts[0]), 1, 1), PublicationDatePrecision.Year),
                2 when parts[0].Length == 4 => (new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), 1), PublicationDatePrecision.Month),
                3 when parts[0].Length == 4 => (new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2])), PublicationDatePrecision.Day),
                _ => (null, null)
            };
        }
        catch
        {
            return (null, null);
        }
    }

    private static List<TempEntity> GroundAndFilter(IEnumerable<TempEntity> entities, string sourceText, HashSet<string> authorNames) =>
        [.. entities
            .Where(e => !string.IsNullOrWhiteSpace(e.Name) && e.Name.Length > 2)
            .Where(e => !e.Name.Contains("et al.", StringComparison.OrdinalIgnoreCase))
            .Where(e => !e.Name.Contains("author", StringComparison.OrdinalIgnoreCase))
            .Where(e => !e.Name.Contains("anonymous", StringComparison.OrdinalIgnoreCase))
            .Where(e => !authorNames.Contains(e.Name.Trim().ToLowerInvariant()))
            .Where(e => sourceText.Contains(e.Name, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(e => e.Name.Trim().ToLowerInvariant())
        ];

    private async Task<List<SimilarEntity>> FindSimilarsAsync(string name, string[] typeFilter)
    {
        string nameLower = name.Trim().ToLowerInvariant();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (typeFilter.Contains("person"))
        {
            var p = await db.Persons
                .Where(x => !x.Trashed && x.Aliases.Any(a => a.ToLower() == nameLower))
                .Select(x => new SimilarEntity { Id = x.Id, Name = x.Name, Type = "person", IsQcConfirmed = true, Score = 1.0f, Description = x.Description })
                .FirstOrDefaultAsync();

            if (p != null) return [p];
        }

        if (typeFilter.Contains("organisation"))
        {
            var o = await db.Organisations
                .Where(x => !x.Trashed && x.Aliases.Any(a => a.ToLower() == nameLower))
                .Select(x => new SimilarEntity { Id = x.Id, Name = x.Name, Type = "organisation", IsQcConfirmed = true, Score = 1.0f, Description = x.Description })
                .FirstOrDefaultAsync();

            if (o != null) return [o];
        }

        await SearchSemaphore.WaitAsync();
        try
        {
            var result = await searchService.SearchAsync(
                name.Replace(".", ""),
                page: 1,
                pageSize: 6,
                filters: new Dictionary<string, object?> { { "type", typeFilter } }
            );

            return [.. result.Items.Where(i => i.RelevanceScore >= 0.55f).Select(i => new SimilarEntity { Id = i.Id, Name = i.Name, Type = i.Type, Score = i.RelevanceScore, Description = i.Description })];
        }
        finally
        {
            SearchSemaphore.Release();
        }
    }

    private async Task RunEntityQcAsync(ExtractedMetadata metadata, List<string> keyInsights)
    {
        var allEntities = metadata.Authors
            .Cast<EntityWithSimilars>()
            .Concat(metadata.RelatedPersons)
            .Concat(metadata.Organisations)
            .ToList();

        if (allEntities.Count == 0 && keyInsights.Count == 0) return;

        // STEP 1: Description enrichment (small model)
        StringBuilder enrichSb = new();
        enrichSb.AppendLine($"Document title: {metadata.Title}");
        enrichSb.AppendLine($"Original description: {metadata.Description}");
        if (keyInsights.Count > 0)
        {
            enrichSb.AppendLine("\nKey section insights:");
            foreach (string insight in keyInsights)
                enrichSb.AppendLine($"- {insight}");
        }

        EnrichmentResult? enrichResult = await CallLLMAsync<EnrichmentResult>(EnrichmentSystemPrompt, enrichSb.ToString(), GetEnrichmentJsonSchema(), modelOverride: smallModelName);
        if (!string.IsNullOrWhiteSpace(enrichResult?.EnrichedDescription))
            metadata.Description = enrichResult.EnrichedDescription;

        if (allEntities.Count == 0) return;

        // STEP 2: Entity QC (medium model) — uses enriched description as context
        StringBuilder qcSb = new();
        qcSb.AppendLine($"Document title: {metadata.Title}");
        qcSb.AppendLine($"Document description: {metadata.Description}");
        qcSb.AppendLine("\nEntities to validate:");
        foreach (EntityWithSimilars entity in allEntities)
        {
            qcSb.AppendLine($"\nName: \"{entity.Name}\" | Type: {entity.Type}");
            if (!string.IsNullOrWhiteSpace(entity.Reason))
                qcSb.AppendLine($"Reason: {entity.Reason}");
            if (entity.Similars.Count > 0)
            {
                qcSb.AppendLine("Database candidates:");
                foreach (SimilarEntity similar in entity.Similars)
                {
                    string desc = string.IsNullOrWhiteSpace(similar.Description) ? "no description" : similar.Description[..Math.Min(similar.Description.Length, 100)];
                    qcSb.AppendLine($"  - ID: {similar.Id} | Name: {similar.Name} | Similarity: {similar.Score:P0} | Description: {desc}");
                }
            }
            else qcSb.AppendLine("Database candidates: none");
        }

        EntityQcResult? qcResult = await CallLLMAsync<EntityQcResult>(EntityQcSystemPrompt, qcSb.ToString(), GetEntityQcJsonSchema(), modelOverride: mediumModelName);
        if (qcResult == null) return;

        var decisionMap = qcResult.Decisions.ToDictionary(d => d.Name.Trim().ToLowerInvariant());

        foreach (var entity in allEntities)
        {
            if (!decisionMap.TryGetValue(entity.Name.Trim().ToLowerInvariant(), out var decision)) continue;

            if (!decision.IsValid)
            {
                entity.IsValid = false;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(decision.ConfirmedMatchId) && Guid.TryParse(decision.ConfirmedMatchId, out Guid matchId))
            {
                var confirmed = entity.Similars.FirstOrDefault(s => s.Id == matchId);
                if (confirmed != null)
                {
                    confirmed.IsQcConfirmed = true;
                    if (!string.IsNullOrWhiteSpace(decision.SuggestedAlias) && !decision.SuggestedAlias.Equals(confirmed.Name, StringComparison.OrdinalIgnoreCase))
                        confirmed.SuggestedAlias = decision.SuggestedAlias;
                }
            }
        }
    }

    private static string TrimText(string text, int firstChars, int lastChars)
    {
        int maxTotal = firstChars + lastChars;
        if (text.Length <= maxTotal) return text;

        string beginning = text[..firstChars];
        if (lastChars == 0) return beginning + "\n\n[...rest of document omitted...]";

        string ending = text[^lastChars..];
        return beginning + "\n\n[...middle section omitted...]\n\n" + ending;
    }

    private static string[] ChunkText(string text, int chunkSize = 4000, int overlap = 300)
    {
        List<string> chunks = [];
        int start = 0;

        while (start < text.Length)
        {
            int end = Math.Min(start + chunkSize, text.Length);
            chunks.Add(text[start..end]);
            if (end == text.Length) break;
            start = end - overlap;
        }

        return chunks.ToArray();
    }

    private static string? ExtractPublicationCode(string text)
    {
        var doi = DoiRegex.Match(text);
        if (doi.Success) return $"DOI: {doi.Value.TrimEnd('.', ',', ')', ']')}";

        var arxiv = ArxivRegex.Match(text);
        if (arxiv.Success) return $"arXiv: {arxiv.Groups[1].Value}";

        var pmid = PmidRegex.Match(text);
        if (pmid.Success) return $"PMID: {pmid.Groups[1].Value}";

        var issn = IssnRegex.Match(text);
        if (issn.Success) return $"ISSN: {issn.Groups[1].Value}";

        var isbn13 = Isbn13Regex.Match(text);
        if (isbn13.Success) return $"ISBN: {isbn13.Value}";

        var isbn10 = Isbn10Regex.Match(text);
        if (isbn10.Success) return $"ISBN: {isbn10.Value}";

        return null;
    }

    private static string? DetectLanguage(string text)
    {
        string sample = text.Length > 500 ? text[..500] : text;
        Language? lang = languageDetector.DetectLanguageOf(sample);

        return lang?.IsoCode6391().ToString().ToLowerInvariant();
    }

    private async Task<string> StripBackMatterAsync(string text)
    {
        text = UnicodeSuperscriptLineRegex.Replace(text, "");
        text = LatexFootnoteMarkerRegex.Replace(text, "");

        List<string> headings = [.. HeadingRegex.Matches(text).Select(m => m.Value.Trim()).Distinct()];
        if (headings.Count == 0) return text;

        string headingList = string.Join("\n", headings);
        string prompt = $"Here are section headings from a document:\n{headingList}\n\nReturn the headings that are back matter (references, bibliography, notes, acknowledgements, appendices, or any equivalent in any language). Return empty array if none qualify.";
        string schema = """{"type":"object","properties":{"backMatterHeadings":{"type":"array","items":{"type":"string"}}},"required":["backMatterHeadings"],"additionalProperties":false}""";

        BackMatterResult? result = await CallLLMAsync<BackMatterResult>("You are a document structure analyzer. Identify which section headings are back matter.", prompt, schema, modelOverride: smallModelName);

        if (result == null || result.BackMatterHeadings.Count == 0) return text;

        int midpoint = text.Length / 2;
        int? cutIndex = null;

        foreach (string heading in result.BackMatterHeadings)
        {
            string normalizedHeading = heading.TrimStart('#').Trim();

            var match = HeadingRegex.Matches(text).Cast<Match>().FirstOrDefault(m => m.Index >= midpoint &&
                m.Value.TrimStart('#').Trim().Equals(normalizedHeading, StringComparison.OrdinalIgnoreCase));

            if (match != null && (cutIndex == null || match.Index < cutIndex))
                cutIndex = match.Index;
        }

        return cutIndex.HasValue ? text[..cutIndex.Value] : text;
    }

    #endregion



    #region Internal Classes

    internal class BackMatterResult
    {
        public List<string> BackMatterHeadings { get; set; } = [];
    }

    internal class TempEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string? Occupation { get; set; }
        public string? Website { get; set; }
        public string? Email { get; set; }
    }

    internal class TempBiblio
    {
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PublicationDate { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string Journal { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public List<TempEntity> Authors { get; set; } = [];
        public List<TempEntity> Productions { get; set; } = [];
        public List<string> Tags { get; set; } = [];
        public List<string> Regions { get; set; } = [];
    }

    internal class TempSubjectList
    {
        public List<TempEntity> Subjects { get; set; } = [];
        public string KeyInsight { get; set; } = string.Empty;
    }

    internal class TempVerdict
    {
        public string Name { get; set; } = string.Empty;
        public bool Keep { get; set; }
    }

    internal class TempValidationResult
    {
        public List<TempVerdict> Verdicts { get; set; } = [];
        public string Description { get; set; } = string.Empty;
    }

    internal class EnrichmentResult
    {
        public string? EnrichedDescription { get; set; }
    }

    internal class QcDecision
    {
        public string Name { get; set; } = "";
        public bool IsValid { get; set; } = true;
        public string? ConfirmedMatchId { get; set; }
        public string? SuggestedAlias { get; set; }
    }

    internal class EntityQcResult
    {
        public List<QcDecision> Decisions { get; set; } = [];
    }

    #endregion
}