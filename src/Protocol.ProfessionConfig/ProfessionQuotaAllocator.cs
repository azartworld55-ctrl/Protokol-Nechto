using Protocol.Determinism;

namespace Protocol.ProfessionConfig;

/// <summary>
/// Expands an explicit quota configuration into twelve profession assignments.
/// A fixed SplitMix64 stream and Fisher-Yates shuffle make results reproducible
/// across supported .NET runtimes for the same seed and configuration order.
/// </summary>
public sealed class ProfessionQuotaAllocator
{
    public IReadOnlyList<string> Allocate(
        ProfessionQuotaConfiguration configuration,
        long seed)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var assignments = new string[ProfessionQuotaConfiguration.RequiredResidentCount];
        var nextIndex = 0;

        foreach (var quota in configuration.Quotas)
        {
            for (var i = 0; i < quota.Count; i++)
                assignments[nextIndex++] = quota.ProfessionId;
        }

        var random = new StableRandom(seed);
        random.Shuffle(assignments);
        return Array.AsReadOnly(assignments);
    }
}
