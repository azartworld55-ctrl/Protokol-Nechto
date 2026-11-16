namespace Protocol.ProfessionConfig;

/// <summary>
/// Applies a deterministic quota allocation to an already generated roster.
/// The caller supplies the immutable resident-copy operation, keeping this
/// package independent from Protocol.Core and any game/runtime assemblies.
/// </summary>
public sealed class ProfessionRosterComposer
{
    private readonly ProfessionQuotaAllocator _allocator;

    public ProfessionRosterComposer(ProfessionQuotaAllocator? allocator = null)
    {
        _allocator = allocator ?? new ProfessionQuotaAllocator();
    }

    /// <summary>
    /// Returns a new read-only roster in the same resident order, with exactly
    /// twelve profession assignments matching the supplied configuration.
    /// </summary>
    /// <remarks>
    /// The callback should return a new resident value rather than mutate the
    /// supplied resident. The composer cannot enforce immutability of TResident.
    /// </remarks>
    public IReadOnlyList<TResident> Compose<TResident>(
        IReadOnlyList<TResident> residents,
        ProfessionQuotaConfiguration configuration,
        long seed,
        Func<TResident, string, TResident> assignProfession)
    {
        ArgumentNullException.ThrowIfNull(residents);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(assignProfession);

        if (residents.Count != ProfessionQuotaConfiguration.RequiredResidentCount)
        {
            throw new ArgumentException(
                $"A roster must contain exactly {ProfessionQuotaConfiguration.RequiredResidentCount} residents.",
                nameof(residents));
        }

        var residentSnapshot = residents.ToArray();
        if (residentSnapshot.Length != ProfessionQuotaConfiguration.RequiredResidentCount)
        {
            throw new ArgumentException(
                $"A roster must enumerate exactly {ProfessionQuotaConfiguration.RequiredResidentCount} residents.",
                nameof(residents));
        }

        if (residentSnapshot.Any(resident => resident is null))
            throw new ArgumentException("A roster cannot contain null residents.", nameof(residents));

        var assignments = _allocator.Allocate(configuration, seed);
        var composed = new TResident[ProfessionQuotaConfiguration.RequiredResidentCount];

        for (var index = 0; index < composed.Length; index++)
        {
            var resident = assignProfession(residentSnapshot[index], assignments[index]);
            if (resident is null)
            {
                throw new InvalidOperationException(
                    $"The profession assignment callback returned null for roster slot {index}.");
            }

            composed[index] = resident;
        }

        return Array.AsReadOnly(composed);
    }
}
