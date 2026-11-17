namespace Protocol.Core.Domain;

/// <summary>
/// A generated station group. The parasite identity is kept internal so a
/// player-facing projection cannot reveal it by accident.
/// </summary>
public sealed class StationRoster
{
    public const int RequiredResidentCount = 12;

    public StationRoster(
        IEnumerable<Resident> residents,
        Guid playerResidentId,
        Guid parasiteResidentId)
    {
        ArgumentNullException.ThrowIfNull(residents);

        var snapshot = new List<Resident>(RequiredResidentCount);
        foreach (var resident in residents)
        {
            if (snapshot.Count == RequiredResidentCount)
                throw new ArgumentException($"A station roster must contain exactly {RequiredResidentCount} residents.", nameof(residents));

            if (resident is null)
                throw new ArgumentException("Roster cannot contain null residents.", nameof(residents));

            snapshot.Add(resident);
        }

        if (snapshot.Count != RequiredResidentCount)
            throw new ArgumentException($"A station roster must contain exactly {RequiredResidentCount} residents.", nameof(residents));

        if (snapshot.Select(resident => resident.Id).Distinct().Count() != RequiredResidentCount)
            throw new ArgumentException("Every resident must have a unique id.", nameof(residents));

        var ids = snapshot.Select(resident => resident.Id).ToHashSet();
        if (!ids.Contains(playerResidentId))
            throw new ArgumentException("Player resident must belong to the roster.", nameof(playerResidentId));

        if (!ids.Contains(parasiteResidentId))
            throw new ArgumentException("Parasite resident must belong to the roster.", nameof(parasiteResidentId));

        if (playerResidentId == parasiteResidentId)
            throw new ArgumentException("The player must always control a human resident.", nameof(playerResidentId));

        Residents = Array.AsReadOnly(snapshot.ToArray());
        PlayerResidentId = playerResidentId;
        ParasiteResidentId = parasiteResidentId;
    }

    public IReadOnlyList<Resident> Residents { get; }
    public Guid PlayerResidentId { get; }

    internal Guid ParasiteResidentId { get; }
}
