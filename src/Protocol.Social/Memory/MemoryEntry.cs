using Protocol.Social.Domain;

namespace Protocol.Social.Memory;

public enum MemoryKind
{
    DirectObservation = 0,
    HeardRumor = 1
}

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
        Guid? sourceConversationId)
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

        if (kind == MemoryKind.DirectObservation &&
            (sourceConversationId is not null ||
             retellCount != 0 ||
             confidencePermille != 1000 ||
             originalObserverId != lastInformantId))
        {
            throw new ArgumentException("Direct observations cannot contain rumor provenance or decay.", nameof(kind));
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
    }

    public Guid EventId { get; }

    public ResidentId SubjectId { get; }

    public ResidentId OriginalObserverId { get; }

    public ResidentId LastInformantId { get; }

    public string Description { get; }

    public long EventTick { get; }

    public long LearnedAtTick { get; }

    public MemoryKind Kind { get; }

    /// <summary>Confidence in thousandths: 1000 is full confidence, 0 is none.</summary>
    public int ConfidencePermille { get; }

    public int RetellCount { get; }

    public Guid? SourceConversationId { get; }
}
