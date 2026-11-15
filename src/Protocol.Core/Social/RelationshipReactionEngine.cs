namespace Protocol.Core.Social;

/// <summary>
/// Converts known social events into bounded relationship changes.
/// This class contains game-design tuning values, not RimWorld integration.
/// </summary>
public sealed class RelationshipReactionEngine
{
    public RelationshipState Apply(RelationshipGraph graph, SocialEvent socialEvent)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(socialEvent);

        var (trustDelta, familiarityDelta) = socialEvent.Kind switch
        {
            SocialEventKind.Met => (2, 10),
            SocialEventKind.Helped => (18, 5),
            SocialEventKind.SharedSecret => (12, 3),
            SocialEventKind.Insulted => (-16, 4),
            SocialEventKind.BrokePromise => (-28, 5),
            SocialEventKind.FalseAccusationDisproved => (-22, 6),
            _ => throw new ArgumentOutOfRangeException(
                nameof(socialEvent),
                socialEvent.Kind,
                "Unknown social event kind.")
        };

        graph.AdjustTrust(socialEvent.ActorId, socialEvent.TargetId, trustDelta);
        return graph.AdjustFamiliarity(
            socialEvent.ActorId,
            socialEvent.TargetId,
            familiarityDelta);
    }
}
