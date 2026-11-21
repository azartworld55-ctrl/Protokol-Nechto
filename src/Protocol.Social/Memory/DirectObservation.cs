using Protocol.Social.Domain;

namespace Protocol.Social.Memory;

/// <summary>
/// An observation already validated by the caller's perception layer. EventTick
/// identifies the event; ObservedAtTick identifies when this resident learned it.
/// </summary>
public sealed class DirectObservation
{
    public DirectObservation(
        Guid eventId,
        ResidentId observerId,
        ResidentId subjectId,
        long tick,
        string description,
        int importancePermille = 500)
        : this(eventId, observerId, subjectId, tick, tick, description, importancePermille)
    {
    }

    public DirectObservation(
        Guid eventId,
        ResidentId observerId,
        ResidentId subjectId,
        long eventTick,
        long observedAtTick,
        string description,
        int importancePermille = 500)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event id cannot be empty.", nameof(eventId));

        ArgumentNullException.ThrowIfNull(observerId);
        ArgumentNullException.ThrowIfNull(subjectId);

        if (eventTick < 0)
            throw new ArgumentOutOfRangeException(nameof(eventTick));

        if (observedAtTick < eventTick)
            throw new ArgumentOutOfRangeException(nameof(observedAtTick));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Observation description is required.", nameof(description));

        if (importancePermille is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(importancePermille));

        EventId = eventId;
        ObserverId = observerId;
        SubjectId = subjectId;
        EventTick = eventTick;
        ObservedAtTick = observedAtTick;
        Description = description.Trim();
        ImportancePermille = importancePermille;
    }

    public Guid EventId { get; }

    public ResidentId ObserverId { get; }

    public ResidentId SubjectId { get; }

    public long EventTick { get; }

    public long ObservedAtTick { get; }

    /// <summary>Compatibility alias for the time the observation was recorded.</summary>
    public long Tick => ObservedAtTick;

    public string Description { get; }

    public int ImportancePermille { get; }
}
