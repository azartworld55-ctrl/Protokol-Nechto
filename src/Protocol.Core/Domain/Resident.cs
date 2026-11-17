namespace Protocol.Core.Domain;

/// <summary>
/// Immutable resident profile independent from RimWorld, Unity and Harmony.
/// </summary>
public sealed class Resident
{
    public Resident(
        Guid id,
        string name,
        int age,
        string professionId,
        ResidentPersonality? personality = null,
        IEnumerable<ResidentHabit>? habits = null,
        IEnumerable<ResidentTrait>? traits = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Resident id cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Resident name is required.", nameof(name));

        if (age is < 18 or > 120)
            throw new ArgumentOutOfRangeException(nameof(age), "Age must be between 18 and 120.");

        if (string.IsNullOrWhiteSpace(professionId))
            throw new ArgumentException("Profession id is required.", nameof(professionId));

        Id = id;
        Name = name.Trim();
        Age = age;
        ProfessionId = professionId.Trim();
        Personality = personality ?? ResidentPersonality.Neutral;
        Habits = Array.AsReadOnly(SnapshotUnique(habits ?? Array.Empty<ResidentHabit>(), nameof(habits)));
        Traits = Array.AsReadOnly(SnapshotUnique(traits ?? Array.Empty<ResidentTrait>(), nameof(traits)));
    }

    public Guid Id { get; }
    public string Name { get; }
    public int Age { get; }
    public string ProfessionId { get; }
    public ResidentPersonality Personality { get; }
    public IReadOnlyList<ResidentHabit> Habits { get; }
    public IReadOnlyList<ResidentTrait> Traits { get; }

    private static TEnum[] SnapshotUnique<TEnum>(
        IEnumerable<TEnum> values,
        string parameterName)
        where TEnum : struct, Enum
    {
        var snapshot = new List<TEnum>();
        var seen = new HashSet<TEnum>();

        foreach (var value in values)
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(parameterName, $"Unknown {typeof(TEnum).Name} value.");

            if (!seen.Add(value))
                throw new ArgumentException($"{typeof(TEnum).Name} values must be unique.", parameterName);

            snapshot.Add(value);
        }

        return snapshot.ToArray();
    }
}
