using Protocol.Core.Social;

namespace Protocol.Core.Simulation;

/// <summary>
/// Result of translating one game-side incident into resident knowledge.
/// ObserverIds contains only participants/witnesses established from the
/// supplied map snapshot; it is not a station-wide notification list.
/// </summary>
public sealed record RuntimeIncidentRecordResult(
    ResidentIncident Incident,
    IReadOnlySet<Guid> ObserverIds,
    int RecordedJournalCount);

/// <summary>
/// Bridges a game-side incident into personal resident journals. The adapter
/// supplies the event location and a map snapshot from the same logical tick;
/// visibility is calculated here and no hidden residents are informed.
/// </summary>
public sealed class RuntimeIncidentBridge
{
    private readonly StationPerceptionService _perception;
    private readonly IncidentMemoryService _memory;

    public RuntimeIncidentBridge(
        StationPerceptionService? perception = null,
        IncidentMemoryService? memory = null)
    {
        _perception = perception ?? new StationPerceptionService();
        _memory = memory ?? new IncidentMemoryService();
    }

    public RuntimeIncidentRecordResult RecordAt(
        string actorRuntimePawnId,
        string? affectedRuntimePawnId,
        string eventCode,
        long tick,
        ResidentIncidentKind kind,
        GridPosition eventPosition,
        int sightRadius,
        StationMapSnapshot map,
        ResidentRuntimeRegistry registry,
        IReadOnlyDictionary<Guid, MemoryJournal> journals)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(journals);
        if (string.IsNullOrWhiteSpace(actorRuntimePawnId))
            throw new ArgumentException("Actor runtime pawn id is required.", nameof(actorRuntimePawnId));
        if (sightRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(sightRadius));
        if (!map.Contains(eventPosition))
            throw new ArgumentOutOfRangeException(nameof(eventPosition));

        if (!registry.TryGetResidentId(actorRuntimePawnId, out var actorId))
            throw new InvalidOperationException("The incident actor has no registered resident identity.");
        if (!map.ResidentPositions.ContainsKey(actorId))
            throw new InvalidOperationException("The incident actor is absent from the supplied map snapshot.");

        Guid? affectedId = null;
        if (!string.IsNullOrWhiteSpace(affectedRuntimePawnId))
        {
            if (!registry.TryGetResidentId(affectedRuntimePawnId, out var resolvedAffectedId))
                throw new InvalidOperationException("The affected pawn has no registered resident identity.");
            if (!map.ResidentPositions.ContainsKey(resolvedAffectedId))
                throw new InvalidOperationException("The affected resident is absent from the supplied map snapshot.");
            affectedId = resolvedAffectedId;
        }

        var incident = new ResidentIncident(
            actorId,
            affectedId,
            eventCode,
            tick,
            kind);

        var observers = _perception.GetResidentsWhoCanSee(
            map,
            eventPosition,
            sightRadius).ToHashSet();

        // A person directly affected by an incident knows it happened to them,
        // even if an obstruction blocks their view of the event tile.
        if (affectedId.HasValue)
            observers.Add(affectedId.Value);

        var recorded = _memory.RecordForObservers(incident, observers, journals);
        return new RuntimeIncidentRecordResult(incident, observers, recorded);
    }
}
