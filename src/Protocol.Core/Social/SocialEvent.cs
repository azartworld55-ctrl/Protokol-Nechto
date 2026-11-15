namespace Protocol.Core.Social;

public enum SocialEventKind
{
    Met,
    Helped,
    SharedSecret,
    Insulted,
    BrokePromise,
    FalseAccusationDisproved
}

/// <summary>
/// A social event between two distinct residents. The event is applied only
/// when the simulation has established that the target learned about it.
/// </summary>
public sealed record SocialEvent
{
    public SocialEvent(
        Guid actorId,
        Guid targetId,
        long tick,
        SocialEventKind kind)
    {
        if (actorId == Guid.Empty || targetId == Guid.Empty)
            throw new ArgumentException("Social event participants must have valid ids.");

        if (actorId == targetId)
            throw new ArgumentException("A social event requires two different residents.");

        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));

        ActorId = actorId;
        TargetId = targetId;
        Tick = tick;
        Kind = kind;
    }

    public Guid ActorId { get; }
    public Guid TargetId { get; }
    public long Tick { get; }
    public SocialEventKind Kind { get; }
}
