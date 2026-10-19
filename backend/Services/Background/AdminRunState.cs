namespace KnowledgeBank.Services.Background;

/// <summary>
/// Tracks whether an admin-triggered background run is in progress, so its endpoint can refuse to start a second
/// overlapping run and the frontend can grey out its buttons and offer a cancel action. Register each subclass as a
/// singleton, since this state needs to outlive any single request/scope.
/// </summary>
public abstract class AdminRunState
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


/// <summary>
/// Run state for admin re-embed. See <see cref="AdminRunState"/>. 
/// </summary>
public class ReembedRunState : AdminRunState;

/// <summary>
/// Run state for chat evaluation. See <see cref="AdminRunState"/>. 
/// </summary>
public class ChatEvalRunState : AdminRunState;

