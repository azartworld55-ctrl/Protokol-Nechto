namespace Protocol.Core.Social;

/// <summary>
/// Evidence collected by one investigator about one resident. Information
/// must be explicitly submitted; the case file never reads other residents'
/// private memories automatically.
/// </summary>
public sealed class InvestigationCaseFile
{
    private readonly HashSet<Guid> _knownResidentIds;
    private readonly List<InvestigationEvidence> _evidence = [];

    public InvestigationCaseFile(
        Guid investigatorId,
        Guid suspectId,
        IEnumerable<Guid> knownResidentIds)
    {
        ArgumentNullException.ThrowIfNull(knownResidentIds);
        if (investigatorId == Guid.Empty)
            throw new ArgumentException("Investigator id cannot be empty.", nameof(investigatorId));
        if (suspectId == Guid.Empty || investigatorId == suspectId)
            throw new ArgumentException("Investigator and suspect must be different valid residents.", nameof(suspectId));

        _knownResidentIds = knownResidentIds.ToHashSet();
        if (!_knownResidentIds.Contains(investigatorId) || !_knownResidentIds.Contains(suspectId))
            throw new ArgumentException("Investigator and suspect must belong to the known station residents.", nameof(knownResidentIds));

        InvestigatorId = investigatorId;
        SuspectId = suspectId;
    }

    public Guid InvestigatorId { get; }
    public Guid SuspectId { get; }
    public IReadOnlyList<InvestigationEvidence> Evidence => _evidence.AsReadOnly();

    public bool AddEvidence(InvestigationEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.SuspectId != SuspectId)
            throw new ArgumentException("Evidence belongs to a different suspect.", nameof(evidence));
        if (!_knownResidentIds.Contains(evidence.ReporterId))
            throw new ArgumentException("Evidence reporter is not a known station resident.", nameof(evidence));

        if (_evidence.Any(existing =>
            existing.ReporterId == evidence.ReporterId &&
            string.Equals(existing.EventCode, evidence.EventCode, StringComparison.Ordinal)))
            return false;

        _evidence.Add(evidence);
        return true;
    }

    public EvidenceAssessment Assess()
    {
        var combined = 1.0;
        foreach (var evidence in _evidence)
            combined *= 1.0 - evidence.WeightedStrength;

        return new EvidenceAssessment(
            SuspectId,
            (int)Math.Round((1.0 - combined) * 100, MidpointRounding.AwayFromZero),
            _evidence.Count(item => item.Kind == InvestigationEvidenceKind.DirectObservation),
            _evidence.Count(item => item.Kind == InvestigationEvidenceKind.PhysicalTrace),
            _evidence.Count(item => item.Kind == InvestigationEvidenceKind.Rumor),
            _evidence.Select(item => item.ReporterId).Distinct().Count(),
            _evidence.Count == 0 ? null : _evidence.Max(item => item.Tick),
            _evidence.OrderByDescending(item => item.Tick).ToArray());
    }
}
