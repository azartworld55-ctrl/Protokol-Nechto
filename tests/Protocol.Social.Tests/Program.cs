using System.Collections.Generic;
using Protocol.Social.Domain;
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

static ResidentId Id(string value) => new(Guid.Parse(value));
static Guid EventId(string value) => Guid.Parse(value);

var alice = Id("10000000-0000-0000-0000-000000000001");
var bob = Id("10000000-0000-0000-0000-000000000002");
var carol = Id("10000000-0000-0000-0000-000000000003");
var dave = Id("10000000-0000-0000-0000-000000000004");
var subject = Id("20000000-0000-0000-0000-000000000001");

Expect<ArgumentException>(
    () => new ResidentId(Guid.Empty),
    "empty resident ids are rejected");

var relationships = new RelationshipLedger();
var unknown = relationships.Get(alice, bob);
Check(unknown.Trust == 0 && unknown.Familiarity == 0,
    "unknown relationships start with independent zero scales");

var aliceToBob = relationships.Adjust(alice, bob, 65, 20);
Check(aliceToBob.Trust == 65 && aliceToBob.Familiarity == 20,
    "trust and familiarity are stored as separate axes");
Check(relationships.Get(alice, bob) == relationships.Get(bob, alice),
    "trust and familiarity are symmetric for both residents");
var bobToAlice = relationships.Adjust(bob, alice, 10, 80);
Check(bobToAlice.Trust == 75 && bobToAlice.Familiarity == 100,
    "adjusting the pair from the reverse order updates the shared relationship");
Check(relationships.Get(alice, bob) == relationships.Get(bob, alice),
    "reverse lookup remains identical after an adjustment");

var saturatedHigh = relationships.Adjust(alice, bob, long.MaxValue, long.MaxValue);
Check(saturatedHigh.Trust == 100 && saturatedHigh.Familiarity == 100,
    "positive extreme deltas saturate without Int64 overflow");
var saturatedLow = relationships.Adjust(alice, bob, long.MinValue, long.MinValue);
Check(saturatedLow.Trust == 0 && saturatedLow.Familiarity == 0,
    "negative extreme deltas saturate without Int64 overflow");

Expect<ArgumentException>(
    () => relationships.Get(alice, alice),
    "self-relationships are rejected");
Expect<ArgumentOutOfRangeException>(
    () => new RelationshipState(101, 0),
    "trust cannot exceed its scale");
Expect<ArgumentOutOfRangeException>(
    () => new RelationshipState(0, -1),
    "familiarity cannot fall below its scale");

var eventId = EventId("30000000-0000-0000-0000-000000000001");
var secondEventId = EventId("30000000-0000-0000-0000-000000000002");
var aliceJournal = new MemoryJournal(alice);
var observation = new DirectObservation(
    eventId,
    alice,
    subject,
    tick: 5,
    description: "Alice directly observed the station door being opened.");

Check(aliceJournal.RecordObservation(observation),
    "the observer can record a direct observation in their own journal");
Check(!aliceJournal.RecordObservation(observation),
    "duplicate event ids are not counted twice");

var immutableSnapshot = aliceJournal.Entries;
Check(object.ReferenceEquals(immutableSnapshot, aliceJournal.Entries),
    "unchanged journals reuse their cached immutable snapshot");
var secondObservation = new DirectObservation(
    secondEventId,
    alice,
    subject,
    tick: 6,
    description: "Alice directly observed a second event.");
Check(aliceJournal.RecordObservation(secondObservation),
    "a separate event can be recorded");
Check(immutableSnapshot.Count == 1,
    "previously returned memory snapshots do not change after later writes");
Check(!object.ReferenceEquals(immutableSnapshot, aliceJournal.Entries),
    "a journal write invalidates only the current snapshot cache");

var readOnlyEntries = (IList<MemoryEntry>)immutableSnapshot;
Check(readOnlyEntries.IsReadOnly,
    "memory snapshots cannot be modified through IList");
Expect<NotSupportedException>(
    () => readOnlyEntries.Clear(),
    "memory snapshots reject mutation");

Expect<ArgumentException>(
    () => new MemoryJournal(bob).RecordObservation(observation),
    "one resident cannot record another resident's direct observation");

var policy = new RumorTransmissionPolicy(
    minimumTrust: 50,
    minimumFamiliarity: 40,
    confidenceDecayPermille: 750,
    minimumConfidencePermille: 100,
    maximumRetellCount: 2);

relationships.Adjust(alice, bob, 70, 60);
relationships.Adjust(bob, carol, 70, 60);
relationships.Adjust(carol, dave, 70, 60);

var bobJournal = new MemoryJournal(bob);
var carolJournal = new MemoryJournal(carol);
var daveJournal = new MemoryJournal(dave);
var rumorService = new RumorTransmissionService();

var invalidConversation = new ConversationEvent(
    EventId("40000000-0000-0000-0000-000000000001"), alice, carol, tick: 6);
var invalidShare = rumorService.TryRetell(
    invalidConversation, alice, bob, eventId,
    aliceJournal, bobJournal, relationships, policy);
Check(invalidShare.Status == RumorShareStatus.InvalidConversationParticipants,
    "an unrelated conversation cannot authorize a transfer");

var weakRelationships = new RelationshipLedger();
weakRelationships.Adjust(alice, bob, trustDelta: 49, familiarityDelta: 100);
var weakConversation = new ConversationEvent(
    EventId("40000000-0000-0000-0000-000000000002"), alice, bob, tick: 7);
var weakShare = rumorService.TryRetell(
    weakConversation, alice, bob, eventId,
    aliceJournal, bobJournal, weakRelationships, policy);
Check(weakShare.Status == RumorShareStatus.RelationshipBelowThreshold,
    "rumor transfer respects the sender's configured trust and familiarity thresholds");

var earlyConversation = new ConversationEvent(
    EventId("40000000-0000-0000-0000-000000000003"), alice, bob, tick: 4);
var earlyShare = rumorService.TryRetell(
    earlyConversation, alice, bob, eventId,
    aliceJournal, bobJournal, relationships, policy);
Check(earlyShare.Status == RumorShareStatus.ConversationBeforeKnowledge,
    "a resident cannot share information before learning it");

var strictConfidencePolicy = new RumorTransmissionPolicy(0, 0, 750, 800, 2);
var lowConfidenceConversation = new ConversationEvent(
    EventId("40000000-0000-0000-0000-000000000004"), alice, bob, tick: 7);
var lowConfidenceShare = rumorService.TryRetell(
    lowConfidenceConversation, alice, bob, eventId,
    aliceJournal, bobJournal, relationships, strictConfidencePolicy);
Check(lowConfidenceShare.Status == RumorShareStatus.ConfidenceBelowThreshold,
    "a rumor below the configured confidence threshold is not transmitted");

Check(bobJournal.Entries.Count == 0 && carolJournal.Entries.Count == 0,
    "residents do not learn an event merely because they are nearby");

var firstConversationId = EventId("40000000-0000-0000-0000-000000000005");
var firstConversation = new ConversationEvent(firstConversationId, alice, bob, tick: 7);
var firstShare = rumorService.TryRetell(
    firstConversation, alice, bob, eventId,
    aliceJournal, bobJournal, relationships, policy);
Check(firstShare.WasShared && firstShare.Entry is not null,
    "an explicit conversation can transfer a known event");
Check(firstShare.Entry!.Kind == MemoryKind.HeardRumor &&
      firstShare.Entry.ConfidencePermille == 750 &&
      firstShare.Entry.RetellCount == 1,
    "the first retelling applies the configured confidence decay");
Check(firstShare.Entry.OriginalObserverId == alice &&
      firstShare.Entry.LastInformantId == alice &&
      firstShare.Entry.EventTick == 5 &&
      firstShare.Entry.LearnedAtTick == 7 &&
      firstShare.Entry.SourceConversationId == firstConversationId,
    "a rumor preserves its original observer and records its latest source");

var mismatchedSenderJournal = new MemoryJournal(carol);
var mismatchedSenderShare = rumorService.TryRetell(
    firstConversation, alice, bob, eventId,
    mismatchedSenderJournal, new MemoryJournal(bob), relationships, policy);
Check(mismatchedSenderShare.Status == RumorShareStatus.InvalidConversationParticipants,
    "a sender cannot supply another resident's journal");

var mismatchedRecipientJournal = new MemoryJournal(dave);
var mismatchedRecipientShare = rumorService.TryRetell(
    firstConversation, alice, bob, eventId,
    aliceJournal, mismatchedRecipientJournal, relationships, policy);
Check(mismatchedRecipientShare.Status == RumorShareStatus.InvalidConversationParticipants &&
      mismatchedRecipientJournal.Entries.Count == 0,
    "a recipient cannot receive a rumor into another resident's journal");

Expect<ArgumentException>(
    () => rumorService.TryRetell(
        firstConversation, alice, bob, Guid.Empty, aliceJournal,
        new MemoryJournal(bob), relationships, policy),
    "empty event ids are rejected before rumor transmission");
Check(aliceJournal.Entries.Single(entry => entry.EventId == eventId).Kind ==
      MemoryKind.DirectObservation,
    "sharing does not erase or rewrite the original observation");
Check(carolJournal.Entries.Count == 0,
    "a rumor is not broadcast to residents outside the conversation");

var repeatedShare = rumorService.TryRetell(
    new ConversationEvent(EventId("40000000-0000-0000-0000-000000000006"), alice, bob, tick: 8),
    alice, bob, eventId,
    aliceJournal, bobJournal, relationships, policy);
Check(repeatedShare.Status == RumorShareStatus.RecipientAlreadyKnowsEvent,
    "the same event is not duplicated in one resident's memory");

var secondConversationId = EventId("40000000-0000-0000-0000-000000000007");
var secondShare = rumorService.TryRetell(
    new ConversationEvent(secondConversationId, bob, carol, tick: 9),
    bob, carol, eventId,
    bobJournal, carolJournal, relationships, policy);
Check(secondShare.WasShared && secondShare.Entry is not null,
    "a resident who heard a rumor can retell it during a later conversation");
Check(secondShare.Entry!.ConfidencePermille == 562 &&
      secondShare.Entry.RetellCount == 2 &&
      secondShare.Entry.OriginalObserverId == alice &&
      secondShare.Entry.LastInformantId == bob,
    "each retelling compounds confidence decay and preserves provenance");

var exhaustedShare = rumorService.TryRetell(
    new ConversationEvent(EventId("40000000-0000-0000-0000-000000000008"), carol, dave, tick: 10),
    carol, dave, eventId,
    carolJournal, daveJournal, relationships, policy);
Check(exhaustedShare.Status == RumorShareStatus.RetellLimitReached,
    "the configured maximum retell count is enforced");

var unknownShare = rumorService.TryRetell(
    new ConversationEvent(EventId("40000000-0000-0000-0000-000000000009"), alice, dave, tick: 10),
    alice, dave, EventId("30000000-0000-0000-0000-000000000099"),
    aliceJournal, daveJournal, relationships, policy);
Check(unknownShare.Status == RumorShareStatus.SenderDoesNotKnowEvent,
    "a resident cannot share an event absent from their own journal");

var confirmationEventId = EventId("30000000-0000-0000-0000-000000000003");
var confirmationSource = new DirectObservation(
    confirmationEventId,
    alice,
    subject,
    eventTick: 12,
    observedAtTick: 12,
    description: "Alice directly observed a third event.");
Check(aliceJournal.RecordObservation(confirmationSource),
    "a new event can be recorded before it is shared");

var confirmationRecipientJournal = new MemoryJournal(bob);
var confirmationConversationId = EventId("40000000-0000-0000-0000-000000000012");
var confirmationShare = rumorService.TryRetell(
    new ConversationEvent(confirmationConversationId, alice, bob, tick: 13),
    alice, bob, confirmationEventId,
    aliceJournal, confirmationRecipientJournal, relationships, policy);
Check(confirmationShare.WasShared,
    "a separate event can be shared explicitly before direct confirmation");

var rumorSnapshot = confirmationRecipientJournal.Entries;
Check(rumorSnapshot.Single(entry => entry.EventId == confirmationEventId).Kind == MemoryKind.HeardRumor,
    "the recipient initially stores the event as a rumor");

Expect<ArgumentOutOfRangeException>(
    () => confirmationRecipientJournal.RecordObservation(new DirectObservation(
        confirmationEventId, bob, subject, eventTick: 12, observedAtTick: 12,
        description: "A confirmation that predates the rumor.")),
    "direct confirmation cannot be recorded before the resident learned the rumor");

Expect<ArgumentOutOfRangeException>(
    () => new DirectObservation(
        EventId("30000000-0000-0000-0000-000000000004"),
        bob, subject, eventTick: 15, observedAtTick: 14,
        description: "An observation cannot predate its event"),
    "observation time cannot precede event time");

var directConfirmation = new DirectObservation(
    confirmationEventId,
    bob,
    subject,
    eventTick: 12,
    observedAtTick: 14,
    description: "Bob independently confirmed the same event.");
Check(confirmationRecipientJournal.RecordObservation(directConfirmation),
    "a later direct observation upgrades an existing rumor");
var confirmedEntry = confirmationRecipientJournal.Entries.Single(entry => entry.EventId == confirmationEventId);
Check(confirmedEntry.Kind == MemoryKind.DirectObservation &&
      confirmedEntry.OriginalObserverId == bob &&
      confirmedEntry.LastInformantId == bob &&
      confirmedEntry.ConfidencePermille == 1000 &&
      confirmedEntry.RetellCount == 0 &&
      confirmedEntry.SourceConversationId is null &&
      confirmedEntry.EventTick == 12 &&
      confirmedEntry.LearnedAtTick == 14,
    "direct confirmation replaces rumor provenance while preserving event time");
Check(rumorSnapshot.Single(entry => entry.EventId == confirmationEventId).Kind == MemoryKind.HeardRumor,
    "a previously returned rumor snapshot remains unchanged after confirmation");
Check(!confirmationRecipientJournal.RecordObservation(directConfirmation),
    "repeating an already recorded direct observation is idempotent");
Expect<ArgumentException>(
    () => confirmationRecipientJournal.RecordObservation(new DirectObservation(
        confirmationEventId, bob, subject, eventTick: 11, observedAtTick: 15,
        description: "Conflicting event time.")),
    "an event id cannot be reused for a different event tick");

Expect<ArgumentException>(
    () => confirmationRecipientJournal.RecordObservation(new DirectObservation(
        confirmationEventId, bob, carol, eventTick: 12, observedAtTick: 15,
        description: "A different subject cannot reuse this event id.")),
    "an event id cannot be reused for a different subject");

Expect<ArgumentException>(
    () => new ConversationEvent(Guid.Empty, alice, bob, 1),
    "empty conversation ids are rejected");
Expect<ArgumentException>(
    () => new ConversationEvent(EventId("40000000-0000-0000-0000-000000000010"), alice, alice, 1),
    "a conversation cannot contain the same participant twice");
Expect<ArgumentOutOfRangeException>(
    () => new ConversationEvent(EventId("40000000-0000-0000-0000-000000000011"), alice, bob, -1),
    "negative conversation ticks are rejected");

Console.WriteLine("All social core checks passed.");
