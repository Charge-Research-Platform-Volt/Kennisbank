namespace KnowledgeBank.Services.Background;

/// <summary>
/// Tracks whether an admin-triggered re-embed run is currently in progress, so the endpoint can
/// refuse to start a second overlapping run and the frontend can grey out its buttons and offer
/// a cancel action. Singleton by design — this state needs to outlive any single request/scope.
/// </summary>
public class ReembedRunState
{
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;

    public bool IsRunning
    {
        get { lock (_lock) return _cts != null; }
    }

    public bool TryStart(out CancellationToken token)
    {
        lock (_lock)
        {
            if (_cts != null)
            {
                token = default;
                return false;
            }

            _cts = new CancellationTokenSource();
            token = _cts.Token;
            return true;
        }
    }

    public void Cancel()
    {
        lock (_lock) _cts?.Cancel();
    }

    public void Complete()
    {
        lock (_lock)
        {
            _cts?.Dispose();
            _cts = null;
        }
    }
}
