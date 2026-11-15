namespace Protocol.Core.Social;

public enum AccusationAssessment
{
    StronglySupported,
    Plausible,
    Inconclusive,
    Weak
}

public sealed record AccusationResult(
    Guid AccuserId,
    Guid AccusedId,
    AccusationAssessment Assessment,
    int SuspicionScore,
    int TrustChange,
    int IndependentReporterCount,
    int DirectObservationCount);

/// <summary>
/// Converts an investigator's current evidence into a social accusation.
/// It deliberately judges support, not guilt: even a strong case is not a
/// magical confirmation of the hidden parasite identity.
/// </summary>
public sealed class AccusationService
{
    public AccusationResult Issue(
        InvestigationCaseFile caseFile,
        RelationshipGraph relationships)
    {
        ArgumentNullException.ThrowIfNull(caseFile);
        ArgumentNullException.ThrowIfNull(relationships);

        var evidence = caseFile.Assess();
        var assessment = evidence.SuspicionScore >= 70 &&
                         evidence.DirectObservationCount >= 2 &&
                         evidence.IndependentReporterCount >= 2
            ? AccusationAssessment.StronglySupported
            : evidence.SuspicionScore >= 55 && evidence.DirectObservationCount >= 1
                ? AccusationAssessment.Plausible
                : evidence.SuspicionScore >= 30
                    ? AccusationAssessment.Inconclusive
                    : AccusationAssessment.Weak;

        var trustChange = assessment switch
        {
            AccusationAssessment.StronglySupported => -5,
            AccusationAssessment.Plausible => -9,
            AccusationAssessment.Inconclusive => -13,
            _ => -18
        };

        relationships.AdjustTrust(caseFile.InvestigatorId, caseFile.SuspectId, trustChange);

        return new AccusationResult(
            caseFile.InvestigatorId,
            caseFile.SuspectId,
            assessment,
            evidence.SuspicionScore,
            trustChange,
            evidence.IndependentReporterCount,
            evidence.DirectObservationCount);
    }
}
