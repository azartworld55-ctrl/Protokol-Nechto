using Protocol.Core.Domain;
using Protocol.Social.Domain;
using Protocol.Social.Memory;
using Protocol.Social.Simulation;

namespace Protocol.Social.Integration;

/// <summary>
/// Owns the social state for one generated station roster. Journals stay private
/// to this aggregate; callers receive resident-scoped views instead of a global
/// collection of every resident's memories.
/// </summary>
public sealed class StationSocialState
{
    private readonly Dictionary<ResidentId, MemoryJournal> _journals;
    private readonly RelationshipLedger _relationships = new();
    private readonly RumorTransmissionService _rumorTransmission = new();

    public StationSocialState(StationRoster roster)
    {
        ArgumentNullException.ThrowIfNull(roster);

        _journals = new Dictionary<ResidentId, MemoryJournal>(roster.Residents.Count);
        foreach (var resident in roster.Residents)
        {
            var residentId = new ResidentId(resident.Id);
            _journals.Add(residentId, new MemoryJournal(residentId));
        }
    }

    public int ResidentCount => _journals.Count;

    /// <summary>
    /// Creates a view for one member of this roster. The caller should retain
    /// only the view belonging to the resident it is currently simulating.
    /// </summary>
    public ResidentSocialView ForResident(ResidentId residentId)
    {
        EnsureResident(residentId);
        return new ResidentSocialView(this, residentId);
    }

    internal IReadOnlyList<MemoryEntry> GetMemories(ResidentId ownerId) =>
        GetJournal(ownerId).Entries;

    internal bool RecordObservation(ResidentId ownerId, DirectObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        EnsureResident(ownerId);
        EnsureResident(observation.ObserverId);
        EnsureResident(observation.SubjectId);

        if (observation.ObserverId != ownerId)
        {
            throw new ArgumentException(
                "A resident view can record only observations made by its owner.",
                nameof(observation));
        }

        return GetJournal(ownerId).RecordObservation(observation);
    }

    internal RelationshipState GetRelationship(ResidentId observerId, ResidentId targetId)
    {
        EnsureResident(observerId);
        EnsureResident(targetId);
        return _relationships.Get(observerId, targetId);
    }

    internal RelationshipState AdjustRelationship(
        ResidentId observerId,
        ResidentId targetId,
        long trustDelta,
        long familiarityDelta)
    {
        EnsureResident(observerId);
        EnsureResident(targetId);
        return _relationships.Adjust(observerId, targetId, trustDelta, familiarityDelta);
    }

    internal RumorShareResult TryRetell(
        ResidentId senderId,
        ResidentId recipientId,
        ConversationEvent conversation,
        Guid eventId,
        RumorTransmissionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(policy);

        EnsureResident(senderId);
        EnsureResident(recipientId);
        EnsureResident(conversation.ParticipantA);
        EnsureResident(conversation.ParticipantB);

        return _rumorTransmission.TryRetell(
            conversation,
            senderId,
            recipientId,
            eventId,
            GetJournal(senderId),
            GetJournal(recipientId),
            _relationships,
            policy);
    }

    private MemoryJournal GetJournal(ResidentId ownerId)
    {
        EnsureResident(ownerId);
        return _journals[ownerId];
    }

    private void EnsureResident(ResidentId residentId)
    {
        ArgumentNullException.ThrowIfNull(residentId);
        if (!_journals.ContainsKey(residentId))
            throw new ArgumentException("Resident does not belong to this station roster.", nameof(residentId));
    }
}
