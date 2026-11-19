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
    private readonly Dictionary<Guid, int> _entryIndexByEventId = new();
    private IReadOnlyList<MemoryEntry>? _entriesSnapshot;

    public MemoryJournal(ResidentId ownerId)
    {
        ArgumentNullException.ThrowIfNull(ownerId);
        OwnerId = ownerId;
    }

    public ResidentId OwnerId { get; }

    /// <summary>
    /// Returns a cached immutable snapshot. A write invalidates the cache but
    /// never changes a snapshot already handed to a caller.
    /// </summary>
    public IReadOnlyList<MemoryEntry> Entries =>
        _entriesSnapshot ??= Array.AsReadOnly(_entries.ToArray());

    public bool TryGet(Guid eventId, out MemoryEntry? entry)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event id cannot be empty.", nameof(eventId));

        return _byEventId.TryGetValue(eventId, out entry);
    }

    /// <summary>
    /// Records a direct observation only in the observing resident's own journal.
    /// If that resident previously heard the same event as a rumor, direct
    /// observation upgrades the entry without changing older snapshots.
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

        if (_byEventId.TryGetValue(observation.EventId, out var existing))
        {
            if (existing.SubjectId != observation.SubjectId ||
                existing.EventTick != observation.EventTick)
            {
                throw new ArgumentException(
                    "An event id cannot be reused for a different event.",
                    nameof(observation));
            }

            if (existing.Kind == MemoryKind.DirectObservation)
                return false;

            var confirmedEntry = CreateDirectObservationEntry(observation);
            Replace(confirmedEntry);
            return true;
        }

        Add(CreateDirectObservationEntry(observation));
        return true;
    }

    /// <summary>
    /// Receives a rumor only through the explicit conversation service.
    /// Duplicate event ids are ignored.
    /// </summary>
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

    private MemoryEntry CreateDirectObservationEntry(DirectObservation observation) =>
        new(
            observation.EventId,
            observation.SubjectId,
            observation.ObserverId,
            observation.ObserverId,
            observation.Description,
            observation.EventTick,
            observation.ObservedAtTick,
            MemoryKind.DirectObservation,
            confidencePermille: 1000,
            retellCount: 0,
            sourceConversationId: null);

    private void Add(MemoryEntry entry)
    {
        var index = _entries.Count;
        _byEventId.Add(entry.EventId, entry);
        _entryIndexByEventId.Add(entry.EventId, index);
        _entries.Add(entry);
        _entriesSnapshot = null;
    }

    private void Replace(MemoryEntry entry)
    {
        var index = _entryIndexByEventId[entry.EventId];
        _entries[index] = entry;
        _byEventId[entry.EventId] = entry;
        _entriesSnapshot = null;
    }
}
