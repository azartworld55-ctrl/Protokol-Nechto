using Protocol.Determinism;
using Protocol.ProfessionConfig;

namespace Protocol.Core.Domain;

/// <summary>
/// Generates exactly twelve immutable resident profiles. Names, traits and
/// identities use the repository's fixed PRNG; professions come only from an
/// explicit, validated quota configuration.
/// </summary>
public sealed class StationRosterGenerator
{
    private static readonly string[] NamePool =
    [
        "Алекс", "Мира", "Илья", "Ника", "Дан", "Ева",
        "Марк", "Лея", "Роман", "Соня", "Тимур", "Ася",
        "Лев", "Вера", "Кирилл", "Эмма", "Олег", "Лина",
        "Павел", "Яна", "Глеб", "Ирина", "Максим", "Зоя"
    ];

    private readonly ProfessionRosterComposer _professionComposer;

    public StationRosterGenerator(ProfessionRosterComposer? professionComposer = null)
    {
        _professionComposer = professionComposer ?? new ProfessionRosterComposer();
    }

    public StationRoster Generate(
        long seed,
        ProfessionQuotaConfiguration professionConfiguration)
    {
        ArgumentNullException.ThrowIfNull(professionConfiguration);

        var random = new StableRandom(seed);
        var shuffledNames = NamePool.ToArray();
        random.Shuffle(shuffledNames);

        var drafts = new List<ResidentDraft>(StationRoster.RequiredResidentCount);
        for (var index = 0; index < StationRoster.RequiredResidentCount; index++)
        {
            var personality = new ResidentPersonality(
                random.NextInt(101),
                random.NextInt(101),
                random.NextInt(101),
                random.NextInt(101),
                random.NextInt(101));

            var availableHabits = Enum.GetValues<ResidentHabit>();
            random.Shuffle(availableHabits);
            var habitCount = random.NextInt(3) + 1;

            var availableTraits = Enum.GetValues<ResidentTrait>();
            random.Shuffle(availableTraits);
            var traitCount = random.NextInt(2) + 1;

            drafts.Add(new ResidentDraft(
                random.NextGuid(),
                shuffledNames[index],
                random.NextInt(46) + 20,
                personality,
                availableHabits.Take(habitCount).ToArray(),
                availableTraits.Take(traitCount).ToArray()));
        }

        var parasiteIndex = random.NextInt(StationRoster.RequiredResidentCount);
        var playerIndex = random.NextInt(StationRoster.RequiredResidentCount - 1);
        if (playerIndex >= parasiteIndex)
            playerIndex++;

        var assignedDrafts = _professionComposer.Compose(
            drafts,
            professionConfiguration,
            seed,
            static (draft, professionId) => draft with { ProfessionId = professionId });

        var residents = assignedDrafts
            .Select(draft => draft.ToResident())
            .ToArray();

        return new StationRoster(
            residents,
            residents[playerIndex].Id,
            residents[parasiteIndex].Id);
    }

    private sealed record ResidentDraft(
        Guid Id,
        string Name,
        int Age,
        ResidentPersonality Personality,
        IReadOnlyList<ResidentHabit> Habits,
        IReadOnlyList<ResidentTrait> Traits,
        string? ProfessionId = null)
    {
        public Resident ToResident()
        {
            var professionId = ProfessionId;
            if (string.IsNullOrWhiteSpace(professionId))
                throw new InvalidOperationException("Profession assignment was not applied to a resident draft.");

            return new Resident(Id, Name, Age, professionId, Personality, Habits, Traits);
        }
    }
}
