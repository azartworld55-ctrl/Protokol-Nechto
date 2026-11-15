using Protocol.Core.Simulation;

namespace Protocol.Core.Social;

public enum SuspicionResponse
{
    Unconcerned,
    Cautious,
    Avoid,
    ReportConcern
}

public sealed record ResidentSuspicionAssessment(
    Guid ObserverId,
    Guid SuspectId,
    int Score,
    int DirectObservations,
    int Rumors,
    SuspicionResponse Response);

/// <summary>
/// Evaluates only the observer's own journal. The service does not inspect
/// other residents' memories, the parasite role, or hidden station events.
/// </summary>
public sealed class SuspicionResponseService
{
    public ResidentSuspicionAssessment Evaluate(
        ResidentMind observer,
        Guid suspectId,
        RelationshipGraph relationships)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(relationships);
        if (suspectId == Guid.Empty || suspectId == observer.Resident.Id)
            throw new ArgumentException("Suspect must be another valid resident.", nameof(suspectId));

        var known = observer.Memories.Entries
            .Where(entry => entry.SubjectId == suspectId)
            .ToArray();
        var direct = known.Where(entry => entry.Origin == MemoryOrigin.Observation).ToArray();
        var rumors = known.Where(entry => entry.Origin == MemoryOrigin.Rumor).ToArray();

        var rawScore = direct.Sum(entry => entry.Confidence * 55.0)
            + rumors.Sum(entry => entry.Confidence * 22.0);
        var trust = relationships.Get(observer.Resident.Id, suspectId).Trust;
        rawScore += Math.Max(0, -trust) * 0.18;
        var score = (int)Math.Clamp(Math.Round(rawScore), 0, 100);

        var response = score switch
        {
            >= 75 => SuspicionResponse.ReportConcern,
            >= 50 => SuspicionResponse.Avoid,
            >= 25 => SuspicionResponse.Cautious,
            _ => SuspicionResponse.Unconcerned
        };

        return new ResidentSuspicionAssessment(
            observer.Resident.Id, suspectId, score, direct.Length, rumors.Length, response);
    }
}
