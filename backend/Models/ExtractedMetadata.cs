using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

/// <summary>
/// Metadata extracted from a document using LLM analysis
/// </summary>
public class ExtractedMetadata
{
    public string? Title { get; set; }
    public string? Abstract { get; set; }
    public string? Description { get; set; }
    public DateTime? PublicationDate { get; set; }
    public PublicationDatePrecision? PublicationDatePrecision { get; set; }
    public string? LanguageCode { get; set; }
    public string? Journal { get; set; }
    public string? License { get; set; }
    public string? ResourceTypeName { get; set; }
    public List<EntityWithSimilars> Authors { get; set; } = [];
    public List<EntityWithSimilars> Organisations { get; set; } = [];
    public List<EntityWithSimilars> RelatedPersons { get; set; } = [];
    public string? PublicationCode { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<string> Regions { get; set; } = [];
}

/// <summary>
/// Represents an entity (person or organisation) with potential similar entities from the database
/// </summary>
public class EntityWithSimilars
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "person" or "organisation" - the suggested type for new entities
    public string? Role { get; set; }
    public string? Reason { get; set; }
    public string? Occupation { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    [JsonIgnore] public List<SimilarEntity> Similars { get; set; } = [];
    public SimilarEntity? ConfirmedMatch { get; set; }
    public List<string> SuggestedAliases { get; set; } = [];
    [JsonIgnore] public bool IsValid { get; set; } = true;
}

/// <summary>
/// Represents a similar entity found in the database during metadata extraction
/// </summary>
public class SimilarEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> SuggestedAliases { get; set; } = [];
    [JsonIgnore] public string? ExistingEmail { get; set; }
    [JsonIgnore] public string? ExistingWebsite { get; set; }
    [JsonIgnore] public string? ExistingOccupation { get; set; }
    [JsonIgnore] public float Score { get; set; }
    [JsonIgnore] public string? Description { get; set; }
}
