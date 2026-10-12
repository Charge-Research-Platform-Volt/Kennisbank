using System.Collections.Concurrent;

namespace KnowledgeBank.Services.Background;

public class PresenceTracker
{
    private class Entry { public int Connections; public DateTime LastActivityUtc; }
    private readonly ConcurrentDictionary<string, Entry> users = new();
    private int activeChatStreams;

    public void Connected(string userId) =>
        users.AddOrUpdate(userId,
            _ => new Entry { Connections = 1, LastActivityUtc = DateTime.UtcNow },
            (_, e) => { Interlocked.Increment(ref e.Connections); return e; });

    public void Disconnected(string userId)
    {
        // Entries are never removed: removing at zero races with a reconnect (page reload) incrementing the same entry
        if (users.TryGetValue(userId, out Entry? e))
            Interlocked.Decrement(ref e.Connections);
    }

    public void Touch(string userId)
    {
        if (users.TryGetValue(userId, out Entry? e)) e.LastActivityUtc = DateTime.UtcNow;
    }

    public void ChatStreamStarted() => Interlocked.Increment(ref activeChatStreams);
    public void ChatStreamEnded() => Interlocked.Decrement(ref activeChatStreams);

    // Only counts leave this class, never user ids
    public (int Connected, int Active, int ChatStreams) Snapshot(TimeSpan activeWindow, string? excludeUserId = null)
    {
        DateTime cutoff = DateTime.UtcNow - activeWindow;
        List<Entry> online = users.Where(kv => kv.Key != excludeUserId && kv.Value.Connections > 0).Select(kv => kv.Value).ToList();
        return (online.Count, online.Count(e => e.LastActivityUtc >= cutoff), activeChatStreams);
    }
}