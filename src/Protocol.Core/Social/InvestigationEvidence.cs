namespace Protocol.Core.Social;

public enum InvestigationEvidenceKind
{
    DirectObservation,
    PhysicalTrace,
    Rumor
}

/// <summary>
/// A piece of information known to an investigator. Evidence records what a
/// resident believes they learned; it does not reveal whether the suspect is
/// actually guilty.
/// </summary>
public sealed record InvestigationEvidence
{
    public InvestigationEvidence(
        Guid reporterId,
        Guid suspectId,
        string eventCode,
        long tick,
        double confidence,
        InvestigationEvidenceKind kind)
    {
        if (reporterId == Guid.Empty)
            throw new ArgumentException("Reporter id cannot be empty.", nameof(reporterId));
        if (suspectId == Guid.Empty)
            throw new ArgumentException("Suspect id cannot be empty.", nameof(suspectId));
        if (reporterId == suspectId)
            throw new ArgumentException("A suspect cannot report evidence against themselves.", nameof(reporterId));
        if (string.IsNullOrWhiteSpace(eventCode))
            throw new ArgumentException("Evidence event code is required.", nameof(eventCode));
        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));
        if (double.IsNaN(confidence) || confidence is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(confidence));

        ReporterId = reporterId;
        SuspectId = suspectId;
        EventCode = eventCode.Trim();
        Tick = tick;
        Confidence = confidence;
        Kind = kind;
    }

    public Guid ReporterId { get; }
    public Guid SuspectId { get; }
    public string EventCode { get; }
    public long Tick { get; }
    public double Confidence { get; }
    public InvestigationEvidenceKind Kind { get; }

    public double WeightedStrength => Confidence * (Kind switch
    {
        InvestigationEvidenceKind.DirectObservation => 0.9,
        InvestigationEvidenceKind.PhysicalTrace => 0.65,
        InvestigationEvidenceKind.Rumor => 0.3,
        _ => 0
    });
}

public sealed record EvidenceAssessment(
    Guid SuspectId,
    int SuspicionScore,
    int DirectObservationCount,
    int PhysicalTraceCount,
    int RumorCount,
    int IndependentReporterCount,
    long? LatestEvidenceTick,
    IReadOnlyList<InvestigationEvidence> Evidence);
