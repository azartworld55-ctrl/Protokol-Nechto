namespace Protocol.Core.Simulation;

/// <summary>
/// Normalized needs. Values are 0..100; higher values mean a stronger need
/// for the corresponding activity.
/// </summary>
public sealed class ResidentNeeds
{
    public ResidentNeeds(int hunger = 20, int fatigue = 20, int social = 20, int stress = 10)
    {
        Hunger = Validate(hunger, nameof(hunger));
        Fatigue = Validate(fatigue, nameof(fatigue));
        Social = Validate(social, nameof(social));
        Stress = Validate(stress, nameof(stress));
    }

    public int Hunger { get; private set; }
    public int Fatigue { get; private set; }
    public int Social { get; private set; }
    public int Stress { get; private set; }

    public void Advance(int elapsedHours = 1)
    {
        if (elapsedHours < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedHours));

        Hunger = Clamp(Hunger + 5 * elapsedHours);
        Fatigue = Clamp(Fatigue + 4 * elapsedHours);
        Social = Clamp(Social + 2 * elapsedHours);
        Stress = Clamp(Stress + elapsedHours);
    }

    public void Satisfy(ResidentAction action)
    {
        switch (action)
        {
            case ResidentAction.Eat:
                Hunger = Clamp(Hunger - 65);
                break;
            case ResidentAction.Rest:
            case ResidentAction.Sleep:
                Fatigue = Clamp(Fatigue - (action == ResidentAction.Sleep ? 75 : 35));
                Stress = Clamp(Stress - 10);
                break;
            case ResidentAction.Socialize:
                Social = Clamp(Social - 45);
                Stress = Clamp(Stress - 8);
                break;
            case ResidentAction.Recreation:
                Stress = Clamp(Stress - 30);
                Fatigue = Clamp(Fatigue - 5);
                break;
            case ResidentAction.Work:
            case ResidentAction.Inspect:
            case ResidentAction.SeekQuiet:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    private static int Validate(int value, string name) =>
        value is >= 0 and <= 100
            ? value
            : throw new ArgumentOutOfRangeException(name, "Need values must be between 0 and 100.");

    private static int Clamp(int value) => Math.Clamp(value, 0, 100);
}
