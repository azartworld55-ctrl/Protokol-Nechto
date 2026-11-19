namespace Protocol.Social.Domain;

/// <summary>
/// One shared relationship state for a resident pair. Trust and familiarity are
/// intentionally independent scales in the inclusive range 0..100.
/// </summary>
public sealed record RelationshipState
{
    public RelationshipState(int trust, int familiarity)
    {
        if (trust is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(trust), "Trust must be between 0 and 100.");

        if (familiarity is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(familiarity), "Familiarity must be between 0 and 100.");

        Trust = trust;
        Familiarity = familiarity;
    }

    public int Trust { get; }

    public int Familiarity { get; }
}
