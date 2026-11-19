using Protocol.Social.Domain;
using Protocol.Social.Memory;
using Protocol.Social.Simulation;

namespace Protocol.Social.Integration;

/// <summary>
/// A resident-scoped capability over station social state. It exposes only
/// this resident's memories while allowing pairwise relationship operations.
/// </summary>
public sealed class ResidentSocialView
{
    private readonly StationSocialState _state;

    internal ResidentSocialView(StationSocialState state, ResidentId residentId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(residentId);
        _state = state;
        ResidentId = residentId;
    }

    public ResidentId ResidentId { get; }

    public IReadOnlyList<MemoryEntry> Memories => _state.GetMemories(ResidentId);

    public bool RecordObservation(DirectObservation observation) =>
        _state.RecordObservation(ResidentId, observation);

    public RelationshipState GetRelationshipWith(ResidentId otherResidentId) =>
        _state.GetRelationship(ResidentId, otherResidentId);

    public RelationshipState AdjustRelationshipWith(
        ResidentId otherResidentId,
        long trustDelta,
        long familiarityDelta) =>
        _state.AdjustRelationship(ResidentId, otherResidentId, trustDelta, familiarityDelta);

    public RumorShareResult TryRetellTo(
        ResidentId recipientId,
        ConversationEvent conversation,
        Guid eventId,
        RumorTransmissionPolicy policy) =>
        _state.TryRetell(ResidentId, recipientId, conversation, eventId, policy);
}
