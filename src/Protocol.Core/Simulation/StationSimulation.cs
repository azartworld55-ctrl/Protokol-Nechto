using Protocol.Core.Social;

namespace Protocol.Core.Simulation;

/// <summary>
/// Small deterministic simulation facade. Each resident acts from their own
/// needs and knowledge; this class does not expose hidden information to them.
/// </summary>
public sealed class StationSimulation
{
    private readonly ResidentDecisionEngine _decisionEngine;
    private readonly ResidentWorkPlanner _workPlanner;

    public StationSimulation(
        IEnumerable<ResidentMind> residents,
        RelationshipGraph? relationships = null,
        ResidentDecisionEngine? decisionEngine = null,
        ResidentWorkPlanner? workPlanner = null)
    {
        ArgumentNullException.ThrowIfNull(residents);
        Minds = residents.ToDictionary(mind => mind.Resident.Id);
        if (Minds.Count == 0)
            throw new ArgumentException("A simulation requires at least one resident.", nameof(residents));

        Relationships = relationships ?? new RelationshipGraph();
        _decisionEngine = decisionEngine ?? new ResidentDecisionEngine();
        _workPlanner = workPlanner ?? new ResidentWorkPlanner();
        ActiveWorkOrders = Array.Empty<ResidentWorkOrder>();
    }

    public IReadOnlyDictionary<Guid, ResidentMind> Minds { get; }
    public RelationshipGraph Relationships { get; }
    public IReadOnlyList<ResidentWorkOrder> ActiveWorkOrders { get; private set; }
    public long Tick { get; private set; }

    public IReadOnlyDictionary<Guid, ResidentAction> AdvanceHour(int hourOfDay)
    {
        if (hourOfDay is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(hourOfDay));

        Tick++;
        var residents = Minds.Values.Select(mind => mind.Resident).ToArray();
        var actions = new Dictionary<Guid, ResidentAction>();

        foreach (var mind in Minds.Values.OrderBy(item => item.Resident.Id))
        {
            mind.Needs.Advance();
            var action = _decisionEngine.ChooseNext(
                mind,
                residents,
                Relationships,
                hourOfDay);
            mind.SetAction(action);
            mind.Needs.Satisfy(action);
            actions.Add(mind.Resident.Id, action);
        }

        ActiveWorkOrders = _workPlanner.Plan(Minds.Values, hourOfDay);
        return actions;
    }
}
