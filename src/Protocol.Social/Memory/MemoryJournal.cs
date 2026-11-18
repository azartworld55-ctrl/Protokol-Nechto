using Protocol.Social.Domain;

namespace Protocol.Social.Memory;

/// <summary>
/// A capability-scoped journal owned by one resident. There is no global API
/// that enumerates every resident's private memories.
/// </summary>
public sealed class MemoryJournal
{
    private readonly List<MemoryEntry> _entries = new();
    private readonly Dictionary<Guid, MemoryEntry> _byEventId = new();

    public MemoryJournal(ResidentId ownerId)
    {
        ArgumentNullException.ThrowIfNull(ownerId);
        OwnerId = ownerId;
    }

    public ResidentId OwnerId { get; }

    /// <summary>
    /// Returns an immutable snapshot, not the backing List or a live wrapper.
    /// </summary>
    public IReadOnlyList<MemoryEntry> Entries =>
        Array.AsReadOnly(_entries.ToArray());

    public bool TryGet(Guid eventId, out MemoryEntry? entry)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event id cannot be empty.", nameof(eventId));

        return _byEventId.TryGetValue(eventId, out entry);
    }

    /// <summary>
    /// Records an observation only in the observing resident's own journal.
    /// Duplicate event ids are ignored so one event cannot be counted twice.
    /// </summary>
    public bool RecordObservation(DirectObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        if (observation.ObserverId != OwnerId)
        {
            throw new ArgumentException(
                "An observation can only be recorded in its observer's own journal.",
                nameof(observation));
        }

        if (_byEventId.ContainsKey(observation.EventId))
            return false;

        var entry = new MemoryEntry(
            observation.EventId,
            observation.SubjectId,
            observation.ObserverId,
            observation.ObserverId,
            observation.Description,
            observation.Tick,
            observation.Tick,
            MemoryKind.DirectObservation,
            confidencePermille: 1000,
            retellCount: 0,
            sourceConversationId: null);

        Add(entry);
        return true;
    }

    internal bool TryRecordRumor(MemoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Kind != MemoryKind.HeardRumor)
            throw new ArgumentException("Only rumor entries can be received through this path.", nameof(entry));

        if (entry.LastInformantId == OwnerId)
            throw new ArgumentException("A resident cannot be their own rumor informant.", nameof(entry));

        if (_byEventId.ContainsKey(entry.EventId))
            return false;

        Add(entry);
        return true;
    }

    private void Add(MemoryEntry entry)
    {
        _entries.Add(entry);
        _byEventId.Add(entry.EventId, entry);
    }
}
