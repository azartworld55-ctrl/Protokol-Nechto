namespace Protocol.Social.Domain;

/// <summary>
/// Stores directed relationship opinions. A's trust in B is not assumed to
/// equal B's trust in A. The ledger deliberately exposes no mutable collection.
/// </summary>
public sealed class RelationshipLedger
{
    private static readonly RelationshipState UnknownRelationship = new(0, 0);

    private readonly Dictionary<(ResidentId Observer, ResidentId Target), RelationshipState> _states = new();

    public RelationshipState Get(ResidentId observerId, ResidentId targetId)
    {
        ValidatePair(observerId, targetId);

        return _states.TryGetValue((observerId, targetId), out var state)
            ? state
            : UnknownRelationship;
    }

    /// <summary>
    /// Applies signed deltas without overflowing, then clamps each axis to 0..100.
    /// </summary>
    public RelationshipState Adjust(
        ResidentId observerId,
        ResidentId targetId,
        long trustDelta,
        long familiarityDelta)
    {
        ValidatePair(observerId, targetId);

        var current = Get(observerId, targetId);
        var updated = new RelationshipState(
            ClampDelta(current.Trust, trustDelta),
            ClampDelta(current.Familiarity, familiarityDelta));

        _states[(observerId, targetId)] = updated;
        return updated;
    }

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

    private static void ValidatePair(ResidentId observerId, ResidentId targetId)
    {
        ArgumentNullException.ThrowIfNull(observerId);
        ArgumentNullException.ThrowIfNull(targetId);

        if (observerId == targetId)
            throw new ArgumentException("A resident cannot have a relationship with themself.");
    }
}
