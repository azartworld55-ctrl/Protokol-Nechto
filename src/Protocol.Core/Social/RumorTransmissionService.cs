namespace Protocol.Core.Social;

/// <summary>
/// Transfers one known memory between two residents after an explicit social
/// contact. The service never broadcasts to the whole station.
/// </summary>
public sealed class RumorTransmissionService
{
    public bool TryShare(
        MemoryJournal source,
        MemoryJournal recipient,
        string eventCode,
        long receivedAtTick,
        RelationshipState relationship,
        bool hasFaceToFaceContact,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(random);

        if (string.IsNullOrWhiteSpace(eventCode))
            throw new ArgumentException("Event code is required.", nameof(eventCode));

        if (receivedAtTick < 0)
            throw new ArgumentOutOfRangeException(nameof(receivedAtTick));

        if (relationship.Trust is < -100 or > 100)
            throw new ArgumentOutOfRangeException(nameof(relationship), "Trust must be between -100 and 100.");

        if (relationship.Familiarity is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(relationship), "Familiarity must be between 0 and 100.");

        if (!hasFaceToFaceContact || recipient.HasHeard(eventCode))
            return false;

        var sourceMemory = source.Entries
            .Where(entry => string.Equals(entry.EventCode, eventCode, StringComparison.Ordinal))
            .OrderByDescending(entry => entry.Confidence)
            .ThenByDescending(entry => entry.Tick)
            .FirstOrDefault();

        if (sourceMemory is null)
            return false;

        var transmissionChance = Math.Clamp(
            0.35 + relationship.Trust / 250.0 + relationship.Familiarity / 500.0,
            0.05,
            0.95);

        if (random.NextDouble() >= transmissionChance)
            return false;

        var reliability = Math.Clamp(0.65 + relationship.Trust / 400.0, 0.2, 0.9);
        recipient.Hear(sourceMemory, receivedAtTick, reliability);
        return true;
    }
}
