using Protocol.Core.Simulation;

namespace Protocol.Core.Social;

public sealed record SocialConsequenceResult(
    ResidentSuspicionAssessment Assessment,
    int TrustChange,
    bool ShouldOpenInvestigation);

/// <summary>
/// Applies a modest social reaction only to an observer who has a relevant
/// personal memory. It does not establish guilt or automatically detain anyone.
/// </summary>
public sealed class SocialConsequenceService
{
    private readonly SuspicionResponseService _suspicion = new();

    public SocialConsequenceResult ProcessKnownConcern(
        ResidentMind observer,
        Guid suspectId,
        RelationshipGraph relationships)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(relationships);

        var assessment = _suspicion.Evaluate(observer, suspectId, relationships);
        if (assessment.DirectObservations == 0 && assessment.Rumors == 0)
            return new SocialConsequenceResult(assessment, 0, false);

        var trustChange = assessment.Response switch
        {
            SuspicionResponse.ReportConcern => -8,
            SuspicionResponse.Avoid => -5,
            SuspicionResponse.Cautious => -2,
            _ => 0
        };

        if (trustChange != 0)
            relationships.AdjustTrust(observer.Resident.Id, suspectId, trustChange);

        return new SocialConsequenceResult(
            assessment,
            trustChange,
            assessment.Response == SuspicionResponse.ReportConcern);
    }
}
