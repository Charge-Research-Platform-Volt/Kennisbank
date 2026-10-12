using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace KnowledgeBank.Services.AI;

/// <summary>
/// A model family to watch. Configured is the pinned id to compare against, or null to only list what's available.
/// UsedByApp is false for families watched only as a reference, e.g. to notice when a preview model becomes the default.
/// </summary>
public record ModelFamily(string Name, string Prefix, string? Configured, bool UsedByApp = true);

[JsonConverter(typeof(JsonStringEnumConverter<ModelUpdateStatus>))]
public enum ModelUpdateStatus { UpToDate, UpdateAvailable, NotPinned, Missing, WatchOnly, Unknown }

/// <summary>
/// One model in a family, with all of its names (e.g. mistral-large-4 and mistral-large-4-0).
/// </summary>
public record FamilyModel(List<string> Names, bool IsNew, bool IsLatest, bool IsConfigured);

public record ModelFamilyStatus(
    string Name,
    string? Configured,
    bool UsedByApp,
    ModelUpdateStatus Status,
    string? Latest,
    DateTime? Deprecation,
    string? DeprecationReplacement,
    List<FamilyModel> Models
);

public static class ModelUpdateChecker
{
    public static ModelFamilyStatus Check(ModelFamily family, IReadOnlyList<MistralModelCard> cards, IReadOnlySet<string> acknowledgedIds)
    {
        string memberPrefix = family.Prefix + "-";
        MistralModelCard? latestCard = cards.FirstOrDefault(c => c.Id == family.Prefix + "-latest");
        MistralModelCard? pinnedCard = family.Configured == null ? null : cards.FirstOrDefault(c => c.Id == family.Configured);

        ModelUpdateStatus status = family.Configured switch
        {
            null => ModelUpdateStatus.WatchOnly,
            string c when c.EndsWith("-latest") => ModelUpdateStatus.NotPinned,
            _ when pinnedCard == null => ModelUpdateStatus.Missing,
            _ when latestCard == null => ModelUpdateStatus.Unknown,
            string c => latestCard.IsKnownAs(c) ? ModelUpdateStatus.UpToDate : ModelUpdateStatus.UpdateAvailable
        };

        // Every alias is listed as its own card, so collapse cards into one entry per model.
        // A model counts as new only when none of its names were seen before, so a new alias on an old model doesn't flag it
        List<FamilyModel> models = cards
            .Where(c => c.Id.StartsWith(memberPrefix) && !c.Id.EndsWith("-latest"))
            .Select(c => c.Names.Where(n => n.StartsWith(memberPrefix) && !n.EndsWith("-latest")).Order().ToList())
            .DistinctBy(names => string.Join(",", names))
            .Select(names => new FamilyModel(
                names,
                IsNew: !names.Any(acknowledgedIds.Contains),
                IsLatest: latestCard != null && names.Any(latestCard.IsKnownAs),
                IsConfigured: family.Configured != null && names.Contains(family.Configured)))
            .ToList();

        return new ModelFamilyStatus(
            family.Name,
            family.Configured,
            family.UsedByApp,
            status,
            latestCard == null ? null : PreferredName(latestCard.Names.Where(n => n.StartsWith(memberPrefix))),
            pinnedCard?.Deprecation,
            pinnedCard?.DeprecationReplacement,
            models
        );
    }

    // Dated ids (mistral-small-2603) are the most stable names; otherwise the most specific one (mistral-ocr-4-1 over mistral-ocr-4)
    private static string? PreferredName(IEnumerable<string> names)
    {
        List<string> candidates = names.Where(n => !n.EndsWith("-latest")).ToList();
        return candidates.FirstOrDefault(n => DatedIdPattern.IsMatch(n)) ?? candidates.OrderByDescending(n => n.Length).FirstOrDefault();
    }

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
    private static readonly Regex DatedIdPattern = new(@"-\d{4}$", RegexOptions.Compiled);
#pragma warning restore SYSLIB1045
}
