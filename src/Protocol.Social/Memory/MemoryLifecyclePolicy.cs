namespace Protocol.Social.Memory;

/// <summary>
/// Explicit memory-retention tuning supplied by the caller. Tick units are
/// domain-defined; this policy does not assume a RimWorld tick rate.
/// </summary>
public sealed record MemoryLifecyclePolicy
{
    public MemoryLifecyclePolicy(
        int recallDecayPerTickPermille,
        int forgetBelowRecallStrengthPermille,
        long minimumAgeBeforeForgettingTicks,
        int maximumEntries)
    {
        if (recallDecayPerTickPermille is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(recallDecayPerTickPermille));

        if (forgetBelowRecallStrengthPermille is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(forgetBelowRecallStrengthPermille));

        if (minimumAgeBeforeForgettingTicks < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumAgeBeforeForgettingTicks));

        if (maximumEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumEntries));

        RecallDecayPerTickPermille = recallDecayPerTickPermille;
        ForgetBelowRecallStrengthPermille = forgetBelowRecallStrengthPermille;
        MinimumAgeBeforeForgettingTicks = minimumAgeBeforeForgettingTicks;
        MaximumEntries = maximumEntries;
    }

    /// <summary>Linear recall-strength loss per elapsed tick, in thousandths.</summary>
    public int RecallDecayPerTickPermille { get; }

    /// <summary>Entries at or below this strength may be forgotten after the minimum age.</summary>
    public int ForgetBelowRecallStrengthPermille { get; }

    public long MinimumAgeBeforeForgettingTicks { get; }

    public int MaximumEntries { get; }
}
