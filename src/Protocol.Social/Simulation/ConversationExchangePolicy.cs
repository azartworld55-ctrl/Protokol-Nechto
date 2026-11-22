namespace Protocol.Social.Simulation;

/// <summary>
/// Bounds the number of structured speaking turns in one explicit conversation.
/// This is a computational safety limit, not a gameplay balance default.
/// </summary>
public sealed record ConversationExchangePolicy
{
    public const int MaximumSupportedTurns = 32;

    public ConversationExchangePolicy(int maximumTurns)
    {
        if (maximumTurns is < 1 or > MaximumSupportedTurns)
            throw new ArgumentOutOfRangeException(nameof(maximumTurns));

        MaximumTurns = maximumTurns;
    }

    public int MaximumTurns { get; }
}
