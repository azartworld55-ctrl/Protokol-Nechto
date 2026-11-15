namespace Protocol.Core.Domain;

/// <summary>
/// A station resident. This type deliberately has no RimWorld references,
/// so its rules can be tested before the game adapter is written.
/// </summary>
public sealed class Resident
{
    public Resident(Guid id, string name, int age, string profession)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Resident id cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Resident name is required.", nameof(name));

        if (age is < 18 or > 120)
            throw new ArgumentOutOfRangeException(nameof(age), "Age must be between 18 and 120.");

        if (string.IsNullOrWhiteSpace(profession))
            throw new ArgumentException("Profession is required.", nameof(profession));

        Id = id;
        Name = name.Trim();
        Age = age;
        Profession = profession.Trim();
    }

    public Guid Id { get; }
    public string Name { get; }
    public int Age { get; }
    public string Profession { get; }
}
