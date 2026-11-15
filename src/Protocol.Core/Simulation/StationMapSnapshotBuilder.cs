namespace Protocol.Core.Simulation;

/// <summary>
/// A pawn position reported by the game adapter. RuntimePawnId is the
/// adapter's stable identifier, not a display name or a transient list index.
/// </summary>
public sealed record ResidentMapObservation(string RuntimePawnId, GridPosition Position);

/// <summary>
/// Converts adapter-owned pawn observations into a core map snapshot.
/// Unknown pawn IDs are rejected rather than silently assigned identities.
/// </summary>
public sealed class StationMapSnapshotBuilder
{
    public StationMapSnapshot Build(
        int width,
        int height,
        IEnumerable<MapCell> cells,
        IEnumerable<ResidentMapObservation> observations,
        ResidentRuntimeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(cells);
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(registry);

        var positions = new Dictionary<Guid, GridPosition>();
        var seenRuntimeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var observation in observations)
        {
            if (observation is null)
                throw new ArgumentException("Map observations cannot contain null entries.", nameof(observations));
            if (string.IsNullOrWhiteSpace(observation.RuntimePawnId))
                throw new ArgumentException("Every map observation requires a runtime pawn id.", nameof(observations));

            var runtimePawnId = observation.RuntimePawnId.Trim();
            if (!seenRuntimeIds.Add(runtimePawnId))
                throw new ArgumentException("A pawn can appear only once in a map snapshot.", nameof(observations));

            if (!registry.TryGetResidentId(runtimePawnId, out var residentId))
                throw new InvalidOperationException(
                    $"Runtime pawn '{runtimePawnId}' has no registered resident identity.");

            positions.Add(residentId, observation.Position);
        }

        return new StationMapSnapshot(width, height, cells, positions);
    }
}
