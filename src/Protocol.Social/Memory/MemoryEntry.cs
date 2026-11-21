using Protocol.Social.Domain;

namespace Protocol.Social.Memory;

/// <summary>
/// Immutable knowledge held by one resident. Rumors retain their original
/// observer and record the most recent informant separately.
/// </summary>
public sealed class MemoryEntry
{
    internal MemoryEntry(
        Guid eventId,
        ResidentId subjectId,
        ResidentId originalObserverId,
        ResidentId lastInformantId,
        string description,
        long eventTick,
        long learnedAtTick,
        MemoryKind kind,
        int confidencePermille,
        int retellCount,
        Guid? sourceConversationId,
        int recallStrengthPermille = 1000,
        int importancePermille = 500,
        long? lastEvaluatedTick = null)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event id cannot be empty.", nameof(eventId));

        ArgumentNullException.ThrowIfNull(subjectId);
        ArgumentNullException.ThrowIfNull(originalObserverId);
        ArgumentNullException.ThrowIfNull(lastInformantId);

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Memory description is required.", nameof(description));

        if (eventTick < 0)
            throw new ArgumentOutOfRangeException(nameof(eventTick));

        if (learnedAtTick < eventTick)
            throw new ArgumentOutOfRangeException(nameof(learnedAtTick));

        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));

        if (confidencePermille is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(confidencePermille));

        if (retellCount < 0)
            throw new ArgumentOutOfRangeException(nameof(retellCount));

        if (sourceConversationId == Guid.Empty)
            throw new ArgumentException("Source conversation id cannot be empty.", nameof(sourceConversationId));

        if (recallStrengthPermille is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(recallStrengthPermille));

        if (importancePermille is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(importancePermille));

        var evaluatedAt = lastEvaluatedTick ?? learnedAtTick;
        if (evaluatedAt < learnedAtTick)
            throw new ArgumentOutOfRangeException(nameof(lastEvaluatedTick));

        if (kind == MemoryKind.DirectObservation &&
            (sourceConversationId is not null ||
             retellCount != 0 ||
             confidencePermille != 1000 ||
             originalObserverId != lastInformantId))
        {
            throw new ArgumentException("Direct observations cannot contain rumor provenance or reduced truth confidence.", nameof(kind));
        }

        if (kind == MemoryKind.HeardRumor &&
            (sourceConversationId is null || retellCount == 0 || confidencePermille == 0))
        {
            throw new ArgumentException("Rumors require a source conversation, a retell count and positive confidence.", nameof(kind));
        }

        EventId = eventId;
        SubjectId = subjectId;
        OriginalObserverId = originalObserverId;
        LastInformantId = lastInformantId;
        Description = description.Trim();
        EventTick = eventTick;
        LearnedAtTick = learnedAtTick;
        Kind = kind;
        ConfidencePermille = confidencePermille;
        RetellCount = retellCount;
        SourceConversationId = sourceConversationId;
        RecallStrengthPermille = recallStrengthPermille;
        ImportancePermille = importancePermille;
        LastEvaluatedTick = evaluatedAt;
    }

    public Guid EventId { get; }

    public ResidentId SubjectId { get; }

    public ResidentId OriginalObserverId { get; }

    public ResidentId LastInformantId { get; }

    public string Description { get; }

    public long EventTick { get; }

    public long LearnedAtTick { get; }

    public MemoryKind Kind { get; }

    /// <summary>Confidence that a rumor is true, in thousandths. Direct observations remain 1000.</summary>
    public int ConfidencePermille { get; }

    /// <summary>How readily this resident can recall the memory, in thousandths.</summary>
    public int RecallStrengthPermille { get; }

    /// <summary>Caller-supplied retention importance, in thousandths.</summary>
    public int ImportancePermille { get; }

    /// <summary>The last simulation tick at which the memory lifecycle was evaluated.</summary>
    public long LastEvaluatedTick { get; }

    public int RetellCount { get; }

    public Guid? SourceConversationId { get; }

    internal MemoryEntry WithLifecycle(int recallStrengthPermille, long lastEvaluatedTick) =>
        new(
            EventId,
            SubjectId,
            OriginalObserverId,
            LastInformantId,
            Description,
            EventTick,
            LearnedAtTick,
            Kind,
            ConfidencePermille,
            RetellCount,
            SourceConversationId,
            recallStrengthPermille,
            ImportancePermille,
            lastEvaluatedTick);
}
