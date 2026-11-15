namespace Protocol.Core.Simulation;

public enum ResidentCustodyStatus
{
    Free,
    Detained,
    Released,
    Escaped
}

public enum CustodyTransitionKind
{
    Detained,
    Released,
    EscapeAttemptFailed,
    Escaped
}

public sealed record CustodyTransition(
    Guid ResidentId,
    CustodyTransitionKind Kind,
    long Tick,
    string Reason);

public sealed record ResidentCustodySnapshot(
    Guid ResidentId,
    ResidentCustodyStatus Status,
    long? DetainedAtTick,
    string? DetentionReason,
    IReadOnlyList<CustodyTransition> History);

/// <summary>
/// Tracks the social/legal state of residents held by station security.
/// The game adapter remains responsible for actual cells, guards, pathing,
/// UI, and the conditions that determine whether an escape attempt succeeds.
/// </summary>
public sealed class ResidentCustodyRegistry
{
    private sealed class Entry
    {
        public ResidentCustodyStatus Status { get; set; } = ResidentCustodyStatus.Free;
        public long? DetainedAtTick { get; set; }
        public string? DetentionReason { get; set; }
        public List<CustodyTransition> History { get; } = [];
    }

    private readonly HashSet<Guid> _knownResidents;
    private readonly Dictionary<Guid, Entry> _entries = [];

    public ResidentCustodyRegistry(IEnumerable<Guid> residentIds)
    {
        ArgumentNullException.ThrowIfNull(residentIds);
        _knownResidents = residentIds.ToHashSet();
        if (_knownResidents.Count == 0 || _knownResidents.Contains(Guid.Empty))
            throw new ArgumentException("At least one valid resident id is required.", nameof(residentIds));
    }

    public bool IsDetained(Guid residentId) =>
        TryGetEntry(residentId, out var entry) &&
        entry.Status == ResidentCustodyStatus.Detained;

    public ResidentCustodyStatus GetStatus(Guid residentId)
    {
        ValidateResident(residentId);
        return _entries.TryGetValue(residentId, out var entry)
            ? entry.Status
            : ResidentCustodyStatus.Free;
    }

    public bool Detain(Guid residentId, string reason, long tick)
    {
        ValidateTick(tick);
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A detention reason is required.", nameof(reason));

        ValidateResident(residentId);
        var entry = GetOrCreate(residentId);
        if (entry.Status is ResidentCustodyStatus.Detained or ResidentCustodyStatus.Escaped)
            return false;

        entry.Status = ResidentCustodyStatus.Detained;
        entry.DetainedAtTick = tick;
        entry.DetentionReason = reason.Trim();
        entry.History.Add(new CustodyTransition(
            residentId, CustodyTransitionKind.Detained, tick, entry.DetentionReason));
        return true;
    }

    public bool Release(Guid residentId, string reason, long tick)
    {
        ValidateTick(tick);
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A release reason is required.", nameof(reason));

        ValidateResident(residentId);
        var entry = GetOrCreate(residentId);
        if (entry.Status != ResidentCustodyStatus.Detained)
            return false;

        entry.Status = ResidentCustodyStatus.Released;
        entry.History.Add(new CustodyTransition(
            residentId, CustodyTransitionKind.Released, tick, reason.Trim()));
        return true;
    }

    /// <summary>
    /// The caller supplies the outcome of the simulated attempt. This core
    /// records consequences but does not simulate guards or physical tactics.
    /// </summary>
    public bool ResolveEscapeAttempt(Guid residentId, bool succeeded, long tick)
    {
        ValidateTick(tick);
        ValidateResident(residentId);
        var entry = GetOrCreate(residentId);
        if (entry.Status != ResidentCustodyStatus.Detained)
            return false;

        if (succeeded)
        {
            entry.Status = ResidentCustodyStatus.Escaped;
            entry.History.Add(new CustodyTransition(
                residentId, CustodyTransitionKind.Escaped, tick, "escape.succeeded"));
        }
        else
        {
            entry.History.Add(new CustodyTransition(
                residentId, CustodyTransitionKind.EscapeAttemptFailed, tick, "escape.failed"));
        }

        return true;
    }

    public ResidentCustodySnapshot GetSnapshot(Guid residentId)
    {
        ValidateResident(residentId);
        if (!_entries.TryGetValue(residentId, out var entry))
            return new ResidentCustodySnapshot(
                residentId, ResidentCustodyStatus.Free, null, null, Array.Empty<CustodyTransition>());

        return new ResidentCustodySnapshot(
            residentId,
            entry.Status,
            entry.DetainedAtTick,
            entry.DetentionReason,
            Array.AsReadOnly(entry.History.ToArray()));
    }

    private bool TryGetEntry(Guid residentId, out Entry entry)
    {
        ValidateResident(residentId);
        return _entries.TryGetValue(residentId, out entry!);
    }

    private Entry GetOrCreate(Guid residentId)
    {
        if (!_entries.TryGetValue(residentId, out var entry))
        {
            entry = new Entry();
            _entries.Add(residentId, entry);
        }

        return entry;
    }

    private void ValidateResident(Guid residentId)
    {
        if (!_knownResidents.Contains(residentId))
            throw new ArgumentException("Resident does not belong to this station.", nameof(residentId));
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));
    }
}
