namespace Protocol.ProfessionConfig;

/// <summary>
/// Explicit profession quotas for the fixed twelve-resident station.
/// A configuration is valid only when its quotas sum to exactly twelve.
/// </summary>
public sealed class ProfessionQuotaConfiguration
{
    public const int RequiredResidentCount = 12;

    public ProfessionQuotaConfiguration(IEnumerable<ProfessionQuota> quotas)
    {
        ArgumentNullException.ThrowIfNull(quotas);

        // Every quota must be positive and the total must be twelve, so a valid
        // configuration can contain at most twelve entries. Enumerate into a
        // bounded snapshot instead of trusting arbitrary IEnumerable/CopyTo
        // implementations or materializing an unbounded sequence.
        var snapshot = new List<ProfessionQuota>(RequiredResidentCount);
        foreach (var quota in quotas)
        {
            if (snapshot.Count == RequiredResidentCount)
            {
                throw new ArgumentException(
                    $"A configuration cannot contain more than {RequiredResidentCount} profession quotas.",
                    nameof(quotas));
            }

            if (quota is null)
                throw new ArgumentException("Profession quotas cannot contain null entries.", nameof(quotas));

            snapshot.Add(quota);
        }

        if (snapshot.Count == 0)
            throw new ArgumentException("At least one profession quota is required.", nameof(quotas));

        var duplicateIds = snapshot
            .GroupBy(quota => quota.ProfessionId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateIds.Length > 0)
            throw new ArgumentException(
                $"Profession ids must be unique (case-insensitive): {string.Join(", ", duplicateIds)}.",
                nameof(quotas));

        var total = snapshot.Sum(quota => (long)quota.Count);
        if (total != RequiredResidentCount)
            throw new ArgumentException(
                $"Profession quotas must sum to exactly {RequiredResidentCount}; configured total is {total}.",
                nameof(quotas));

        Quotas = snapshot.AsReadOnly();
    }

    public IReadOnlyList<ProfessionQuota> Quotas { get; }
}
