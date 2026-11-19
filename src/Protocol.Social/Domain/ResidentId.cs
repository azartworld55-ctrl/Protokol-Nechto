namespace Protocol.Social.Domain;

/// <summary>
/// Stable identity used by the social domain without depending on game objects.
/// </summary>
public sealed record ResidentId
{
    public ResidentId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Resident id cannot be empty.", nameof(value));

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString("N");
}
