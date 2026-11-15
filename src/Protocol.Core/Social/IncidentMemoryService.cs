namespace Protocol.Core.Social;

/// <summary>
/// Records an incident only for explicitly supplied observers and converts
/// only their own knowledge into investigation evidence. It never queries
/// hidden map state or broadcasts a station-wide alert.
/// </summary>
public sealed class IncidentMemoryService
{
    public int RecordForObservers(
        ResidentIncident incident,
        IEnumerable<Guid> observerIds,
        IReadOnlyDictionary<Guid, MemoryJournal> journals)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(observerIds);
        ArgumentNullException.ThrowIfNull(journals);

        var recorded = 0;
        foreach (var observerId in observerIds.Distinct())
        {
            if (observerId == Guid.Empty)
                throw new ArgumentException("Observer id cannot be empty.", nameof(observerIds));
            if (observerId == incident.ActorId)
                continue;
            if (!journals.TryGetValue(observerId, out var journal))
                throw new ArgumentException("An observer has no registered memory journal.", nameof(observerIds));

            if (journal.HasHeard(incident.EventCode))
                continue;

            journal.Remember(new MemoryEntry(
                incident.ActorId,
                incident.EventCode,
                incident.Tick,
                1.0,
                MemoryOrigin.Observation));
            recorded++;
        }

        return recorded;
    }

    public bool TrySubmitKnownIncident(
        Guid investigatorId,
        InvestigationCaseFile caseFile,
        MemoryJournal investigatorJournal,
        long submittedAtTick)
    {
        if (investigatorId == Guid.Empty)
            throw new ArgumentException("Investigator id cannot be empty.", nameof(investigatorId));
        ArgumentNullException.ThrowIfNull(caseFile);
        ArgumentNullException.ThrowIfNull(investigatorJournal);
        if (caseFile.InvestigatorId != investigatorId)
            throw new ArgumentException("Investigator does not own this case file.", nameof(caseFile));
        if (submittedAtTick < 0)
            throw new ArgumentOutOfRangeException(nameof(submittedAtTick));

        var knownMemories = investigatorJournal.Entries
            .Where(entry => entry.SubjectId == caseFile.SuspectId)
            .Where(entry => !caseFile.Evidence.Any(evidence =>
                evidence.ReporterId == investigatorId &&
                string.Equals(evidence.EventCode, entry.EventCode, StringComparison.Ordinal)))
            .ToArray();

        var added = false;
        foreach (var memory in knownMemories)
        {
            if (memory.Tick > submittedAtTick)
                continue;

            var kind = memory.Origin == MemoryOrigin.Observation
                ? InvestigationEvidenceKind.DirectObservation
                : InvestigationEvidenceKind.Rumor;
            added |= caseFile.AddEvidence(new InvestigationEvidence(
                investigatorId,
                caseFile.SuspectId,
                memory.EventCode,
                memory.Tick,
                memory.Confidence,
                kind));
        }

        return added;
    }

    public bool TrySubmitPhysicalTrace(
        Guid reporterId,
        InvestigationCaseFile caseFile,
        string traceEventCode,
        long observedAtTick,
        double confidence)
    {
        ArgumentNullException.ThrowIfNull(caseFile);
        return caseFile.AddEvidence(new InvestigationEvidence(
            reporterId,
            caseFile.SuspectId,
            traceEventCode,
            observedAtTick,
            confidence,
            InvestigationEvidenceKind.PhysicalTrace));
    }
}
