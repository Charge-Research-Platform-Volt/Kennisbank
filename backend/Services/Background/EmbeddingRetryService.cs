using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace KnowledgeBank.Services.Background;

/// <summary>
/// Periodically retries failed embeddings with increasing waits between attempts, and gives up
/// after <see cref="MaxFailures"/> failures so a permanently broken item isn't retried forever.
/// A successful run or a manual re-embed resets the failure count. 
/// </summary>
public class EmbeddingRetryService(IServiceProvider serviceProvider, ReembedRunState reembedRunState) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    // Wait before the next retry, indexed by (failures so far -1)
    private static readonly TimeSpan[] Backoff = [
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(6),
        TimeSpan.FromHours(24)
    ];

    public static readonly int MaxFailures = Backoff.Length + 1;

    private readonly Serilog.ILogger logger = Log.ForContext<EmbeddingRetryService>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.Information("Embedding retry service started.");

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // An admin re-embed is already working through items; don't process them twice
                    if (!reembedRunState.IsRunning)
                        await RunPassAsync(stoppingToken);
                    else
                        logger.Information("Skipping embedding retry service run, since re-embedding is running.");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.Error(ex, "Embedding retry pass failed.");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) {}
    }

    private async Task RunPassAsync(CancellationToken stoppingToken)
    {
        DateTime now = DateTime.UtcNow;
        Guid[] resourceIds, personIds, organisationIds;
        List<(Guid Id, bool Preview, bool Embedding)> versionRetries;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            var resources = await db.Resources
                .Where(r => !r.Trashed && r.EmbeddingStatus == EmbeddingStatus.Failed && r.EmbeddingFailures < MaxFailures)
                .Select(r => new { r.Id, r.EmbeddingFailures, r.EmbeddingLastAttempt })
                .ToListAsync(stoppingToken);

            var persons = await db.Persons
                .Where(p => !p.Trashed && p.EmbeddingStatus == EmbeddingStatus.Failed && p.EmbeddingFailures < MaxFailures)
                .Select(p => new { p.Id, p.EmbeddingFailures, p.EmbeddingLastAttempt })
                .ToListAsync(stoppingToken);

            var organisations = await db.Organisations
                .Where(o => !o.Trashed && o.EmbeddingStatus == EmbeddingStatus.Failed && o.EmbeddingFailures < MaxFailures)
                .Select(o => new { o.Id, o.EmbeddingFailures, o.EmbeddingLastAttempt })
                .ToListAsync(stoppingToken);

            resourceIds = [.. resources.Where(r => IsDue(r.EmbeddingFailures, r.EmbeddingLastAttempt, now)).Select(r => r.Id)];
            personIds = [.. persons.Where(p => IsDue(p.EmbeddingFailures, p.EmbeddingLastAttempt, now)).Select(p => p.Id)];
            organisationIds = [.. organisations.Where(o => IsDue(o.EmbeddingFailures, o.EmbeddingLastAttempt, now)).Select(o => o.Id)];
        
            var versions = await db.ProjectDocumentVersions
                .Where(v => (v.EmbeddingStatus == EmbeddingStatus.Failed && v.EmbeddingFailures < MaxFailures)
                    || (v.PreviewStatus == EmbeddingStatus.Failed && v.PreviewFailures < MaxFailures))
                .Select(v => new { v.Id, v.EmbeddingStatus, v.EmbeddingFailures, v.EmbeddingLastAttempt, v.PreviewStatus, v.PreviewFailures, v.PreviewLastAttempt })
                .ToListAsync(stoppingToken);

            // Each step keeps its own backoff, so a due preview doesn't drag along an embedding that's still waiting
            versionRetries = [.. versions
                .Select(v => (
                    v.Id,
                    Preview: IsStepDue(v.PreviewStatus, v.PreviewFailures, v.PreviewLastAttempt, now),
                    Embedding: IsStepDue(v.EmbeddingStatus, v.EmbeddingFailures, v.EmbeddingLastAttempt, now)
                )).Where(v => v.Preview || v.Embedding)];
        }   

        if (resourceIds.Length + personIds.Length + organisationIds.Length + versionRetries.Count == 0) return;

        logger.Information("Retrying failed embeddings: {Resources} resources, {Persons} persons, {Organisations} organisations, {Versions} project document versions",
            resourceIds.Length, personIds.Length, organisationIds.Length, versionRetries.Count);

        await ConcurrencyUtils.RunBoundedAsync(resourceIds, 3, async id =>
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IngestionService>().RunResourcePipelineFromStorageAsync(id);
        }, stoppingToken);

        await ConcurrencyUtils.RunBoundedAsync(personIds, 5, async id =>
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            try { await scope.ServiceProvider.GetRequiredService<IngestionService>().RunPersonEntityPipelineAsync(id); }
            catch (Exception ex) { logger.Error(ex, "Failed to retry embedding for person {Id}", id); }
        }, stoppingToken);

        await ConcurrencyUtils.RunBoundedAsync(organisationIds, 5, async id =>
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            try { await scope.ServiceProvider.GetRequiredService<IngestionService>().RunOrganisationEntityPipelineAsync(id); }
            catch (Exception ex) { logger.Error(ex, "Failed to retry embedding for organisation {Id}", id); }
        }, stoppingToken);

        await ConcurrencyUtils.RunBoundedAsync(versionRetries, 3, async retry =>
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            try { await scope.ServiceProvider.GetRequiredService<IngestionService>().RunProjectDocumentPipelineAsync(retry.Id, retry.Preview, retry.Embedding); }
            catch (Exception ex) { logger.Error(ex, "Failed to retry project document version {Id}", retry.Id); }
        }, stoppingToken);
    }

    // Never-counted failures (interrupted by a restart, or failed before the pipeline started) are due immediately
    internal static bool IsDue(int failures, DateTime? lastAttempt, DateTime now)
        => failures == 0 || lastAttempt == null || now - lastAttempt.Value >= Backoff[failures - 1];

    /// <summary>
    /// Whether one step of a multi-step item (such as a project document's preview or embedding) should be retried now.
    /// The failure cap is checked first because <see cref="IsDue"/> has no backoff entry past it.
    /// </summary>
    internal static bool IsStepDue(EmbeddingStatus status, int failures, DateTime? lastAttempt, DateTime now)
        => status == EmbeddingStatus.Failed && failures < MaxFailures && IsDue(failures, lastAttempt, now);
}