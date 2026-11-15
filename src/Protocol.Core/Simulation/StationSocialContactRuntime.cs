namespace Protocol.Core.Simulation;

public sealed record ProcessedSocialContact(
    SocialContact Contact,
    SocialContactResult Result);

/// <summary>
/// Connects map-derived physical encounters to the social memory processor.
/// Call once per chosen social simulation step, not on every render frame.
/// </summary>
public sealed class StationSocialContactRuntime
{
    private readonly StationPerceptionService _perception;
    private readonly SocialContactProcessor _processor;

    public StationSocialContactRuntime(
        StationPerceptionService? perception = null,
        SocialContactProcessor? processor = null)
    {
        _perception = perception ?? new StationPerceptionService();
        _processor = processor ?? new SocialContactProcessor();
    }

    public IReadOnlyList<ProcessedSocialContact> ProcessMapContacts(
        StationMapSnapshot map,
        IReadOnlyDictionary<Guid, ResidentMind> minds,
        RelationshipGraph relationships,
        long tick,
        Random random,
        int conversationRadius = 3)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(minds);
        ArgumentNullException.ThrowIfNull(relationships);
        ArgumentNullException.ThrowIfNull(random);

        var contacts = _perception.DetectSocialContacts(map, tick, conversationRadius);
        var results = new List<ProcessedSocialContact>(contacts.Count);
        foreach (var contact in contacts)
        {
            if (!minds.TryGetValue(contact.FirstResidentId, out var first) ||
                !minds.TryGetValue(contact.SecondResidentId, out var second))
                continue;

            var result = _processor.Process(
                contact,
                first,
                second,
                relationships,
                random);
            results.Add(new ProcessedSocialContact(contact, result));
        }

        return results;
    }
}
