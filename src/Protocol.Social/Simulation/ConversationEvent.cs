using Protocol.Social.Domain;

namespace Protocol.Social.Simulation;

/// <summary>
/// An explicit communication event produced by the simulation. Physical
/// proximity or line of sight alone must never construct this event.
/// </summary>
public sealed class ConversationEvent
{
    public ConversationEvent(
        Guid id,
        ResidentId participantA,
        ResidentId participantB,
        long tick)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Conversation id cannot be empty.", nameof(id));

        ArgumentNullException.ThrowIfNull(participantA);
        ArgumentNullException.ThrowIfNull(participantB);

        if (participantA == participantB)
            throw new ArgumentException("A conversation requires two different residents.");

        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));

        Id = id;
        ParticipantA = participantA;
        ParticipantB = participantB;
        Tick = tick;
    }

    public Guid Id { get; }

    public ResidentId ParticipantA { get; }

    public ResidentId ParticipantB { get; }

    public long Tick { get; }

    public bool IncludesPair(ResidentId first, ResidentId second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return (ParticipantA == first && ParticipantB == second) ||
               (ParticipantA == second && ParticipantB == first);
    }
}
