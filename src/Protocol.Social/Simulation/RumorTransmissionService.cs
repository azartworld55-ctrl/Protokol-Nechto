using Protocol.Social.Domain;
using Protocol.Social.Memory;

namespace Protocol.Social.Simulation;

public enum RumorShareStatus
{
    Shared = 0,
    InvalidConversationParticipants = 1,
    SenderDoesNotKnowEvent = 2,
    RecipientAlreadyKnowsEvent = 3,
    ConversationBeforeKnowledge = 4,
    RelationshipBelowThreshold = 5,
    RetellLimitReached = 6,
    ConfidenceBelowThreshold = 7
}

public sealed record RumorShareResult(RumorShareStatus Status, MemoryEntry? Entry)
{
    public bool WasShared => Status == RumorShareStatus.Shared;
}

/// <summary>
/// Transfers one known event only during an explicit conversation. The caller
/// must supply the two participant-owned journals; this service has no global
/// knowledge store and cannot discover unrelated residents' memories.
/// </summary>
public sealed class RumorTransmissionService
{
    public RumorShareResult TryRetell(
        ConversationEvent conversation,
        ResidentId senderId,
        ResidentId recipientId,
        Guid eventId,
        MemoryJournal senderMemory,
        MemoryJournal recipientMemory,
        RelationshipLedger relationships,
        RumorTransmissionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(senderId);
        ArgumentNullException.ThrowIfNull(recipientId);
        ArgumentNullException.ThrowIfNull(senderMemory);
        ArgumentNullException.ThrowIfNull(recipientMemory);
        ArgumentNullException.ThrowIfNull(relationships);
        ArgumentNullException.ThrowIfNull(policy);

        if (senderId == recipientId ||
            !conversation.IncludesPair(senderId, recipientId) ||
            senderMemory.OwnerId != senderId ||
            recipientMemory.OwnerId != recipientId)
        {
            return new RumorShareResult(RumorShareStatus.InvalidConversationParticipants, null);
        }

        if (!senderMemory.TryGet(eventId, out var source) || source is null)
            return new RumorShareResult(RumorShareStatus.SenderDoesNotKnowEvent, null);

        if (recipientMemory.TryGet(eventId, out _))
            return new RumorShareResult(RumorShareStatus.RecipientAlreadyKnowsEvent, null);

        if (conversation.Tick < source.LearnedAtTick)
            return new RumorShareResult(RumorShareStatus.ConversationBeforeKnowledge, null);

        var relationship = relationships.Get(senderId, recipientId);
        if (relationship.Trust < policy.MinimumTrust ||
            relationship.Familiarity < policy.MinimumFamiliarity)
        {
            return new RumorShareResult(RumorShareStatus.RelationshipBelowThreshold, null);
        }

        if (source.RetellCount >= policy.MaximumRetellCount)
            return new RumorShareResult(RumorShareStatus.RetellLimitReached, null);

        var confidence = (int)(
            (long)source.ConfidencePermille * policy.ConfidenceDecayPermille / 1000L);

        if (confidence < policy.MinimumConfidencePermille)
            return new RumorShareResult(RumorShareStatus.ConfidenceBelowThreshold, null);

        var rumor = new MemoryEntry(
            source.EventId,
            source.SubjectId,
            source.OriginalObserverId,
            senderId,
            source.Description,
            source.EventTick,
            conversation.Tick,
            MemoryKind.HeardRumor,
            confidence,
            source.RetellCount + 1,
            conversation.Id);

        if (!recipientMemory.TryRecordRumor(rumor))
            return new RumorShareResult(RumorShareStatus.RecipientAlreadyKnowsEvent, null);

        return new RumorShareResult(RumorShareStatus.Shared, rumor);
    }
}
