using Protocol.Social.Domain;

namespace Protocol.Social.Memory;

/// <summary>
/// An observation already validated by the caller's perception layer.
/// This type does not decide what a resident can see.
/// </summary>
public sealed class DirectObservation
{
    public DirectObservation(
        Guid eventId,
        ResidentId observerId,
        ResidentId subjectId,
        long tick,
        string description)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event id cannot be empty.", nameof(eventId));

        ArgumentNullException.ThrowIfNull(observerId);
        ArgumentNullException.ThrowIfNull(subjectId);

        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Observation description is required.", nameof(description));

        EventId = eventId;
        ObserverId = observerId;
        SubjectId = subjectId;
        Tick = tick;
        Description = description.Trim();
    }

    public Guid EventId { get; }

    public ResidentId ObserverId { get; }

    public ResidentId SubjectId { get; }

    public long Tick { get; }

    public string Description { get; }
}
