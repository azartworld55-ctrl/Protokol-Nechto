using Protocol.Social.Domain;
using Protocol.Social.Memory;

namespace Protocol.Social.Simulation;

/// <summary>
/// Executes a bounded, alternating exchange of caller-selected memory topics
/// during an already-confirmed conversation. It never creates conversations,
/// selects topics from a global store, or generates natural-language dialogue.
/// </summary>
public sealed class ConversationExchangeService
{
    private readonly RumorTransmissionService _rumorTransmission = new();

    public ConversationExchangeResult Exchange(
        ConversationEvent conversation,
        MemoryJournal participantAMemory,
        MemoryJournal participantBMemory,
        RelationshipLedger relationships,
        RumorTransmissionPolicy rumorPolicy,
        ConversationExchangePolicy exchangePolicy,
        IReadOnlyList<Guid> participantATopics,
        IReadOnlyList<Guid> participantBTopics)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(participantAMemory);
        ArgumentNullException.ThrowIfNull(participantBMemory);
        ArgumentNullException.ThrowIfNull(relationships);
        ArgumentNullException.ThrowIfNull(rumorPolicy);
        ArgumentNullException.ThrowIfNull(exchangePolicy);

        if (participantAMemory.OwnerId != conversation.ParticipantA)
        {
            throw new ArgumentException(
                "Participant A's journal must belong to ConversationEvent.ParticipantA.",
                nameof(participantAMemory));
        }

        if (participantBMemory.OwnerId != conversation.ParticipantB)
        {
            throw new ArgumentException(
                "Participant B's journal must belong to ConversationEvent.ParticipantB.",
                nameof(participantBMemory));
        }

        var topicsA = CopyAndValidateTopics(
            participantATopics,
            nameof(participantATopics),
            exchangePolicy.MaximumTurns);
        var topicsB = CopyAndValidateTopics(
            participantBTopics,
            nameof(participantBTopics),
            exchangePolicy.MaximumTurns);

        var turns = new List<ConversationTurn>(exchangePolicy.MaximumTurns);
        var nextTopicA = 0;
        var nextTopicB = 0;

        for (var turnNumber = 1; turnNumber <= exchangePolicy.MaximumTurns; turnNumber++)
        {
            var participantASpeaks = turnNumber % 2 == 1;
            var speakerId = participantASpeaks
                ? conversation.ParticipantA
                : conversation.ParticipantB;
            var listenerId = participantASpeaks
                ? conversation.ParticipantB
                : conversation.ParticipantA;
            var senderMemory = participantASpeaks ? participantAMemory : participantBMemory;
            var recipientMemory = participantASpeaks ? participantBMemory : participantAMemory;
            var topics = participantASpeaks ? topicsA : topicsB;
            var topicIndex = participantASpeaks ? nextTopicA++ : nextTopicB++;

            if (topicIndex >= topics.Length)
            {
                turns.Add(new ConversationTurn(
                    turnNumber,
                    speakerId,
                    listenerId,
                    ConversationTurnStatus.NoTopic,
                    topicEventId: null,
                    rejectionStatus: null));
                continue;
            }

            var topicEventId = topics[topicIndex];
            var share = _rumorTransmission.TryRetell(
                conversation,
                speakerId,
                listenerId,
                topicEventId,
                senderMemory,
                recipientMemory,
                relationships,
                rumorPolicy);

            turns.Add(share.WasShared
                ? new ConversationTurn(
                    turnNumber,
                    speakerId,
                    listenerId,
                    ConversationTurnStatus.SharedRumor,
                    topicEventId,
                    rejectionStatus: null)
                : new ConversationTurn(
                    turnNumber,
                    speakerId,
                    listenerId,
                    ConversationTurnStatus.Rejected,
                    topicEventId,
                    share.Status));
        }

        return new ConversationExchangeResult(conversation.Id, turns);
    }

    private static Guid[] CopyAndValidateTopics(
        IReadOnlyList<Guid> topics,
        string parameterName,
        int maximumTopics)
    {
        ArgumentNullException.ThrowIfNull(topics);

        if (topics.Count > maximumTopics)
        {
            throw new ArgumentException(
                "A participant cannot supply more topics than the conversation turn limit.",
                parameterName);
        }

        var copy = new Guid[topics.Count];
        var seen = new HashSet<Guid>();

        for (var index = 0; index < topics.Count; index++)
        {
            var eventId = topics[index];
            if (eventId == Guid.Empty)
                throw new ArgumentException("Topic event ids cannot be empty.", parameterName);

            if (!seen.Add(eventId))
                throw new ArgumentException("A topic event id cannot be repeated in one agenda.", parameterName);

            copy[index] = eventId;
        }

        return copy;
    }
}
