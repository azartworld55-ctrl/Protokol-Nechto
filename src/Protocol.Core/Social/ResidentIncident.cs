namespace Protocol.Core.Social;

public enum ResidentIncidentKind
{
    Theft,
    Sabotage,
    Assault,
    UnauthorizedAccess,
    Emergency
}

/// <summary>
/// A discrete station incident. The incident itself is not globally known;
/// only residents explicitly supplied as observers learn about it.
/// </summary>
public sealed record ResidentIncident
{
    public ResidentIncident(
        Guid actorId,
        Guid? affectedResidentId,
        string eventCode,
        long tick,
        ResidentIncidentKind kind)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor id cannot be empty.", nameof(actorId));
        if (affectedResidentId == Guid.Empty)
            throw new ArgumentException("Affected resident id must be null or valid.", nameof(affectedResidentId));
        if (affectedResidentId == actorId)
            throw new ArgumentException("An incident actor cannot be their own affected resident.", nameof(affectedResidentId));
        if (string.IsNullOrWhiteSpace(eventCode))
            throw new ArgumentException("Event code is required.", nameof(eventCode));
        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));

        ActorId = actorId;
        AffectedResidentId = affectedResidentId;
        EventCode = eventCode.Trim();
        Tick = tick;
        Kind = kind;
    }

    public Guid ActorId { get; }
    public Guid? AffectedResidentId { get; }
    public string EventCode { get; }
    public long Tick { get; }
    public ResidentIncidentKind Kind { get; }
}
