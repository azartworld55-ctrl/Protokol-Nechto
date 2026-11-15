namespace Protocol.Core.Simulation;

/// <summary>
/// A candidate the parasite actually observed during the current perception step.
/// The adapter must only include residents visible to the parasite.
/// </summary>
public sealed record ParasiteOpportunity(
    Guid ResidentId,
    bool IsLivingHuman,
    int Distance,
    int NearbyResidents,
    bool HasLineOfSight,
    double SuspicionRisk)
{
    public bool IsIsolated => NearbyResidents == 0;
}

/// <summary>
/// Facts personally observed by the parasite. This is deliberately not a map
/// snapshot: hidden residents and unseen events cannot enter this decision.
/// </summary>
public sealed class ParasiteObservation
{
    public ParasiteObservation(
        long tick,
        IReadOnlyCollection<ParasiteOpportunity> opportunities)
    {
        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));
        ArgumentNullException.ThrowIfNull(opportunities);
        if (opportunities.Any(item => item.ResidentId == Guid.Empty ||
                                      item.Distance < 0 ||
                                      double.IsNaN(item.SuspicionRisk) ||
                                      item.SuspicionRisk is < 0 or > 1))
            throw new ArgumentException("Observation contains invalid opportunity data.", nameof(opportunities));

        Tick = tick;
        Opportunities = opportunities
            .GroupBy(item => item.ResidentId)
            .Select(group => group.OrderBy(item => item.Distance).First())
            .ToArray();
    }

    public long Tick { get; }
    public IReadOnlyList<ParasiteOpportunity> Opportunities { get; }
}

public enum ParasiteDecisionKind
{
    Wait,
    Observe,
    PursueOpportunity
}

public sealed record ParasiteDecision(
    ParasiteDecisionKind Kind,
    Guid? TargetId,
    double Evaluation,
    string Reason);

/// <summary>
/// Minimal persistent state for the parasite's own decision process.
/// It remembers only targets it has personally observed.
/// </summary>
public sealed class ParasiteMind
{
    private readonly Dictionary<Guid, long> _lastObservedAt = [];

    public Guid ParasiteId { get; }
    public Guid? PreferredTargetId { get; private set; }
    public int ConsecutiveWaits { get; private set; }

    public ParasiteMind(Guid parasiteId)
    {
        if (parasiteId == Guid.Empty)
            throw new ArgumentException("Parasite id cannot be empty.", nameof(parasiteId));
        ParasiteId = parasiteId;
    }

    internal void RecordObservation(ParasiteObservation observation)
    {
        foreach (var opportunity in observation.Opportunities)
            _lastObservedAt[opportunity.ResidentId] = observation.Tick;
    }

    internal bool HasObserved(Guid residentId) => _lastObservedAt.ContainsKey(residentId);

    internal void SetDecision(ParasiteDecision decision)
    {
        if (decision.Kind == ParasiteDecisionKind.PursueOpportunity)
        {
            PreferredTargetId = decision.TargetId;
            ConsecutiveWaits = 0;
        }
        else
        {
            ConsecutiveWaits++;
        }
    }
}

/// <summary>
/// Chooses between waiting, observing and pursuing an already observed
/// opportunity. It never searches the station or reads unseen pawn positions.
/// </summary>
public sealed class ParasiteDecisionEngine
{
    public ParasiteDecision Decide(ParasiteMind mind, ParasiteObservation observation)
    {
        ArgumentNullException.ThrowIfNull(mind);
        ArgumentNullException.ThrowIfNull(observation);
        mind.RecordObservation(observation);

        var candidates = observation.Opportunities
            .Where(item => item.ResidentId != mind.ParasiteId && item.IsLivingHuman)
            .Where(item => item.HasLineOfSight && item.IsIsolated)
            .Where(item => item.SuspicionRisk <= 0.55)
            .Select(item => new
            {
                Opportunity = item,
                Score = 100.0
                    - item.Distance * 2.0
                    - item.SuspicionRisk * 45.0
                    + (mind.PreferredTargetId == item.ResidentId ? 8.0 : 0.0)
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Opportunity.ResidentId)
            .ToArray();

        ParasiteDecision decision;
        if (candidates.Length > 0)
        {
            var selected = candidates[0];
            decision = new ParasiteDecision(
                ParasiteDecisionKind.PursueOpportunity,
                selected.Opportunity.ResidentId,
                selected.Score,
                "An observed, isolated opportunity has an acceptable suspicion risk.");
        }
        else if (observation.Opportunities.Count == 0)
        {
            decision = new ParasiteDecision(
                ParasiteDecisionKind.Observe, null, 0,
                "No residents were observed; the parasite has no basis for selecting a target.");
        }
        else
        {
            decision = new ParasiteDecision(
                ParasiteDecisionKind.Wait, null, 0,
                "No observed opportunity currently meets the parasite's risk limits.");
        }

        mind.SetDecision(decision);
        return decision;
    }
}
