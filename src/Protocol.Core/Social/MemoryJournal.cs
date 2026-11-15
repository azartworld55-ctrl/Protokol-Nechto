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
