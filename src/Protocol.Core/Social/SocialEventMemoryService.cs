namespace Protocol.Core.Social;

/// <summary>
/// Writes an event only into the journals of residents who participated in
/// or witnessed it. A witness forms an opinion about the event's actor.
/// </summary>
public sealed class SocialEventMemoryService
{
    private readonly RelationshipReactionEngine _reactionEngine;

    public SocialEventMemoryService(RelationshipReactionEngine? reactionEngine = null)
    {
        _reactionEngine = reactionEngine ?? new RelationshipReactionEngine();
    }

    public bool RecordObservation(
        SocialEvent socialEvent,
        Guid observerId,
        MemoryJournal journal,
        RelationshipGraph relationships)
    {
        ArgumentNullException.ThrowIfNull(socialEvent);
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(relationships);

        if (observerId == Guid.Empty)
            throw new ArgumentException("Observer id cannot be empty.", nameof(observerId));

        if (observerId != socialEvent.ActorId && observerId != socialEvent.TargetId)
            return false;

        var eventCode = BuildEventCode(socialEvent);
        if (journal.HasHeard(eventCode))
            return false;

        journal.Remember(new MemoryEntry(
            socialEvent.ActorId,
            eventCode,
            socialEvent.Tick,
            1.0,
            MemoryOrigin.Observation));

        if (observerId != socialEvent.ActorId)
        {
            var perceivedEvent = new SocialEvent(
                socialEvent.ActorId,
                observerId,
                socialEvent.Tick,
                socialEvent.Kind);
            _reactionEngine.Apply(relationships, perceivedEvent);
        }

        return true;
    }

    public bool TryShareRumor(
        SocialEvent socialEvent,
        Guid recipientId,
        MemoryJournal source,
        MemoryJournal recipient,
        long receivedAtTick,
        RelationshipState relationship,
        bool hasFaceToFaceContact,
        Random random,
        RelationshipGraph relationships)
    {
        ArgumentNullException.ThrowIfNull(socialEvent);
        ArgumentNullException.ThrowIfNull(relationships);

        if (recipientId == Guid.Empty)
            throw new ArgumentException("Recipient id cannot be empty.", nameof(recipientId));

        var transmitted = new RumorTransmissionService().TryShare(
            source,
            recipient,
            BuildEventCode(socialEvent),
            receivedAtTick,
            relationship,
            hasFaceToFaceContact,
            random);

        if (transmitted && recipientId != socialEvent.ActorId)
        {
            var perceivedEvent = new SocialEvent(
                socialEvent.ActorId,
                recipientId,
                receivedAtTick,
                socialEvent.Kind);
            _reactionEngine.Apply(relationships, perceivedEvent);
        }

        return transmitted;
    }

    public static string BuildEventCode(SocialEvent socialEvent)
    {
        ArgumentNullException.ThrowIfNull(socialEvent);
        return $"social.{socialEvent.Kind}.{socialEvent.Tick}";
    }
}
