namespace Protocol.Core.Social;

public enum MemoryOrigin
{
    Observation,
    Rumor
}

/// <summary>
/// A compact social memory. EventCode is an internal identifier, not player-facing prose.
/// Confidence is between 0 and 1.
/// </summary>
public sealed record MemoryEntry
{
    public MemoryEntry(
        Guid subjectId,
        string eventCode,
        long tick,
        double confidence,
        MemoryOrigin origin)
    {
        if (subjectId == Guid.Empty)
            throw new ArgumentException("Subject id cannot be empty.", nameof(subjectId));

        if (string.IsNullOrWhiteSpace(eventCode))
            throw new ArgumentException("Event code is required.", nameof(eventCode));

        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));

        if (double.IsNaN(confidence) || confidence is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(confidence));

        SubjectId = subjectId;
        EventCode = eventCode.Trim();
        Tick = tick;
        Confidence = confidence;
        Origin = origin;
    }

    public Guid SubjectId { get; }
    public string EventCode { get; }
    public long Tick { get; }
    public double Confidence { get; }
    public MemoryOrigin Origin { get; }

    public MemoryEntry Retold(long receivedAtTick, double reliability = 0.75)
    {
        if (double.IsNaN(reliability) || reliability is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(reliability));

        return new MemoryEntry(
            SubjectId,
            EventCode,
            receivedAtTick,
            Confidence * reliability,
            MemoryOrigin.Rumor);
    }
}
