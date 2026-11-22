using System.Collections.Generic;
using Protocol.Social.Domain;
using Protocol.Social.Memory;
using Protocol.Social.Simulation;

var assertions = 0;

void Check(bool condition, string message)
{
    assertions++;
    if (!condition)
        throw new InvalidOperationException($"FAILED: {message}");
}

void Expect<TException>(Action action, string message)
    where TException : Exception
{
    assertions++;
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

var alice = Id("61000000-0000-0000-0000-000000000001");
var bob = Id("61000000-0000-0000-0000-000000000002");
var carol = Id("61000000-0000-0000-0000-000000000003");
var subject = Id("62000000-0000-0000-0000-000000000001");

var aliceEventOne = EventId("63000000-0000-0000-0000-000000000001");
var aliceEventTwo = EventId("63000000-0000-0000-0000-000000000002");
var bobEventOne = EventId("63000000-0000-0000-0000-000000000003");
var conversationId = EventId("64000000-0000-0000-0000-000000000001");

var aliceJournal = new MemoryJournal(alice);
var bobJournal = new MemoryJournal(bob);
var carolJournal = new MemoryJournal(carol);

Check(aliceJournal.RecordObservation(new DirectObservation(
        aliceEventOne, alice, subject, 1, "Alice saw the lab door open.")),
    "participant A can record the first topic in their own journal");
Check(aliceJournal.RecordObservation(new DirectObservation(
        aliceEventTwo, alice, subject, 2, "Alice heard the ventilation stop.")),
    "participant A can record a second independent topic");
Check(bobJournal.RecordObservation(new DirectObservation(
        bobEventOne, bob, subject, 3, "Bob saw a light in the corridor.")),
    "participant B can record a topic in their own journal");

var relationships = new RelationshipLedger();
relationships.Adjust(alice, bob, trustDelta: 80, familiarityDelta: 75);

var conversation = new ConversationEvent(conversationId, alice, bob, tick: 10);
var rumorPolicy = new RumorTransmissionPolicy(
    minimumTrust: 50,
    minimumFamiliarity: 40,
    confidenceDecayPermille: 750,
    minimumConfidencePermille: 100,
    maximumRetellCount: 3);
var exchangePolicy = new ConversationExchangePolicy(maximumTurns: 5);
var service = new ConversationExchangeService();

var exchange = service.Exchange(
    conversation,
    aliceJournal,
    bobJournal,
    relationships,
    rumorPolicy,
    exchangePolicy,
    new[] { aliceEventOne, aliceEventTwo },
    new[] { bobEventOne });

Check(exchange.ConversationId == conversationId,
    "the transcript retains the explicit conversation id");
Check(exchange.Turns.Count == 5 && exchange.SharedRumorCount == 3,
    "the exchange executes the configured number of turns and counts only successful transfers");
Check(exchange.Turns[0].SpeakerId == alice &&
      exchange.Turns[0].ListenerId == bob &&
      exchange.Turns[0].Status == ConversationTurnStatus.SharedRumor &&
      exchange.Turns[0].TopicEventId == aliceEventOne,
    "participant A opens and shares the first caller-selected topic");
Check(exchange.Turns[1].SpeakerId == bob &&
      exchange.Turns[1].ListenerId == alice &&
      exchange.Turns[1].Status == ConversationTurnStatus.SharedRumor &&
      exchange.Turns[1].TopicEventId == bobEventOne,
    "the second turn alternates to participant B");
Check(exchange.Turns[2].SpeakerId == alice &&
      exchange.Turns[2].Status == ConversationTurnStatus.SharedRumor &&
      exchange.Turns[2].TopicEventId == aliceEventTwo,
    "participant A can share a second distinct topic on a later turn");
Check(exchange.Turns[3].Status == ConversationTurnStatus.NoTopic &&
      exchange.Turns[4].Status == ConversationTurnStatus.NoTopic,
    "a participant with an exhausted agenda produces an explicit no-topic turn");

Check(bobJournal.TryGet(aliceEventOne, out var aliceRumor) &&
      aliceRumor is not null &&
      aliceRumor.Kind == MemoryKind.HeardRumor &&
      aliceRumor.ConfidencePermille == 750 &&
      aliceRumor.OriginalObserverId == alice &&
      aliceRumor.LastInformantId == alice &&
      aliceRumor.SourceConversationId == conversationId,
    "the receiver stores the topic as a rumor with decay and provenance");
Check(aliceJournal.TryGet(bobEventOne, out var bobRumor) &&
      bobRumor is not null &&
      bobRumor.Kind == MemoryKind.HeardRumor &&
      bobRumor.ConfidencePermille == 750 &&
      bobRumor.OriginalObserverId == bob &&
      bobRumor.LastInformantId == bob,
    "the reverse turn uses the same rumor safeguards");
Check(carolJournal.Entries.Count == 0,
    "residents outside the explicit conversation receive no information");
Check(aliceJournal.TryGet(aliceEventOne, out var originalAliceEntry) &&
      originalAliceEntry is not null &&
      originalAliceEntry.Kind == MemoryKind.DirectObservation,
    "sharing does not rewrite the original observer's memory");

var transcriptList = (IList<ConversationTurn>)exchange.Turns;
Check(transcriptList.IsReadOnly,
    "the returned transcript is read-only");
Expect<NotSupportedException>(
    () => transcriptList.Clear(),
    "callers cannot mutate a completed conversation transcript");

var weakAliceJournal = new MemoryJournal(alice);
var weakBobJournal = new MemoryJournal(bob);
var weakEventId = EventId("63000000-0000-0000-0000-000000000004");
Check(weakAliceJournal.RecordObservation(new DirectObservation(
        weakEventId, alice, subject, 1, "A topic that cannot pass the relationship threshold.")),
    "the weak-relationship scenario has a valid source observation");

var weakRelationships = new RelationshipLedger();
weakRelationships.Adjust(alice, bob, trustDelta: 49, familiarityDelta: 100);
var weakExchange = service.Exchange(
    new ConversationEvent(EventId("64000000-0000-0000-0000-000000000002"), alice, bob, 10),
    weakAliceJournal,
    weakBobJournal,
    weakRelationships,
    rumorPolicy,
    new ConversationExchangePolicy(2),
    new[] { weakEventId },
    Array.Empty<Guid>());
Check(weakExchange.Turns[0].Status == ConversationTurnStatus.Rejected &&
      weakExchange.Turns[0].RejectionStatus == RumorShareStatus.RelationshipBelowThreshold,
    "a structured turn preserves the rumor service's relationship rejection");
Check(weakExchange.Turns[1].Status == ConversationTurnStatus.NoTopic &&
      weakBobJournal.Entries.Count == 0,
    "a rejected topic is not delivered and does not leak to another resident");

var unknownTopicExchange = service.Exchange(
    new ConversationEvent(EventId("64000000-0000-0000-0000-000000000003"), alice, bob, 10),
    new MemoryJournal(alice),
    new MemoryJournal(bob),
    relationships,
    rumorPolicy,
    new ConversationExchangePolicy(1),
    new[] { EventId("63000000-0000-0000-0000-000000000099") },
    Array.Empty<Guid>());
Check(unknownTopicExchange.Turns.Single().Status == ConversationTurnStatus.Rejected &&
      unknownTopicExchange.Turns.Single().RejectionStatus == RumorShareStatus.SenderDoesNotKnowEvent,
    "a caller cannot use the exchange service to disclose an event absent from the speaker's journal");

var earlyJournal = new MemoryJournal(alice);
var earlyEventId = EventId("63000000-0000-0000-0000-000000000005");
Check(earlyJournal.RecordObservation(new DirectObservation(
        earlyEventId, alice, subject, 8, "A later observation.")),
    "the early-conversation scenario records its event");
var earlyExchange = service.Exchange(
    new ConversationEvent(EventId("64000000-0000-0000-0000-000000000004"), alice, bob, 7),
    earlyJournal,
    new MemoryJournal(bob),
    relationships,
    rumorPolicy,
    new ConversationExchangePolicy(1),
    new[] { earlyEventId },
    Array.Empty<Guid>());
Check(earlyExchange.Turns.Single().RejectionStatus == RumorShareStatus.ConversationBeforeKnowledge,
    "the exchange cannot move knowledge backward in time");

Expect<ArgumentException>(
    () => service.Exchange(
        conversation,
        bobJournal,
        aliceJournal,
        relationships,
        rumorPolicy,
        exchangePolicy,
        new[] { aliceEventOne },
        new[] { bobEventOne }),
    "journals must match the participant order declared by the conversation");

Expect<ArgumentNullException>(
    () => service.Exchange(
        null!,
        aliceJournal,
        bobJournal,
        relationships,
        rumorPolicy,
        exchangePolicy,
        Array.Empty<Guid>(),
        Array.Empty<Guid>()),
    "an exchange cannot be started without an explicit ConversationEvent");

Expect<ArgumentOutOfRangeException>(
    () => new ConversationExchangePolicy(0),
    "zero conversation turns are rejected");
Expect<ArgumentOutOfRangeException>(
    () => new ConversationExchangePolicy(ConversationExchangePolicy.MaximumSupportedTurns + 1),
    "the hard upper bound prevents unbounded conversation processing");

Expect<ArgumentException>(
    () => service.Exchange(
        conversation,
        aliceJournal,
        bobJournal,
        relationships,
        rumorPolicy,
        exchangePolicy,
        new[] { Guid.Empty },
        Array.Empty<Guid>()),
    "empty topic event ids are rejected");
Expect<ArgumentException>(
    () => service.Exchange(
        conversation,
        aliceJournal,
        bobJournal,
        relationships,
        rumorPolicy,
        exchangePolicy,
        new[] { aliceEventOne, aliceEventOne },
        Array.Empty<Guid>()),
    "duplicate topic ids in one participant agenda are rejected");
Expect<ArgumentException>(
    () => service.Exchange(
        conversation,
        aliceJournal,
        bobJournal,
        relationships,
        rumorPolicy,
        new ConversationExchangePolicy(1),
        new[] { aliceEventOne, aliceEventTwo },
        Array.Empty<Guid>()),
    "a participant agenda cannot exceed the configured turn bound");

var replayAliceJournal = new MemoryJournal(alice);
var replayBobJournal = new MemoryJournal(bob);
replayAliceJournal.RecordObservation(new DirectObservation(
    aliceEventOne, alice, subject, 1, "Alice saw the lab door open."));
replayAliceJournal.RecordObservation(new DirectObservation(
    aliceEventTwo, alice, subject, 2, "Alice heard the ventilation stop."));
replayBobJournal.RecordObservation(new DirectObservation(
    bobEventOne, bob, subject, 3, "Bob saw a light in the corridor."));
var replayRelationships = new RelationshipLedger();
replayRelationships.Adjust(alice, bob, 80, 75);
var replay = service.Exchange(
    new ConversationEvent(conversationId, alice, bob, 10),
    replayAliceJournal,
    replayBobJournal,
    replayRelationships,
    rumorPolicy,
    exchangePolicy,
    new[] { aliceEventOne, aliceEventTwo },
    new[] { bobEventOne });

var originalTrace = exchange.Turns
    .Select(turn => $"{turn.Number}|{turn.SpeakerId.Value}|{turn.ListenerId.Value}|{turn.Status}|{turn.TopicEventId}|{turn.RejectionStatus}")
    .ToArray();
var replayTrace = replay.Turns
    .Select(turn => $"{turn.Number}|{turn.SpeakerId.Value}|{turn.ListenerId.Value}|{turn.Status}|{turn.TopicEventId}|{turn.RejectionStatus}")
    .ToArray();
Check(originalTrace.SequenceEqual(replayTrace),
    "identical conversation inputs produce an identical structured transcript");

Console.WriteLine($"All conversation exchange checks passed ({assertions} assertions).");
