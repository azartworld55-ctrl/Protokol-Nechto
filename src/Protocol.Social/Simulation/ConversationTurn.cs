using Protocol.Social.Domain;

namespace Protocol.Social.Simulation;

public enum ConversationTurnStatus
{
    SharedRumor = 0,
    NoTopic = 1,
    Rejected = 2
}

/// <summary>
/// Immutable structured turn. It records a knowledge-transfer attempt, not
/// generated dialogue text or proof that a physical conversation occurred.
/// </summary>
public sealed class ConversationTurn
{
    internal ConversationTurn(
        int number,
        ResidentId speakerId,
        ResidentId listenerId,
        ConversationTurnStatus status,
        Guid? topicEventId,
        RumorShareStatus? rejectionStatus)
    {
        if (number <= 0)
            throw new ArgumentOutOfRangeException(nameof(number));

        ArgumentNullException.ThrowIfNull(speakerId);
        ArgumentNullException.ThrowIfNull(listenerId);

        if (speakerId == listenerId)
            throw new ArgumentException("A conversation turn requires two different residents.");

        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status));

        switch (status)
        {
            case ConversationTurnStatus.SharedRumor
                when topicEventId is null || rejectionStatus is not null:
                throw new ArgumentException("A successful turn requires a topic and cannot contain a rejection.");

            case ConversationTurnStatus.NoTopic
                when topicEventId is not null || rejectionStatus is not null:
                throw new ArgumentException("A turn without a topic cannot contain a topic id or rejection.");

            case ConversationTurnStatus.Rejected
                when topicEventId is null || rejectionStatus is null:
                throw new ArgumentException("A rejected turn requires a topic id and rejection status.");
        }

        Number = number;
        SpeakerId = speakerId;
        ListenerId = listenerId;
        Status = status;
        TopicEventId = topicEventId;
        RejectionStatus = rejectionStatus;
    }

    public int Number { get; }

    public ResidentId SpeakerId { get; }

    public ResidentId ListenerId { get; }

    public ConversationTurnStatus Status { get; }

    /// <summary>The event proposed for this turn, or null when the speaker had no topic.</summary>
    public Guid? TopicEventId { get; }

    /// <summary>The rumor service's rejection reason, when a proposed topic was not shared.</summary>
    public RumorShareStatus? RejectionStatus { get; }

    public bool SharedRumor => Status == ConversationTurnStatus.SharedRumor;
}

/// <summary>An immutable transcript of structured knowledge-transfer turns.</summary>
public sealed class ConversationExchangeResult
{
    internal ConversationExchangeResult(Guid conversationId, IEnumerable<ConversationTurn> turns)
    {
        if (conversationId == Guid.Empty)
            throw new ArgumentException("Conversation id cannot be empty.", nameof(conversationId));

        ArgumentNullException.ThrowIfNull(turns);

        ConversationId = conversationId;
        Turns = Array.AsReadOnly(turns.ToArray());
        SharedRumorCount = Turns.Count(turn => turn.SharedRumor);
    }

    public Guid ConversationId { get; }

    public IReadOnlyList<ConversationTurn> Turns { get; }

    public int SharedRumorCount { get; }
}
