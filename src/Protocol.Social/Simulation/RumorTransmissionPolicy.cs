namespace Protocol.Social.Simulation;

/// <summary>
/// Explicit tuning supplied by game design/configuration. The domain does not
/// silently choose trust thresholds or rumor decay values.
/// </summary>
public sealed record RumorTransmissionPolicy
{
    public RumorTransmissionPolicy(
        int minimumTrust,
        int minimumFamiliarity,
        int confidenceDecayPermille,
        int minimumConfidencePermille,
        int maximumRetellCount)
    {
        if (minimumTrust is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(minimumTrust));

        if (minimumFamiliarity is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(minimumFamiliarity));

        if (confidenceDecayPermille is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(confidenceDecayPermille));

        if (minimumConfidencePermille is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(minimumConfidencePermille));

        if (maximumRetellCount < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumRetellCount));

        MinimumTrust = minimumTrust;
        MinimumFamiliarity = minimumFamiliarity;
        ConfidenceDecayPermille = confidenceDecayPermille;
        MinimumConfidencePermille = minimumConfidencePermille;
        MaximumRetellCount = maximumRetellCount;
    }

    public int MinimumTrust { get; }

    public int MinimumFamiliarity { get; }

    /// <summary>Multiplier applied once per retelling, in thousandths.</summary>
    public int ConfidenceDecayPermille { get; }

    public int MinimumConfidencePermille { get; }

    public int MaximumRetellCount { get; }
}
