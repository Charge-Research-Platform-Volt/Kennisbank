using KnowledgeBank.Models;
using KnowledgeBank.Services.Search;
using Lingua;
using Serilog;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KnowledgeBank.Services.AI;

public class MetadataExtractionService(MistralHttpClient mistralHttpClient, HybridSearchService searchService)
{
    private readonly Serilog.ILogger logger = Log.ForContext<MetadataExtractionService>();
    private static readonly LanguageDetector languageDetector = LanguageDetectorBuilder.FromAllLanguages().WithPreloadedLanguageModels().Build();

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
    private static readonly Regex DoiRegex = new(@"10\.\d{4,}/[^\s,;)""\]]+", RegexOptions.Compiled);
    private static readonly Regex ArxivRegex = new(@"arXiv:\s*(\d{4}\.\d{4,5})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Isbn13Regex = new(@"978[-\s]?\d[-\s]?\d{1,5}[-\s]?\d{1,7}[-\s]?\d{1,7}[-\s]?\d", RegexOptions.Compiled);
    private static readonly Regex Isbn10Regex = new(@"\b\d{9}[\dX]\b", RegexOptions.Compiled);
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

    private static string TrimTextForFile(string text) => TrimText(text, firstChars: 16000, lastChars: 4000);
    private static string TrimTextForWeb(string text) => TrimText(text, firstChars: 8000, lastChars: 0);

    private const string systemPrompt = """
        You are a metadata extraction assistant.

        Rules:
        - Only extract information explicitly present in the document. Do not infer, guess, or assume values. Use null or empty array when a field cannot be found.
        - Publication date formats: YYYY, YYYY-MM, or YYYY-MM-DD. Use the most specific format the document allows.
        - If the document uses relative dates ("today", "yesterday", "vandaag", "gisteren", etc.), calculate the absolute date using the Current date provided above.
        - Tags must be capitalized, but not in full-caps (e.g. "Machine Learning", not "machine learning" or "MACHINE LEARNING").
        - Deduplication: each person or organisation must appear only once per list. When variations exist, use the most complete version.
        - Person names must follow Given name(s) FIRST, Family name LAST. Reorder any "Last, First" formatted names from citations.

        Field distinctions:
        - AUTHORS: Who wrote or created this document (persons or organisations).
            * Look for bylines, "by", "door", or similar attribution markers.
            * If attribution refers to an internal team or staff ("our newsroom", "onze redactie", "by staff"), the publishing organisation is the author - find its name in the document.
            * If no attribution is found, leave empty.
        - ORGANISATIONS: Organisations mentioned or associated with the document (publishers, sources, subject).
            * An organisation can appear in both authors and organisations.
        - RELATEDPERSONS: People mentioned in the document who are not authors (cited, quoted, acknowledged, discussed)
    """;

    private const string MetadataExtractionOutputJsonSchema = """
        {
            "title": "Metadata Extraction",
            "type": "object",
            "properties": {
                "title": { "anyOf": [{"type": "string"}, {"type": "null"}], "description": "The document title" },
                "abstract": { "anyOf": [{"type": "string"}, {"type": "null"}], "description": "The abstract of the paper when scientific, otherwise null" },
                "description": { "anyOf": [{"type": "string"}, {"type": "null"}], "description": "A concise description of the document (50-300 words)" },
                "publicationDate": { "anyOf": [{"type": "string"}, {"type": "null"}], "description": "Publication date in YYYY, YYYY-MM, or YYYY-MM-DD format, or null" },
                "authors": {
                    "type": "array",
                    "description": "Document authors (persons or organisations)",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": { "type": "string" },
                            "type": { "type": "string", "enum": ["person", "organisation"] }
                        },
                        "required": ["name", "type"],
                        "additionalProperties": false
                    }
                },
                "organisations": {
                    "type": "array",
                    "description": "Organisation names associated with the document",
                    "items": { "type": "string" }
                },
                "relatedPersons": {
                    "type": "array",
                    "description": "Person names mentioned in the document who are not authors",
                    "items": { "type": "string" }
                },
                "tags": {
                    "type": "array",
                    "description": "Relevant tags for the document",
                    "items": { "type": "string" }
                }
            },
            "required": ["title", "abstract", "description", "publicationDate", "authors", "organisations", "relatedPersons", "tags"],
            "additionalProperties": false
        }
    """;

    public async Task<ExtractedMetadata?> ExtractMetadataFromFileAsync(string text, string fileName, Action? progressCallback = null)
    {
        logger.Information("Starting metadata extraction for file: {FileName}", fileName);

        string trimmed = TrimTextForFile(text);

        string prompt = $"""
            Current date: {DateTime.UtcNow:yyyy-MM-dd}
            Filename: {fileName}

            Document text:
            {trimmed}
        """;

        TempExtractedMetadata? temp = await ExtractMetadataWithLLMAsync(prompt);

        if (temp == null)
        {
            logger.Warning("Failed to extract metadata from LLM response for file: {FileName}", fileName);
            return null;
        }

        // Manual extraction
        temp.PublicationCode = ExtractPublicationCode(text);
        temp.LanguageCode = DetectLanguage(text);

        progressCallback?.Invoke();
        return await ValidateAndProcessMetadataAsync(temp);
    }

    public async Task<ExtractedMetadata?> ExtractMetadataFromWebAsync(ReadabilityResult readabilityResult, string url, Action? progressCallback = null)
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

        string prompt = $"""
            Current date: {DateTime.UtcNow:yyyy-MM-dd}
            URL: {url}

            Pre-extracted fields (prefer these over body text):
            - Title: {readabilityResult.Title ?? "not extracted"}
            - Byline: {readabilityResult.Byline ?? "not extracted"}
            - Excerpt: {readabilityResult.Excerpt ?? "not extracted"}
            - SiteName: {readabilityResult.SiteName ?? "not extracted"}

            Extracted text:
            {trimmed}
        """;

        TempExtractedMetadata? temp = await ExtractMetadataWithLLMAsync(prompt);
        if (temp == null)
        {
            logger.Warning("Failed to extract metadata from LLM response for URL: {Url}", url);
            return null;
        }

        // Manual extraction
        temp.PublicationCode = ExtractPublicationCode(readabilityResult.TextContent);
        temp.LanguageCode = DetectLanguage(readabilityResult.TextContent);

        progressCallback?.Invoke();
        return await ValidateAndProcessMetadataAsync(temp);
    }

    private async Task<TempExtractedMetadata?> ExtractMetadataWithLLMAsync(string prompt)
    {
        var request = new MistralChatRequest
        {
            Messages = [
                new { role = "system", content = systemPrompt },
                new { role = "user", content = prompt }
            ],
            Temperature = 0.1f,
            ResponseFormat = MistralResponseFormat.JsonSchema,
            JsonSchema = JsonSerializer.Deserialize<object>(MetadataExtractionOutputJsonSchema)
        };

        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                MistralCompletion completion = await mistralHttpClient.CompleteAsync(request);
                if (string.IsNullOrWhiteSpace(completion.Content)) return null;

                return JsonSerializer.Deserialize<TempExtractedMetadata>(completion.Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                if (attempt == maxRetries) throw;

                int waitSeconds = 60 * attempt;
                logger.Warning("Rate limit hit. {Wait}s before retry {Attempt}/{Max}", waitSeconds, attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
            }
        }

        return null;
    }

    private async Task<ExtractedMetadata?> ValidateAndProcessMetadataAsync(TempExtractedMetadata temp)
    {
        try
        {
            var metadata = new ExtractedMetadata
            {
                Title = temp.Title?.Trim(),
                Abstract = temp.Abstract?.Trim(),
                Description = temp.Description?.Trim(),
                PublicationCode = temp.PublicationCode?.Trim(),
                Tags = temp.Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList() ?? [],
            };

            // Language code: must be exactly 2 lowercase letters
            if (!string.IsNullOrEmpty(temp.LanguageCode))
            {
                string code = temp.LanguageCode.ToLowerInvariant();
                metadata.LanguageCode = code.Length == 2 ? code : null;
            }

            // Publication date: parse partial dates (YYYY / YYYY-MM / YYYY-MM-DD)
            (metadata.PublicationDate, metadata.PublicationDatePrecision) = ParsePublicationDate(temp.PublicationDate);

            // Entity processing — deduplicate orgs/persons that already appear as authors
            var authorOrgNames = (temp.Authors ?? [])
                .Where(a => a.Type.Equals("organisation", StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Name.Trim().ToLowerInvariant())
                .ToHashSet();
            var authorPersonNames = (temp.Authors ?? [])
                .Where(a => a.Type.Equals("person", StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Name.Trim().ToLowerInvariant())
                .ToHashSet();

            metadata.Authors = await ProcessAuthorsAsync(temp.Authors ?? []);
            metadata.Organisations = await ProcessEntitiesAsync(
                (temp.Organisations ?? []).Where(o => !authorOrgNames.Contains(o.Trim().ToLowerInvariant())).ToList(),
                ["organisation"]);
            metadata.RelatedPersons = await ProcessEntitiesAsync(
                (temp.RelatedPersons ?? []).Where(p => !authorPersonNames.Contains(p.Trim().ToLowerInvariant())).ToList(),
                ["person"]);

            return metadata;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error validating and processing metadata");
            return null;
        }
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

    private async Task<List<EntityWithSimilars>> ProcessEntitiesAsync(List<string> names, string[] typeFilter)
    {
        var filtered = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

        var tasks = filtered.Select(async name =>
        {
            var similars = await FindSimilarsAsync(name, typeFilter);
            return new EntityWithSimilars { Name = name.Trim(), Type = typeFilter[0], Similars = similars };
        });

        return [.. await Task.WhenAll(tasks)];
    }

    private async Task<List<AuthorWithSimilars>> ProcessAuthorsAsync(List<TempAuthor> authors)
    {
        var filtered = authors.Where(a => !string.IsNullOrWhiteSpace(a.Name)).ToList();

        var tasks = filtered.Select(async author =>
        {
            var similars = await FindSimilarsAsync(author.Name, ["person", "organisation"]);
            return new AuthorWithSimilars { Name = author.Name.Trim(), Type = author.Type.ToLowerInvariant().Trim(), Similars = similars };
        });

        return [.. await Task.WhenAll(tasks)];
    }

    private async Task<List<SimilarEntity>> FindSimilarsAsync(string name, string[] typeFilter)
    {
        var result = await searchService.SearchAsync(
            name.Replace(".", ""),
            page: 1,
            pageSize: 3,
            filters: new Dictionary<string, object?> { { "type", typeFilter } }
        );

        return [.. result.Items.Where(i => i.RelevanceScore >= 0.5f).Select(i => new SimilarEntity { Id = i.Id, Name = i.Name, Score = i.RelevanceScore, Type = i.Type })];
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

    private static string? ExtractPublicationCode(string text)
    {
        var doi = DoiRegex.Match(text);
        if (doi.Success) return $"DOI: {doi.Value.TrimEnd('.', ',', ')', ']')}";

        var arxiv = ArxivRegex.Match(text);
        if (arxiv.Success) return $"arXiv: {arxiv.Groups[1].Value}";

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
}

/// <summary>
/// Temporary class for deserializing LLM JSON response (before enrichment with similars)
/// </summary>
internal class TempExtractedMetadata
{
    public string? Title { get; set; }
    public string? Abstract { get; set; }
    public string? Description { get; set; }
    public string? PublicationDate { get; set; }  // String to support partial dates (YYYY, YYYY-MM, YYYY-MM-DD)
    public string? LanguageCode { get; set; }
    public List<TempAuthor> Authors { get; set; } = [];
    public List<string> Organisations { get; set; } = [];
    public List<string> RelatedPersons { get; set; } = [];
    public string? PublicationCode { get; set; }
    public List<string> Tags { get; set; } = [];
}

/// <summary>
/// Temporary class for author with type information from LLM
/// </summary>
internal class TempAuthor
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "person" or "organisation"
}
