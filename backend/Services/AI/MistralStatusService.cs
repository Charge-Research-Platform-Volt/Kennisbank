using Hubs;
using Microsoft.AspNetCore.SignalR;

namespace KnowledgeBank.Services.AI;

public enum MistralAvailability { Operational, Down }

public record MistralStatusSnapshot(MistralAvailability Status, DateTime? DownSinceUtc, DateTime LastCheckedUtc, string? LastError);

/// <summary>
/// Tracks whether Mistral's API currently appears reachable, based purely on the outcome of real calls
/// (MistralHttpClient's chat/streaming/OCR methods). Only flips to Down after a couple of consecutive
/// failures, to avoid crying wolf over one flaky request; any single success clears it immediately.
/// </summary>
public class MistralStatusService(IHubContext<Chat> hubContext)
{
    private const int FailureThreshold = 2;

    private readonly Serilog.ILogger logger = Serilog.Log.ForContext<MistralStatusService>();
    private readonly Lock statusLock = new();
    private MistralAvailability status = MistralAvailability.Operational;
    private DateTime? downSinceUtc;
    private DateTime lastCheckedUtc = DateTime.UtcNow;
    private string? lastError;
    private int consecutiveFailures;

    public MistralStatusSnapshot Current
    {
        get { lock (statusLock) return new(status, downSinceUtc, lastCheckedUtc, lastError); }
    }

    public async Task ReportSuccess()
    {
        MistralStatusSnapshot? changed = null;

        lock (statusLock)
        {
            consecutiveFailures = 0;
            lastCheckedUtc = DateTime.UtcNow;

            if (status == MistralAvailability.Down)
            {
                status = MistralAvailability.Operational;
                downSinceUtc = null;
                lastError = null;
                changed = new(status, downSinceUtc, lastCheckedUtc, lastError);
            }
        }

        if (changed != null)
        {
            logger.Information("Mistral status changed to Operational");
            await BroadcastAsync(changed);
        }
    }

    public async Task ReportFailure(string reason)
    {
        MistralStatusSnapshot? changed = null;

        lock (statusLock)
        {
            consecutiveFailures++;
            lastCheckedUtc = DateTime.UtcNow;

            if (consecutiveFailures >= FailureThreshold)
            {
                bool wasOperational = status != MistralAvailability.Down;
                if (wasOperational)
                    downSinceUtc = DateTime.UtcNow;

                status = MistralAvailability.Down;
                lastError = reason;

                if (wasOperational)
                    changed = new(status, downSinceUtc, lastCheckedUtc, lastError);
            }
        }

        if (changed != null)
        {
            logger.Warning("Mistral status changed to Down: {Reason}", reason);
            await BroadcastAsync(changed);
        }
    }

    private async Task BroadcastAsync(MistralStatusSnapshot snapshot)
    {
        try
        {
            await hubContext.Clients.All.SendAsync("MistralStatusChanged", new
            {
                status = snapshot.Status == MistralAvailability.Down ? "down" : "operational",
                downSince = snapshot.DownSinceUtc,
                lastChecked = snapshot.LastCheckedUtc,
                lastError = snapshot.LastError
            });
        }
        catch
        {
            // Best-effort push, a client that misses this still gets correct state from the initial /status/mistral fetch on load
        }
    }

    /// <summary>
    /// HTTP statuses that indicate Mistral itself is struggling (overloaded/erroring), as opposed to
    /// a problem with our request (400/401/404 etc.), which shouldn't flip the tracked status to Down.
    /// </summary>
    public static bool IsOutageStatusCode(int statusCode) => statusCode is 500 or 502 or 503 or 504 or 529;
}