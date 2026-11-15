using Protocol.Core.Domain;
using Protocol.Core.Social;

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException($"FAILED: {message}");
}

var residentA = new Resident(Guid.NewGuid(), "Алекс", 31, "Инженер");
var residentB = new Resident(Guid.NewGuid(), "Мира", 28, "Врач");

var relationships = new RelationshipGraph();
relationships.AdjustTrust(residentA.Id, residentB.Id, 140);
Check(relationships.Get(residentB.Id, residentA.Id).Trust == 100,
    "trust is symmetric and capped at 100");

relationships.AdjustTrust(residentA.Id, residentB.Id, -250);
Check(relationships.Get(residentA.Id, residentB.Id).Trust == -100,
    "trust is floored at -100");

relationships.AdjustFamiliarity(residentA.Id, residentB.Id, 35);
Check(relationships.Get(residentB.Id, residentA.Id).Familiarity == 35,
    "familiarity is symmetric");

var observed = new MemoryEntry(
    residentA.Id,
    "station.shift.started",
    tick: 120,
    confidence: 1.0,
    MemoryOrigin.Observation);

var journal = new MemoryJournal();
journal.Remember(observed);
journal.Hear(observed, receivedAtTick: 150, reliability: 0.8);

Check(journal.Entries.Count == 2, "journal stores observations and received rumors");
Check(journal.Entries[1].Origin == MemoryOrigin.Rumor, "retold memory is marked as rumor");
Check(Math.Abs(journal.Entries[1].Confidence - 0.8) < 0.000001,
    "rumor reliability reduces confidence");
Check(journal.HasHeard("station.shift.started"), "journal can query known event codes");

var sourceJournal = new MemoryJournal();
sourceJournal.Remember(observed);
var recipientJournal = new MemoryJournal();
var rumorService = new RumorTransmissionService();

var noContact = rumorService.TryShare(
    sourceJournal,
    recipientJournal,
    "station.shift.started",
    receivedAtTick: 200,
    relationship: new RelationshipState(100, 100),
    hasFaceToFaceContact: false,
    random: new Random(1));
Check(!noContact && recipientJournal.Entries.Count == 0,
    "rumors are not transmitted without social contact");

var shared = rumorService.TryShare(
    sourceJournal,
    recipientJournal,
    "station.shift.started",
    receivedAtTick: 210,
    relationship: new RelationshipState(100, 100),
    hasFaceToFaceContact: true,
    random: new Random(1));
Check(shared, "a high-trust contact can transmit a known memory");
Check(recipientJournal.Entries.Single().Origin == MemoryOrigin.Rumor,
    "transmitted information is stored as a rumor");
Check(Math.Abs(recipientJournal.Entries.Single().Confidence - 0.9) < 0.000001,
    "relationship trust affects rumor reliability");

var repeatedShare = rumorService.TryShare(
    sourceJournal,
    recipientJournal,
    "station.shift.started",
    receivedAtTick: 220,
    relationship: new RelationshipState(100, 100),
    hasFaceToFaceContact: true,
    random: new Random(1));
Check(!repeatedShare && recipientJournal.Entries.Count == 1,
    "the same resident does not receive duplicate copies of a rumor");

var reactionGraph = new RelationshipGraph();
var reactionEngine = new RelationshipReactionEngine();
var helpEvent = new SocialEvent(
    residentA.Id,
    residentB.Id,
    tick: 300,
    SocialEventKind.Helped);
var afterHelp = reactionEngine.Apply(reactionGraph, helpEvent);
Check(afterHelp.Trust == 18 && afterHelp.Familiarity == 5,
    "helping another resident improves trust and familiarity");

var brokenPromise = new SocialEvent(
    residentA.Id,
    residentB.Id,
    tick: 400,
    SocialEventKind.BrokePromise);
var afterPromise = reactionEngine.Apply(reactionGraph, brokenPromise);
Check(afterPromise.Trust == -10 && afterPromise.Familiarity == 10,
    "breaking a promise reduces trust and increases familiarity");

var rosterGenerator = new StationRosterGenerator();
var professions = new[] { "Учёный", "Врач", "Инженер", "Повар", "Техник", "Радист" };
var roster = rosterGenerator.Generate(seed: 417, professions);

Check(roster.Residents.Count == StationRoster.RequiredResidentCount,
    "generated roster contains exactly twelve residents");
Check(roster.Residents.Select(resident => resident.Id).Distinct().Count() == 12,
    "generated roster has twelve unique identities");
Check(roster.PlayerResidentId != roster.ParasiteResidentId,
    "player is never assigned to the parasite");
Check(roster.Residents.All(resident =>
        resident.Personality.Curiosity is >= 0 and <= 100 &&
        resident.Personality.Sociability is >= 0 and <= 100 &&
        resident.Personality.Caution is >= 0 and <= 100 &&
        resident.Personality.Empathy is >= 0 and <= 100 &&
        resident.Personality.Discipline is >= 0 and <= 100),
    "all generated personality dimensions are within bounds");
Check(roster.Residents.All(resident => resident.Habits.Count is >= 1 and <= 3),
    "each generated resident has one to three distinct habits");

var sameSeedRoster = rosterGenerator.Generate(seed: 417, professions);
Check(roster.Residents.Select(resident => resident.Name)
        .SequenceEqual(sameSeedRoster.Residents.Select(resident => resident.Name)),
    "same seed reproduces the same resident name order");
Check(roster.PlayerResidentId == sameSeedRoster.PlayerResidentId &&
      roster.ParasiteResidentId == sameSeedRoster.ParasiteResidentId,
    "same seed reproduces player and parasite assignments");

var rejectedParasitePlayer = false;
try
{
    _ = new StationRoster(
        roster.Residents,
        roster.ParasiteResidentId,
        roster.ParasiteResidentId);
}
catch (ArgumentException)
{
    rejectedParasitePlayer = true;
}
Check(rejectedParasitePlayer, "roster rejects assigning the parasite as the player");


var witness = new Resident(Guid.NewGuid(), "Лена", 34, "Радист");
var rumorRecipient = new Resident(Guid.NewGuid(), "Борис", 42, "Техник");
var eventSource = new MemoryJournal();
var targetJournal = new MemoryJournal();
var witnessJournal = new MemoryJournal();
var recipientJournal2 = new MemoryJournal();
var eventGraph = new RelationshipGraph();
var eventMemoryService = new SocialEventMemoryService();
var witnessedEvent = new SocialEvent(
    residentA.Id,
    residentB.Id,
    tick: 510,
    SocialEventKind.Helped);

Check(eventMemoryService.RecordObservation(
        witnessedEvent, residentA.Id, eventSource, eventGraph),
    "the event actor stores a personal observation");
Check(eventMemoryService.RecordObservation(
        witnessedEvent, residentB.Id, targetJournal, eventGraph),
    "the event target stores a personal observation");
Check(eventMemoryService.RecordObservation(
        witnessedEvent, witness.Id, witnessJournal, eventGraph),
    "a supplied witness stores a personal observation");
Check(!eventMemoryService.RecordObservation(
        witnessedEvent, witness.Id, witnessJournal, eventGraph),
    "the same event is not recorded twice in one journal");
Check(eventGraph.Get(residentA.Id, witness.Id).Trust == 18,
    "a witness updates their relationship with the event actor");
Check(eventGraph.Get(residentA.Id, residentB.Id).Trust == 18,
    "the target updates their relationship with the event actor");
Check(targetJournal.Entries.Single().Origin == MemoryOrigin.Observation,
    "the target's knowledge is recorded as direct observation");

Check(eventMemoryService.TryShareRumor(
        witnessedEvent,
        rumorRecipient.Id,
        eventSource,
        recipientJournal2,
        receivedAtTick: 540,
        relationship: new RelationshipState(100, 100),
        hasFaceToFaceContact: true,
        random: new Random(1),
        relationships: eventGraph),
    "a known social event can spread through a direct conversation");
Check(recipientJournal2.Entries.Single().Origin == MemoryOrigin.Rumor,
    "the receiver records the event as a rumor, not an observation");
Check(eventGraph.Get(residentA.Id, rumorRecipient.Id).Trust == 18,
    "hearing the event changes the receiver's opinion of its actor");

var hungryMind = new Protocol.Core.Simulation.ResidentMind(
    residentA,
    new Protocol.Core.Simulation.ResidentNeeds(hunger: 100, fatigue: 0, social: 0, stress: 0));
var decisionEngine = new Protocol.Core.Simulation.ResidentDecisionEngine();
var urgentAction = decisionEngine.ChooseNext(
    hungryMind,
    new[] { residentA, residentB, witness },
    eventGraph,
    hourOfDay: 14);
Check(urgentAction == Protocol.Core.Simulation.ResidentAction.Eat,
    "urgent hunger can outweigh ordinary work and social preferences");

var simulation = new Protocol.Core.Simulation.StationSimulation(
    new[]
    {
        new Protocol.Core.Simulation.ResidentMind(residentA),
        new Protocol.Core.Simulation.ResidentMind(residentB),
        new Protocol.Core.Simulation.ResidentMind(witness),
        new Protocol.Core.Simulation.ResidentMind(rumorRecipient)
    },
    eventGraph);
var firstHour = simulation.AdvanceHour(hourOfDay: 9);
Check(firstHour.Count == 4 && simulation.Tick == 1,
    "station simulation advances every resident for one hour");
Check(simulation.Minds.Values.All(mind => mind.CurrentAction.HasValue),
    "each simulated resident receives an autonomous action");
Check(simulation.Minds.Values.All(mind => mind.Needs.Hunger > 0),
    "resident needs are tracked independently during simulation");


// Work schedules and profession-based task planning.
var dayShift = new Protocol.Core.Simulation.WorkShift(8, 16);
Check(dayShift.Contains(8) && dayShift.Contains(15) && !dayShift.Contains(16),
    "day shifts include their start and exclude their end");
var nightShift = new Protocol.Core.Simulation.WorkShift(22, 6);
Check(nightShift.Contains(22) && nightShift.Contains(2) && !nightShift.Contains(6),
    "night shifts correctly cross midnight");
var workSchedule = new Protocol.Core.Simulation.WorkSchedule(new[] { dayShift });
Check(workSchedule.IsWorkingAt(9) && !workSchedule.IsWorkingAt(19),
    "work schedule reports active and inactive hours");
Check(Protocol.Core.Simulation.ResidentWorkProfile.FromProfession("Врач").Task ==
      Protocol.Core.Simulation.WorkTask.MedicalCare,
    "medical professions map to medical work");
Check(Protocol.Core.Simulation.ResidentWorkProfile.FromProfession("Инженер").Task ==
      Protocol.Core.Simulation.WorkTask.Engineering,
    "engineering professions map to engineering work");
Check(Protocol.Core.Simulation.ResidentWorkProfile.FromProfession("Радист").Task ==
      Protocol.Core.Simulation.WorkTask.Communications,
    "radio operators map to communications work");

var dayWorker = new Protocol.Core.Simulation.ResidentMind(
    residentA,
    workProfile: Protocol.Core.Simulation.ResidentWorkProfile.FromProfession(
        residentA.Profession, workSchedule));
var nightWorker = new Protocol.Core.Simulation.ResidentMind(
    residentB,
    workProfile: Protocol.Core.Simulation.ResidentWorkProfile.FromProfession(
        residentB.Profession,
        new Protocol.Core.Simulation.WorkSchedule(new[]
        {
            new Protocol.Core.Simulation.WorkShift(22, 6)
        })));
var workPlanner = new Protocol.Core.Simulation.ResidentWorkPlanner();
var dayOrders = workPlanner.Plan(new[] { dayWorker, nightWorker }, hourOfDay: 9);
Check(dayOrders.Count == 1 && dayOrders.Single().ResidentId == residentA.Id,
    "only residents on an active shift receive work orders");
Check(dayOrders.Single().Task == Protocol.Core.Simulation.WorkTask.Engineering,
    "work orders carry the resident's profession-specific task");
var nightOrders = workPlanner.Plan(new[] { dayWorker, nightWorker }, hourOfDay: 2);
Check(nightOrders.Count == 1 && nightOrders.Single().ResidentId == residentB.Id,
    "overnight workers receive orders after midnight");
var offShiftAction = decisionEngine.ChooseNext(
    dayWorker,
    new[] { residentA, residentB },
    eventGraph,
    hourOfDay: 19);
Check(offShiftAction != Protocol.Core.Simulation.ResidentAction.Work,
    "residents are not assigned work as their routine action outside their shift");

var scheduledSimulation = new Protocol.Core.Simulation.StationSimulation(
    new[] { dayWorker, nightWorker }, eventGraph);
scheduledSimulation.AdvanceHour(hourOfDay: 9);
Check(scheduledSimulation.ActiveWorkOrders.Count == 1 &&
      scheduledSimulation.ActiveWorkOrders.Single().ResidentId == residentA.Id,
    "station simulation exposes only currently active work orders");

Console.WriteLine("All Protocol.Core smoke checks passed.");
