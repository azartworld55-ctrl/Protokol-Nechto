namespace Protocol.ProfessionConfig;

/// <summary>
/// One explicitly configured profession quota. No default quota distribution
/// is supplied by the library.
/// </summary>
public sealed record ProfessionQuota
{
    public ProfessionQuota(string professionId, int count)
    {
        if (string.IsNullOrWhiteSpace(professionId))
            throw new ArgumentException("Profession id is required.", nameof(professionId));

        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Profession quota must be positive.");

        ProfessionId = professionId.Trim();
        Count = count;
    }

    public string ProfessionId { get; }
    public int Count { get; }
}
