using Protocol.Core.Domain;
using Protocol.ProfessionConfig;
using Protocol.Social.Domain;
using Protocol.Social.Integration;
using Protocol.Social.Memory;
using Protocol.Social.Simulation;

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException($"FAILED: {message}");
}

static void Expect<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"FAILED: {message}");
}

var configuration = new ProfessionQuotaConfiguration(
[
    new ProfessionQuota("integration-alpha", 4),
    new ProfessionQuota("integration-beta", 4),
    new ProfessionQuota("integration-gamma", 4)
]);

const long seed = 20261120;
var generator = new StationRosterGenerator();
var roster = generator.Generate(seed, configuration);
var repeatedRoster = generator.Generate(seed, configuration);

Check(roster.Residents.Count == StationRoster.RequiredResidentCount,
    "the integrated station roster contains exactly twelve residents");
Check(roster.Residents.Select(resident => resident.Id)
        .SequenceEqual(repeatedRoster.Residents.Select(resident => resident.Id)),
    "resident identities remain stable for the same seed and quota configuration");
Check(roster.Residents.Select(resident => resident.ProfessionId)
        .SequenceEqual(repeatedRoster.Residents.Select(resident => resident.ProfessionId)),
    "profession assignments remain stable for the same seed and quota configuration");

foreach (var quota in configuration.Quotas)
{
    Check(roster.Residents.Count(resident => resident.ProfessionId == quota.ProfessionId) == quota.Count,
        $"the integrated roster respects quota {quota.ProfessionId}");
}

var stationSocial = new StationSocialState(roster);
Check(stationSocial.ResidentCount == StationRoster.RequiredResidentCount,
    "the social aggregate creates one private journal for each of the twelve residents");

var residentIds = roster.Residents
    .Select(resident => new ResidentId(resident.Id))
    .ToArray();
Check(residentIds.Select(id => id.Value).Distinct().Count() == 12,
    "Core resident GUIDs map one-to-one to social ResidentId values");

var views = residentIds.Select(stationSocial.ForResident).ToArray();
Check(views.Length == 12 &&
      views.Select(view => view.ResidentId.Value).SequenceEqual(roster.Residents.Select(resident => resident.Id)),
    "every resident receives a stable view matching the corresponding Core resident");
Check(views.All(view => view.Memories.Count == 0),
    "all twelve personal journals begin empty and isolated");

var alice = views[0];
var bob = views[1];
var carol = views[2];
var dave = views[3];
var subject = views[4];

alice.AdjustRelationshipWith(bob.ResidentId, trustDelta: 80, familiarityDelta: 70);
bob.AdjustRelationshipWith(carol.ResidentId, trustDelta: 80, familiarityDelta: 70);
Check(alice.GetRelationshipWith(bob.ResidentId) == bob.GetRelationshipWith(alice.ResidentId),
    "the integrated relationship state is symmetric for Alice and Bob");
Check(bob.GetRelationshipWith(carol.ResidentId) == carol.GetRelationshipWith(bob.ResidentId),
    "the integrated relationship state is symmetric for Bob and Carol");

var eventId = Guid.Parse("30000000-0000-0000-0000-000000000020");
var observation = new DirectObservation(
    eventId,
    alice.ResidentId,
    subject.ResidentId,
    eventTick: 10,
    observedAtTick: 10,
    description: "Alice directly observed a station event.");

Check(alice.RecordObservation(observation),
    "the observer can record an event in the journal scoped to that resident");
Check(!alice.RecordObservation(observation),
    "the same event is deduplicated in the observer's journal");
var aliceSnapshot = alice.Memories;
Check(aliceSnapshot.Count == 1 && alice.Memories.Count == 1,
    "a resident view exposes its own cached memory snapshot");
Check(views.Skip(1).All(view => view.Memories.Count == 0),
    "recording an observation does not reveal it to the other eleven residents");

Expect<ArgumentException>(
    () => bob.RecordObservation(observation),
    "a resident view cannot write an observation owned by another resident");

var policy = new RumorTransmissionPolicy(
    minimumTrust: 50,
    minimumFamiliarity: 40,
    confidenceDecayPermille: 750,
    minimumConfidencePermille: 100,
    maximumRetellCount: 2);

var aliceBobConversation = new ConversationEvent(
    Guid.Parse("40000000-0000-0000-0000-000000000020"),
    alice.ResidentId,
    bob.ResidentId,
    tick: 12);
var firstShare = alice.TryRetellTo(bob.ResidentId, aliceBobConversation, eventId, policy);
Check(firstShare.WasShared && firstShare.Entry is not null,
    "a known event can be shared through an explicit conversation between roster residents");
Check(firstShare.Entry!.ConfidencePermille == 750 &&
      firstShare.Entry.RetellCount == 1 &&
      firstShare.Entry.OriginalObserverId == alice.ResidentId &&
      firstShare.Entry.LastInformantId == alice.ResidentId,
    "the first integrated retelling preserves provenance and applies configured decay");

var bobCarolConversation = new ConversationEvent(
    Guid.Parse("40000000-0000-0000-0000-000000000021"),
    bob.ResidentId,
    carol.ResidentId,
    tick: 14);
var secondShare = bob.TryRetellTo(carol.ResidentId, bobCarolConversation, eventId, policy);
Check(secondShare.WasShared && secondShare.Entry is not null,
    "a resident who heard a rumor can pass it on during a later explicit conversation");
Check(secondShare.Entry!.ConfidencePermille == 562 &&
      secondShare.Entry.RetellCount == 2 &&
      secondShare.Entry.OriginalObserverId == alice.ResidentId &&
      secondShare.Entry.LastInformantId == bob.ResidentId,
    "the second integrated retelling compounds decay and preserves the original observer");

Check(alice.Memories.Single(entry => entry.EventId == eventId).Kind == MemoryKind.DirectObservation,
    "sharing a rumor does not rewrite the original observer's direct memory");
Check(bob.Memories.Single(entry => entry.EventId == eventId).Kind == MemoryKind.HeardRumor,
    "the first recipient stores the event as a rumor");
Check(carol.Memories.Single(entry => entry.EventId == eventId).ConfidencePermille == 562,
    "the second recipient stores only the decayed rumor");
Check(dave.Memories.Count == 0 &&
      views.Skip(3).All(view => view.Memories.Count == 0),
    "residents outside the two conversations do not receive the event automatically");
Check(aliceSnapshot.Count == 1,
    "previously returned resident memory snapshots remain unchanged");

var outsider = new ResidentId(Guid.NewGuid());
Expect<ArgumentException>(
    () => stationSocial.ForResident(outsider),
    "a resident outside the generated roster cannot obtain a social view");
Expect<ArgumentException>(
    () => alice.GetRelationshipWith(outsider),
    "relationships cannot be created for residents outside the station roster");
Expect<ArgumentException>(
    () => alice.RecordObservation(new DirectObservation(
        Guid.Parse("30000000-0000-0000-0000-000000000021"),
        alice.ResidentId,
        outsider,
        eventTick: 20,
        observedAtTick: 20,
        description: "An event with an unknown subject.")),
    "an observation cannot introduce an unknown resident into the station");

var outsiderConversation = new ConversationEvent(
    Guid.Parse("40000000-0000-0000-0000-000000000022"),
    alice.ResidentId,
    outsider,
    tick: 15);
Expect<ArgumentException>(
    () => alice.TryRetellTo(bob.ResidentId, outsiderConversation, eventId, policy),
    "a conversation cannot introduce a participant outside the generated roster");

Console.WriteLine("All station-social integration checks passed.");
