using Protocol.Core.Domain;
using Protocol.Core.Social;

namespace Protocol.Core.Simulation;

public enum ParasiteAttackFailure
{
    None,
    UnknownTarget,
    InvalidTarget,
    TargetAlreadyDead,
    NotAlone,
    WitnessPresent
}

public sealed record ParasiteAttackResult(
    bool Succeeded,
    ParasiteAttackFailure Failure,
    int MemoriesCopied,
    bool PlayerWasKilled);

/// <summary>
/// The identity the parasite currently presents to other residents.
/// This is a simulation snapshot, not a change to the victim's original record.
/// </summary>
public sealed record ResidentIdentitySnapshot(
    string Name,
    int Age,
    string Profession,
    ResidentPersonality Personality,
    IReadOnlyList<ResidentHabit> Habits)
{
    public static ResidentIdentitySnapshot From(Resident resident)
    {
        ArgumentNullException.ThrowIfNull(resident);
        return new ResidentIdentitySnapshot(
            resident.Name,
            resident.Age,
            resident.Profession,
            resident.Personality,
            Array.AsReadOnly(resident.Habits.ToArray()));
    }
}

/// <summary>
/// Tracks the parasite's single, persistent identity and the humans still
/// available for player control. A successful attack requires the parasite
/// and victim to be the only two residents present and no witnesses.
/// </summary>
public sealed class ParasiteLifecycle
{
    private readonly Guid _parasiteId;
    private readonly ResidentMind _parasiteMind;
    private readonly HashSet<Guid> _knownResidentIds;
    private readonly HashSet<Guid> _deadResidentIds = [];

    public ParasiteLifecycle(
        Guid parasiteId,
        IEnumerable<ResidentMind> residents,
        Guid playerResidentId)
    {
        ArgumentNullException.ThrowIfNull(residents);
        var minds = residents.ToDictionary(mind => mind.Resident.Id);

        if (!minds.ContainsKey(parasiteId))
            throw new ArgumentException("The parasite must belong to the station.", nameof(parasiteId));

        if (!minds.ContainsKey(playerResidentId) || playerResidentId == parasiteId)
            throw new ArgumentException("The player must be a known human resident.", nameof(playerResidentId));

        _parasiteId = parasiteId;
        _parasiteMind = minds[parasiteId];
        _knownResidentIds = minds.Keys.ToHashSet();
        PlayerResidentId = playerResidentId;
    }

    public Guid? PlayerResidentId { get; private set; }
    public ResidentIdentitySnapshot? CurrentDisguise { get; private set; }
    public IReadOnlySet<Guid> DeadResidentIds => _deadResidentIds;

    public bool IsAlive(Guid residentId) =>
        _knownResidentIds.Contains(residentId) && !_deadResidentIds.Contains(residentId);

    public ParasiteAttackResult TryConsume(
        ResidentMind victim,
        IEnumerable<Guid> residentsPresent,
        IEnumerable<Guid> witnesses)
    {
        ArgumentNullException.ThrowIfNull(victim);
        ArgumentNullException.ThrowIfNull(residentsPresent);
        ArgumentNullException.ThrowIfNull(witnesses);

        var victimId = victim.Resident.Id;
        if (!_knownResidentIds.Contains(victimId))
            return Failed(ParasiteAttackFailure.UnknownTarget);

        if (victimId == _parasiteId)
            return Failed(ParasiteAttackFailure.InvalidTarget);

        if (_deadResidentIds.Contains(victimId))
            return Failed(ParasiteAttackFailure.TargetAlreadyDead);

        var present = residentsPresent.ToHashSet();
        if (present.Count != 2 || !present.Contains(_parasiteId) || !present.Contains(victimId))
            return Failed(ParasiteAttackFailure.NotAlone);

        if (witnesses.Any(id => id != _parasiteId && id != victimId))
            return Failed(ParasiteAttackFailure.WitnessPresent);

        _deadResidentIds.Add(victimId);
        CurrentDisguise = ResidentIdentitySnapshot.From(victim.Resident);
        var copied = _parasiteMind.Memories.CopyAllFrom(victim.Memories);

        var playerWasKilled = PlayerResidentId == victimId;
        if (playerWasKilled)
            PlayerResidentId = null;

        return new ParasiteAttackResult(
            true,
            ParasiteAttackFailure.None,
            copied,
            playerWasKilled);
    }

    /// <summary>
    /// Selects a living human after the currently controlled resident dies.
    /// The parasite can never be selected as the player character.
    /// </summary>
    public bool SelectPlayer(Guid residentId)
    {
        if (residentId == _parasiteId || !IsAlive(residentId))
            return false;

        PlayerResidentId = residentId;
        return true;
    }

    private static ParasiteAttackResult Failed(ParasiteAttackFailure failure) =>
        new(false, failure, 0, false);
}
