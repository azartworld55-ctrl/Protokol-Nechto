namespace Protocol.Social.Domain;

/// <summary>
/// Stores one shared relationship state per unordered resident pair.
/// Trust and familiarity are symmetric: A↔B is the same state from either side.
/// </summary>
public sealed class RelationshipLedger
{
    private static readonly RelationshipState UnknownRelationship = new(0, 0);

    private readonly Dictionary<(Guid First, Guid Second), RelationshipState> _states = new();

    public RelationshipState Get(ResidentId observerId, ResidentId targetId)
    {
        ValidatePair(observerId, targetId);
        var key = GetPairKey(observerId, targetId);

        return _states.TryGetValue(key, out var state)
            ? state
            : UnknownRelationship;
    }

    /// <summary>
    /// Applies signed deltas to the shared pair state without overflowing,
    /// then clamps each axis to 0..100.
    /// </summary>
    public RelationshipState Adjust(
        ResidentId observerId,
        ResidentId targetId,
        long trustDelta,
        long familiarityDelta)
    {
        ValidatePair(observerId, targetId);

        var key = GetPairKey(observerId, targetId);
        var current = _states.TryGetValue(key, out var state)
            ? state
            : UnknownRelationship;

        var updated = new RelationshipState(
            ClampDelta(current.Trust, trustDelta),
            ClampDelta(current.Familiarity, familiarityDelta));

        _states[key] = updated;
        return updated;
    }

    private static (Guid First, Guid Second) GetPairKey(ResidentId first, ResidentId second) =>
        first.Value.CompareTo(second.Value) <= 0
            ? (first.Value, second.Value)
            : (second.Value, first.Value);

    private static int ClampDelta(int current, long delta)
    {
        // Compare against the small permitted interval before adding. This also
        // handles long.MinValue and long.MaxValue without overflowing Int64.
        if (delta > 100L - current)
            return 100;

        if (delta < -current)
            return 0;

        return (int)(current + delta);
    }

    private static void ValidatePair(ResidentId first, ResidentId second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (first == second)
            throw new ArgumentException("A resident cannot have a relationship with themself.");
    }
}
