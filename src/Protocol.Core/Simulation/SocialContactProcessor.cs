using Protocol.Core.Social;

namespace Protocol.Core.Simulation;

/// <summary>
/// A face-to-face contact reported by the game/map adapter. The core trusts
/// this input only as a claim that the adapter already checked physical
/// reachability; it does not infer visibility or pathing itself.
/// </summary>
public sealed record SocialContact
{
    public SocialContact(Guid firstResidentId, Guid secondResidentId, long tick)
    {
        if (firstResidentId == Guid.Empty || secondResidentId == Guid.Empty)
            throw new ArgumentException("Contact participants must have valid ids.");
        if (firstResidentId == secondResidentId)
            throw new ArgumentException("A resident cannot contact themselves.");
        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));

        FirstResidentId = firstResidentId;
        SecondResidentId = secondResidentId;
        Tick = tick;
    }

    public Guid FirstResidentId { get; }
    public Guid SecondResidentId { get; }
    public long Tick { get; }
}

public sealed record SocialContactResult(
    RelationshipState Relationship,
    bool FirstSharedWithSecond,
    bool SecondSharedWithFirst);

/// <summary>
/// Processes one already-validated face-to-face encounter. It updates
/// familiarity and may pass one personally known memory in either direction.
/// It never scans the station or creates contacts on its own.
/// </summary>
public sealed class SocialContactProcessor
{
    private readonly RumorTransmissionService _rumorTransmission;

    public SocialContactProcessor(RumorTransmissionService? rumorTransmission = null)
    {
        _rumorTransmission = rumorTransmission ?? new RumorTransmissionService();
    }

    public SocialContactResult Process(
        SocialContact contact,
        ResidentMind first,
        ResidentMind second,
        RelationshipGraph relationships,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(relationships);
        ArgumentNullException.ThrowIfNull(random);

        if (first.Resident.Id != contact.FirstResidentId ||
            second.Resident.Id != contact.SecondResidentId)
            throw new ArgumentException("Contact participant ids do not match the supplied residents.");

        var relationship = relationships.AdjustFamiliarity(
            contact.FirstResidentId,
            contact.SecondResidentId,
            1);

        var firstShared = TryShareMostRecent(
            first,
            second,
            relationship,
            contact.Tick,
            random);
        var secondShared = TryShareMostRecent(
            second,
            first,
            relationships.Get(contact.FirstResidentId, contact.SecondResidentId),
            contact.Tick,
            random);

        return new SocialContactResult(
            relationships.Get(contact.FirstResidentId, contact.SecondResidentId),
            firstShared,
            secondShared);
    }

    private bool TryShareMostRecent(
        ResidentMind source,
        ResidentMind recipient,
        RelationshipState relationship,
        long tick,
        Random random)
    {
        var candidate = source.Memories.Entries
            .Where(entry => !recipient.Memories.HasHeard(entry.EventCode))
            .OrderByDescending(entry => entry.Tick)
            .ThenByDescending(entry => entry.Confidence)
            .FirstOrDefault();

        return candidate is not null && _rumorTransmission.TryShare(
            source.Memories,
            recipient.Memories,
            candidate.EventCode,
            tick,
            relationship,
            hasFaceToFaceContact: true,
            random);
    }
}
