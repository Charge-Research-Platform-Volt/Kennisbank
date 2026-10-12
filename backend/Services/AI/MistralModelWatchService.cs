using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace KnowledgeBank.Services.AI;

/// <summary>
/// Checks Mistral's model list daily for newer versions of the pinned models. Registered as a singleton so the
/// dashboard can read the last result and trigger a check on demand.
/// </summary>
public class MistralModelWatchService(MistralHttpClient mistralClient, EnvironmentConfig environmentConfig, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly Serilog.ILogger logger = Log.ForContext<MistralModelWatchService>();
    private readonly SemaphoreSlim refreshLock = new(1, 1);

    private ModelFamily[] Families =>
    [
        new("OCR", "mistral-ocr", environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_OCR_MODEL_NAME)),
        new("Small", "mistral-small", environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_SMALL_MODEL_NAME)),
        new("Medium", "mistral-medium", environmentConfig.GetVariableValue(EnvironmentVariable.MISTRAL_MEDIUM_MODEL_NAME)),
        // Not used by the app. Pinned to the current default so it's flagged once Large 4 leaves preview and becomes -latest
        new("Large", "mistral-large", "mistral-large-2512", UsedByApp: false)
    ];

    public IReadOnlyList<ModelFamilyStatus> Current { get; private set; } = [];
    public DateTime? LastCheckedUtc { get; private set; }
    public string? LastError { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromHours(24));
        do await RefreshAsync(stoppingToken);
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await refreshLock.WaitAsync(ct);
        try
        {
            List<MistralModelCard> cards = await mistralClient.GetModelCardsAsync(ct);
            IReadOnlySet<string> acknowledged = await RecordSightingsAsync(cards, ct);

            Current = Families.Select(f => ModelUpdateChecker.Check(f, cards, acknowledged)).ToList();
            LastCheckedUtc = DateTime.UtcNow;
            LastError = null;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            LastError = ex.Message;
            logger.Warning(ex, "Failed to check Mistral's model list for updates");
        }
        finally
        {
            refreshLock.Release();
        }
    }

    public async Task AcknowledgeAllAsync(CancellationToken ct = default)
    {
        using (IServiceScope scope = scopeFactory.CreateScope())
        {
            DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            await db.KnownMistralModels
                .Where(m => !m.Acknowledged)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Acknowledged, true), ct);
        }

        await RefreshAsync(ct);
    }

    /// <summary>
    /// Stores model ids not seen before and returns the acknowledged ones. On the very first run everything is stored
    /// as acknowledged, so only models released after that show up as new.
    /// </summary>
    private async Task<IReadOnlySet<string>> RecordSightingsAsync(List<MistralModelCard> cards, CancellationToken ct)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        List<KnownMistralModel> known = await db.KnownMistralModels.ToListAsync(ct);
        bool firstRun = known.Count == 0;
        HashSet<string> knownIds = known.Select(m => m.Id).ToHashSet();

        List<KnownMistralModel> added = cards
            .Where(c => !knownIds.Contains(c.Id))
            .Select(c => new KnownMistralModel { Id = c.Id, Acknowledged = firstRun })
            .ToList();

        if (added.Count > 0)
        {
            db.KnownMistralModels.AddRange(added);
            await db.SaveChangesAsync(ct);

            if (!firstRun)
                logger.Information("New Mistral models listed: {Models}", string.Join(", ", added.Select(m => m.Id)));
        }

        return known.Concat(added).Where(m => m.Acknowledged).Select(m => m.Id).ToHashSet();
    }
}
