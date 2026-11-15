namespace Protocol.Core.Social;

/// <summary>
/// Stores what one resident has personally observed or heard.
/// The journal does not reveal information to other residents automatically.
/// </summary>
public sealed class MemoryJournal
{
    private readonly List<MemoryEntry> _entries = [];

    public IReadOnlyList<MemoryEntry> Entries => _entries;

    public void Remember(MemoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.Add(entry);
    }

    public void Hear(MemoryEntry original, long receivedAtTick, double reliability = 0.75)
    {
        ArgumentNullException.ThrowIfNull(original);
        _entries.Add(original.Retold(receivedAtTick, reliability));
    }

    /// <summary>
    /// Copies every known observation and rumor without broadcasting it to
    /// anyone else or changing its original confidence and timestamp.
    /// Used when the parasite inherits a victim's memories.
    /// </summary>
    public int CopyAllFrom(MemoryJournal source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (ReferenceEquals(this, source))
            throw new ArgumentException("A journal cannot copy itself.", nameof(source));

        var snapshot = source._entries.ToArray();
        _entries.AddRange(snapshot);
        return snapshot.Length;
    }

    public IReadOnlyList<MemoryEntry> Recent(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        return _entries
            .OrderByDescending(entry => entry.Tick)
            .Take(count)
            .ToArray();
    }

    public bool HasHeard(string eventCode)
    {
        if (string.IsNullOrWhiteSpace(eventCode))
            return false;

        return _entries.Any(entry =>
            string.Equals(entry.EventCode, eventCode, StringComparison.Ordinal));
    }
}
