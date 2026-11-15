namespace Protocol.Core.Domain;

/// <summary>
/// Stable personality dimensions. Values are bounded to 0..100 and are
/// tendencies, not deterministic rules for every NPC decision.
/// </summary>
public sealed record ResidentPersonality
{
    public ResidentPersonality(
        int curiosity,
        int sociability,
        int caution,
        int empathy,
        int discipline)
    {
        Curiosity = Validate(curiosity, nameof(curiosity));
        Sociability = Validate(sociability, nameof(sociability));
        Caution = Validate(caution, nameof(caution));
        Empathy = Validate(empathy, nameof(empathy));
        Discipline = Validate(discipline, nameof(discipline));
    }

    public int Curiosity { get; }
    public int Sociability { get; }
    public int Caution { get; }
    public int Empathy { get; }
    public int Discipline { get; }

    public static ResidentPersonality Neutral { get; } = new(50, 50, 50, 50, 50);

    private static int Validate(int value, string parameterName)
    {
        if (value is < 0 or > 100)
            throw new ArgumentOutOfRangeException(parameterName, "Personality values must be between 0 and 100.");

        return value;
    }
}

/// <summary>
/// Small behavioral preferences used later by the schedule and decision systems.
/// They do not force an NPC to act against urgent needs or danger.
/// </summary>
public enum ResidentHabit
{
    DrinksCoffee,
    ReadsBeforeSleep,
    KeepsWorkspaceTidy,
    EatsAtRegularHours,
    TakesEveningWalks,
    SocializesDuringBreaks,
    WorksLate,
    PrefersQuiet
}
