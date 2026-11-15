namespace Protocol.Core.Simulation;

/// <summary>
/// Stable association between a game-side pawn identifier and a core resident.
/// Runtime identifiers are opaque strings: the core does not assume a Verse API.
/// </summary>
public sealed record ResidentRuntimeBinding(string RuntimePawnId, Guid ResidentId);

/// <summary>
/// Versioned, serializable data contract for saving identity associations.
/// The game adapter is responsible for storing this snapshot in its save system.
/// </summary>
public sealed record ResidentRuntimeRegistrySnapshot(
    int SchemaVersion,
    IReadOnlyList<ResidentRuntimeBinding> Bindings);

/// <summary>
/// Maintains a one-to-one mapping between external pawn IDs and core resident IDs.
/// Bindings survive reloads only when the adapter saves and restores the snapshot.
/// </summary>
public sealed class ResidentRuntimeRegistry
{
    public const int CurrentSchemaVersion = 1;

    private readonly Dictionary<string, Guid> _residentByRuntimeId =
        new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, string> _runtimeIdByResident =
        new();

    public ResidentRuntimeRegistry()
    {
    }

    public ResidentRuntimeRegistry(ResidentRuntimeRegistrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.SchemaVersion != CurrentSchemaVersion)
            throw new ArgumentException(
                $"Unsupported runtime registry schema version: {snapshot.SchemaVersion}.",
                nameof(snapshot));
        ArgumentNullException.ThrowIfNull(snapshot.Bindings);

        foreach (var binding in snapshot.Bindings)
        {
            if (!Bind(binding.RuntimePawnId, binding.ResidentId))
                throw new ArgumentException(
                    "Snapshot contains a duplicate or conflicting runtime binding.",
                    nameof(snapshot));
        }
    }

    public int Count => _residentByRuntimeId.Count;

    /// <summary>
    /// Adds a binding. Repeating the exact same binding is harmless; either
    /// side already mapped to a different identity is rejected.
    /// </summary>
    public bool Bind(string runtimePawnId, Guid residentId)
    {
        if (string.IsNullOrWhiteSpace(runtimePawnId))
            throw new ArgumentException("Runtime pawn id is required.", nameof(runtimePawnId));
        if (residentId == Guid.Empty)
            throw new ArgumentException("Resident id cannot be empty.", nameof(residentId));

        var normalizedRuntimeId = runtimePawnId.Trim();
        if (_residentByRuntimeId.TryGetValue(normalizedRuntimeId, out var existingResident))
            return existingResident == residentId;

        if (_runtimeIdByResident.ContainsKey(residentId))
            return false;

        _residentByRuntimeId.Add(normalizedRuntimeId, residentId);
        _runtimeIdByResident.Add(residentId, normalizedRuntimeId);
        return true;
    }

    public bool TryGetResidentId(string runtimePawnId, out Guid residentId)
    {
        if (string.IsNullOrWhiteSpace(runtimePawnId))
        {
            residentId = Guid.Empty;
            return false;
        }

        return _residentByRuntimeId.TryGetValue(runtimePawnId.Trim(), out residentId);
    }

    public bool TryGetRuntimePawnId(Guid residentId, out string runtimePawnId)
    {
        if (residentId == Guid.Empty)
        {
            runtimePawnId = string.Empty;
            return false;
        }

        return _runtimeIdByResident.TryGetValue(residentId, out runtimePawnId!);
    }

    public bool UnbindRuntimePawn(string runtimePawnId)
    {
        if (string.IsNullOrWhiteSpace(runtimePawnId))
            return false;

        var normalizedRuntimeId = runtimePawnId.Trim();
        if (!_residentByRuntimeId.Remove(normalizedRuntimeId, out var residentId))
            return false;

        _runtimeIdByResident.Remove(residentId);
        return true;
    }

    public ResidentRuntimeRegistrySnapshot CreateSnapshot() =>
        new(
            CurrentSchemaVersion,
            _residentByRuntimeId
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new ResidentRuntimeBinding(pair.Key, pair.Value))
                .ToArray());
}
