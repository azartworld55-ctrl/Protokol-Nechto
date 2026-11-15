namespace Protocol.Core.Domain;

/// <summary>
/// A generated starting group. The parasite identity is stored separately
/// from ordinary resident data so UI code can avoid exposing it accidentally.
/// </summary>
public sealed class StationRoster
{
    public const int RequiredResidentCount = 12;

    public StationRoster(
        IReadOnlyCollection<Resident> residents,
        Guid playerResidentId,
        Guid parasiteResidentId)
    {
        ArgumentNullException.ThrowIfNull(residents);

        var snapshot = residents.ToArray();
        if (snapshot.Length != RequiredResidentCount)
            throw new ArgumentException($"A station roster must contain exactly {RequiredResidentCount} residents.", nameof(residents));

        if (snapshot.Any(resident => resident is null))
            throw new ArgumentException("Roster cannot contain null residents.", nameof(residents));

        if (snapshot.Select(resident => resident.Id).Distinct().Count() != RequiredResidentCount)
            throw new ArgumentException("Every resident must have a unique id.", nameof(residents));

        var ids = snapshot.Select(resident => resident.Id).ToHashSet();
        if (!ids.Contains(playerResidentId))
            throw new ArgumentException("Player resident must belong to the roster.", nameof(playerResidentId));

        if (!ids.Contains(parasiteResidentId))
            throw new ArgumentException("Parasite resident must belong to the roster.", nameof(parasiteResidentId));

        if (playerResidentId == parasiteResidentId)
            throw new ArgumentException("The player must always control a human resident.", nameof(playerResidentId));

        Residents = Array.AsReadOnly(snapshot);
        PlayerResidentId = playerResidentId;
        ParasiteResidentId = parasiteResidentId;
    }

    public IReadOnlyList<Resident> Residents { get; }
    public Guid PlayerResidentId { get; }

    // Keep this value inside simulation logic; do not bind it directly to player-facing UI.
    internal Guid ParasiteResidentId { get; }
}
