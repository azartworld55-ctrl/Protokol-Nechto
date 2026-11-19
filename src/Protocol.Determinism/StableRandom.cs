using System.Globalization;

namespace Protocol.Determinism;

/// <summary>
/// Small deterministic PRNG for simulation and configuration generation.
/// This is not a cryptographic random-number generator.
/// </summary>
public sealed class StableRandom
{
    private ulong _state;

    public StableRandom(long seed)
    {
        _state = unchecked((ulong)seed);
    }

    /// <summary>
    /// Returns the next SplitMix64 value. The algorithm and constants are part
    /// of the deterministic generation contract; do not replace casually.
    /// </summary>
    public ulong NextUInt64()
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

    /// <summary>
    /// Returns an unbiased integer in [0, exclusiveMaximum).
    /// </summary>
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

    /// <summary>
    /// Returns a value in [0, 1) using the high 53 random bits.
    /// </summary>
    public double NextDouble()
    {
        const double scale = 1.0 / 9007199254740992.0;
        return (NextUInt64() >> 11) * scale;
    }

    /// <summary>
    /// Creates a stable non-empty identifier from two generated 64-bit words.
    /// </summary>
    public Guid NextGuid()
    {
        while (true)
        {
            var text =
                NextUInt64().ToString("x16", CultureInfo.InvariantCulture) +
                NextUInt64().ToString("x16", CultureInfo.InvariantCulture);
            var id = Guid.ParseExact(text, "N");
            if (id != Guid.Empty)
                return id;
        }
    }

    /// <summary>
    /// In-place Fisher-Yates shuffle using the same unbiased bounded sampler.
    /// </summary>
    public void Shuffle<T>(IList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        for (var index = values.Count - 1; index > 0; index--)
        {
            var swapIndex = NextInt(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }
}
