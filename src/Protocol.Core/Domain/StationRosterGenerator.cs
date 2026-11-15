namespace Protocol.Core.Domain;

/// <summary>
/// Deterministic roster generation: the same seed and profession pool produce
/// the same starting data, which makes simulation tests reproducible.
/// </summary>
public sealed class StationRosterGenerator
{
    private static readonly string[] Names =
    [
        "Алекс", "Мира", "Илья", "Ника", "Дан", "Ева",
        "Марк", "Лея", "Роман", "Соня", "Тимур", "Ася",
        "Лев", "Вера", "Кирилл", "Эмма", "Олег", "Лина",
        "Павел", "Яна", "Глеб", "Ирина", "Максим", "Зоя"
    ];

    private static readonly ResidentHabit[] AvailableHabits =
        Enum.GetValues<ResidentHabit>();

    public StationRoster Generate(int seed, IReadOnlyList<string> professions)
    {
        ArgumentNullException.ThrowIfNull(professions);

        if (professions.Count == 0 || professions.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("At least one non-empty profession is required.", nameof(professions));

        var random = new Random(seed);
        var shuffledNames = Names.OrderBy(_ => random.Next()).Take(StationRoster.RequiredResidentCount);
        var residents = new List<Resident>(StationRoster.RequiredResidentCount);

        foreach (var name in shuffledNames)
        {
            var personality = new ResidentPersonality(
                random.Next(0, 101),
                random.Next(0, 101),
                random.Next(0, 101),
                random.Next(0, 101),
                random.Next(0, 101));

            var habitCount = random.Next(1, 4);
            var habits = AvailableHabits
                .OrderBy(_ => random.Next())
                .Take(habitCount)
                .ToArray();

            residents.Add(new Resident(
                GuidFrom(random),
                name,
                random.Next(20, 66),
                professions[random.Next(professions.Count)],
                personality,
                habits));
        }

        var parasiteIndex = random.Next(residents.Count);
        var playerIndex = random.Next(residents.Count - 1);
        if (playerIndex >= parasiteIndex)
            playerIndex++;

        return new StationRoster(
            residents,
            residents[playerIndex].Id,
            residents[parasiteIndex].Id);
    }

    private static Guid GuidFrom(Random random)
    {
        var bytes = new byte[16];
        random.NextBytes(bytes);
        return new Guid(bytes);
    }
}
