namespace Protocol.Core.Social;

public readonly record struct RelationshipState(int Trust, int Familiarity);

/// <summary>
/// Undirected social links between residents. Trust and familiarity are
/// independent: people can know each other well without trusting each other.
/// </summary>
public sealed class RelationshipGraph
{
    private readonly Dictionary<(Guid First, Guid Second), RelationshipState> _links = [];

    public RelationshipState Get(Guid first, Guid second)
    {
        ValidatePair(first, second);
        return _links.TryGetValue(Normalize(first, second), out var state)
            ? state
            : new RelationshipState(0, 0);
    }

    public RelationshipState AdjustTrust(Guid first, Guid second, int delta)
    {
        var current = Get(first, second);
        var updated = current with { Trust = Math.Clamp(current.Trust + delta, -100, 100) };
        _links[Normalize(first, second)] = updated;
        return updated;
    }

    public RelationshipState AdjustFamiliarity(Guid first, Guid second, int delta)
    {
        var current = Get(first, second);
        var updated = current with
        {
            Familiarity = Math.Clamp(current.Familiarity + delta, 0, 100)
        };
        _links[Normalize(first, second)] = updated;
        return updated;
    }

    private static (Guid First, Guid Second) Normalize(Guid first, Guid second) =>
        first.CompareTo(second) <= 0 ? (first, second) : (second, first);

    private static void ValidatePair(Guid first, Guid second)
    {
        if (first == Guid.Empty || second == Guid.Empty)
            throw new ArgumentException("Relationship endpoints must have valid ids.");

        if (first == second)
            throw new ArgumentException("A resident cannot have a relationship link to themselves.");
    }
}
