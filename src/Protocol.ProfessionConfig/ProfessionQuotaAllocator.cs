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
        for (var i = assignments.Length - 1; i > 0; i--)
        {
            var swapIndex = random.NextInt(i + 1);
            (assignments[i], assignments[swapIndex]) =
                (assignments[swapIndex], assignments[i]);
        }

        return Array.AsReadOnly(assignments);
    }

    private sealed class StableRandom
    {
        private ulong _state;

        public StableRandom(long seed)
        {
            _state = unchecked((ulong)seed);
        }

        public int NextInt(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));

            var bound = (ulong)exclusiveMaximum;
            var rejectionThreshold = unchecked(0UL - bound) % bound;

            while (true)
            {
                var value = NextUInt64();
                if (value >= rejectionThreshold)
                    return (int)(value % bound);
            }
        }

        private ulong NextUInt64()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                var value = _state;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }
    }
}
