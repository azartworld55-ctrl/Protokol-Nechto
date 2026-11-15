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


var contactSourceJournal = new MemoryJournal();
contactSourceJournal.Remember(new MemoryEntry(
    residentA.Id, "contact.test.event", 800, 1.0, MemoryOrigin.Observation));
var contactRecipientJournal = new MemoryJournal();
var uninvolvedJournal = new MemoryJournal();
var contactSourceMind = new Protocol.Core.Simulation.ResidentMind(
    residentA, memories: contactSourceJournal);
var contactRecipientMind = new Protocol.Core.Simulation.ResidentMind(
    residentB, memories: contactRecipientJournal);
var contactGraph = new RelationshipGraph();
var contactProcessor = new Protocol.Core.Simulation.SocialContactProcessor();
var contactResult = contactProcessor.Process(
    new Protocol.Core.Simulation.SocialContact(residentA.Id, residentB.Id, 900),
    contactSourceMind,
    contactRecipientMind,
    contactGraph,
    new Random(1));
Check(contactGraph.Get(residentA.Id, residentB.Id).Familiarity == 1,
    "a face-to-face encounter increases familiarity");
Check(contactResult.FirstSharedWithSecond &&
      contactRecipientJournal.Entries.Single().Origin == MemoryOrigin.Rumor,
    "a contact can transmit one known memory as a rumor");
Check(uninvolvedJournal.Entries.Count == 0,
    "residents outside the supplied contact do not receive information");
var rejectedContactPair = false;
try
{
    contactProcessor.Process(
        new Protocol.Core.Simulation.SocialContact(residentA.Id, witness.Id, 901),
        contactSourceMind,
        contactRecipientMind,
        contactGraph,
        new Random(1));
}
catch (ArgumentException)
{
    rejectedContactPair = true;
}
Check(rejectedContactPair, "contact processing rejects mismatched participant identities");



// Map geometry: visibility is personal and walls/closed doors occlude it.
var mapCells = Enumerable.Repeat(
    Protocol.Core.Simulation.MapCell.Floor, 7 * 5).ToArray();
for (var z = 0; z < 5; z++)
    mapCells[z * 7 + 3] = Protocol.Core.Simulation.MapCell.Wall;
var mapSnapshot = new Protocol.Core.Simulation.StationMapSnapshot(
    7,
    5,
    mapCells,
    new Dictionary<Guid, Protocol.Core.Simulation.GridPosition>
    {
        [residentA.Id] = new(1, 2),
        [residentB.Id] = new(2, 2),
        [witness.Id] = new(4, 2),
        [rumorRecipient.Id] = new(5, 2)
    });
var perception = new Protocol.Core.Simulation.StationPerceptionService();
var playerVisibleResidents = perception.GetVisibleResidents(
    mapSnapshot, residentA.Id, sightRadius: 6);
Check(playerVisibleResidents.SetEquals(new[] { residentB.Id }),
    "the observer sees only residents with an unobstructed line of sight");
var mapContacts = perception.DetectSocialContacts(
    mapSnapshot, tick: 1000, conversationRadius: 2);
Check(mapContacts.Count == 2,
    "map contact detection finds both physically reachable pairs on separate sides of a wall");
Check(mapContacts.Any(contact =>
        new[] { contact.FirstResidentId, contact.SecondResidentId }
            .ToHashSet().SetEquals(new[] { residentA.Id, residentB.Id })),
    "residents on the same side of a wall can meet");
Check(mapContacts.Any(contact =>
        new[] { contact.FirstResidentId, contact.SecondResidentId }
            .ToHashSet().SetEquals(new[] { witness.Id, rumorRecipient.Id })),
    "NPCs outside player vision still have their own social contacts");

var visibleEventWitnesses = perception.GetResidentsWhoCanSee(
    mapSnapshot, new Protocol.Core.Simulation.GridPosition(2, 2), sightRadius: 6);
Check(visibleEventWitnesses.SetEquals(new[] { residentA.Id, residentB.Id }),
    "event witnesses are derived from actual map visibility");
var visibleTiles = perception.GetVisibleCells(mapSnapshot, residentA.Id, sightRadius: 6);
Check(visibleTiles.Contains(new Protocol.Core.Simulation.GridPosition(2, 2)) &&
      !visibleTiles.Contains(new Protocol.Core.Simulation.GridPosition(4, 2)),
    "the visible tile set is blocked by the intervening wall");

var contactMinds = new Dictionary<Guid, Protocol.Core.Simulation.ResidentMind>
{
    [residentA.Id] = new(residentA),
    [residentB.Id] = new(residentB),
    [witness.Id] = new(witness),
    [rumorRecipient.Id] = new(rumorRecipient)
};
var mapContactGraph = new RelationshipGraph();
var contactRuntime = new Protocol.Core.Simulation.StationSocialContactRuntime();
var processedContacts = contactRuntime.ProcessMapContacts(
    mapSnapshot, contactMinds, mapContactGraph, tick: 1001, random: new Random(1),
    conversationRadius: 2);
Check(processedContacts.Count == 2,
    "map-derived contacts are passed into the social contact processor");
Check(mapContactGraph.Get(residentA.Id, residentB.Id).Familiarity == 1 &&
      mapContactGraph.Get(witness.Id, rumorRecipient.Id).Familiarity == 1,
    "both visible and off-screen NPC pairs process their own meetings");


var doorMap = new Protocol.Core.Simulation.StationMapSnapshot(
    3,
    1,
    new[]
    {
        Protocol.Core.Simulation.MapCell.Floor,
        Protocol.Core.Simulation.MapCell.ClosedDoor,
        Protocol.Core.Simulation.MapCell.Floor
    },
    new Dictionary<Guid, Protocol.Core.Simulation.GridPosition>
    {
        [residentA.Id] = new(0, 0),
        [residentB.Id] = new(2, 0)
    });
Check(!doorMap.HasLineOfSight(new(0, 0), new(2, 0)),
    "a closed door blocks line of sight");

var cornerMap = new Protocol.Core.Simulation.StationMapSnapshot(
    2,
    2,
    new[]
    {
        Protocol.Core.Simulation.MapCell.Floor,
        Protocol.Core.Simulation.MapCell.Wall,
        Protocol.Core.Simulation.MapCell.Wall,
        Protocol.Core.Simulation.MapCell.Floor
    },
    new Dictionary<Guid, Protocol.Core.Simulation.GridPosition>());
Check(!cornerMap.HasLineOfSight(new(0, 0), new(1, 1)),
    "line of sight cannot pass diagonally between two blocking corners");

// The controlled resident gets a dedicated task board and HUD snapshot.
var playerSimulation = new Protocol.Core.Simulation.StationSimulation(
    new[]
    {
        new Protocol.Core.Simulation.ResidentMind(residentA),
        new Protocol.Core.Simulation.ResidentMind(residentB),
        new Protocol.Core.Simulation.ResidentMind(witness)
    },
    playerResidentId: residentA.Id);
Check(playerSimulation.PlayerTasks is not null &&
      playerSimulation.PlayerTasks.PlayerResidentId == residentA.Id,
    "the simulation creates a personal task board for the controlled resident");
var playerTaskId = Guid.NewGuid();
playerSimulation.PlayerTasks!.Assign(new Protocol.Core.Simulation.PlayerTask(
    playerTaskId,
    "Проверить лабораторию",
    "Осмотреть оборудование и отметить неисправности.",
    "Сюжет",
    Protocol.Core.Simulation.PlayerTaskPriority.High,
    createdAtTick: 12));
var taskHud = playerSimulation.PlayerTasks.CreateHudSnapshot();
Check(taskHud.Header == "МОИ ЗАДАНИЯ" &&
      taskHud.ActiveCount == 1 &&
      taskHud.Tasks.Single().Id == playerTaskId,
    "the player HUD exposes the player's active task with its title and details");
Check(taskHud.PlayerResidentId == residentA.Id,
    "the task HUD is bound to the controlled resident identity");
Check(playerSimulation.PlayerTasks.Complete(playerTaskId),
    "the player can complete an assigned task");
Check(playerSimulation.PlayerTasks.CreateHudSnapshot().ActiveCount == 0,
    "completed tasks disappear from the active task HUD");
Check(playerSimulation.PlayerTasks.CreateHudSnapshot(includeFinished: true)
        .Tasks.Single().Status == Protocol.Core.Simulation.PlayerTaskStatus.Completed,
    "completed tasks remain available in the expanded task history");

var rejectedUnknownPlayer = false;
try
{
    _ = new Protocol.Core.Simulation.StationSimulation(
        new[] { new Protocol.Core.Simulation.ResidentMind(residentA) },
        playerResidentId: residentB.Id);
}
catch (ArgumentException)
{
    rejectedUnknownPlayer = true;
}
Check(rejectedUnknownPlayer,
    "the personal task board cannot be attached to a resident outside the simulation");




// Parasite lifecycle: an isolated attack copies identity and memories, while
// a witness or another nearby resident prevents the attack.
var parasiteResident = roster.Residents.Single(item => item.Id == roster.ParasiteResidentId);
var playerResident = roster.Residents.Single(item => item.Id == roster.PlayerResidentId);
var nextPlayerResident = roster.Residents.First(item =>
    item.Id != roster.PlayerResidentId && item.Id != roster.ParasiteResidentId);
var parasiteMemory = new MemoryJournal();
parasiteMemory.Remember(new MemoryEntry(
    parasiteResident.Id, "parasite.observed.station", 20, 1.0, MemoryOrigin.Observation));
var victimMemory = new MemoryJournal();
victimMemory.Remember(new MemoryEntry(
    playerResident.Id, "victim.private.discovery", 45, 0.95, MemoryOrigin.Observation));
victimMemory.Hear(new MemoryEntry(
    witness.Id, "station.old.rumor", 30, 0.8, MemoryOrigin.Observation), 50, 0.7);
var parasiteMind = new Protocol.Core.Simulation.ResidentMind(
    parasiteResident, memories: parasiteMemory);
var victimMind = new Protocol.Core.Simulation.ResidentMind(
    playerResident, memories: victimMemory);
var parasiteLifecycle = new Protocol.Core.Simulation.ParasiteLifecycle(
    parasiteResident.Id,
    new[]
    {
        parasiteMind,
        victimMind,
        new Protocol.Core.Simulation.ResidentMind(nextPlayerResident),
        new Protocol.Core.Simulation.ResidentMind(witness),
        new Protocol.Core.Simulation.ResidentMind(rumorRecipient)
    },
    playerResident.Id);
var attackBlockedByWitness = parasiteLifecycle.TryConsume(
    victimMind,
    new[] { parasiteResident.Id, playerResident.Id },
    new[] { witness.Id });
Check(!attackBlockedByWitness.Succeeded &&
      attackBlockedByWitness.Failure ==
          Protocol.Core.Simulation.ParasiteAttackFailure.WitnessPresent &&
      parasiteLifecycle.IsAlive(playerResident.Id),
    "a witnessed attack is rejected without changing the victim's life state");
var attackBlockedByCrowd = parasiteLifecycle.TryConsume(
    victimMind,
    new[] { parasiteResident.Id, playerResident.Id, witness.Id },
    Array.Empty<Guid>());
Check(!attackBlockedByCrowd.Succeeded &&
      attackBlockedByCrowd.Failure ==
          Protocol.Core.Simulation.ParasiteAttackFailure.NotAlone,
    "an attack is rejected when anyone else is present, even without a witness");
var successfulConsumption = parasiteLifecycle.TryConsume(
    victimMind,
    new[] { parasiteResident.Id, playerResident.Id },
    Array.Empty<Guid>());
Check(successfulConsumption.Succeeded &&
      successfulConsumption.PlayerWasKilled &&
      successfulConsumption.MemoriesCopied == 2,
    "an isolated attack consumes the victim and copies all of their memories");
Check(!parasiteLifecycle.IsAlive(playerResident.Id) &&
      parasiteLifecycle.DeadResidentIds.Contains(playerResident.Id),
    "the consumed resident is marked dead");
Check(parasiteLifecycle.CurrentDisguise?.Name == playerResident.Name &&
      parasiteLifecycle.CurrentDisguise?.Profession == playerResident.Profession &&
      parasiteLifecycle.CurrentDisguise?.Age == playerResident.Age,
    "the parasite adopts the victim's visible identity and social role");
Check(parasiteMemory.HasHeard("victim.private.discovery") &&
      parasiteMemory.HasHeard("station.old.rumor"),
    "the parasite inherits both direct memories and rumors");
Check(parasiteLifecycle.PlayerResidentId is null,
    "the player slot is cleared when the controlled human dies");
Check(!parasiteLifecycle.SelectPlayer(parasiteResident.Id),
    "the parasite can never be selected as the player character");
Check(parasiteLifecycle.SelectPlayer(nextPlayerResident.Id) &&
      parasiteLifecycle.PlayerResidentId == nextPlayerResident.Id,
    "the player can take control of another living human after death");
Check(!parasiteLifecycle.SelectPlayer(playerResident.Id),
    "a dead resident cannot be selected again");
var repeatedConsumption = parasiteLifecycle.TryConsume(
    victimMind,
    new[] { parasiteResident.Id, playerResident.Id },
    Array.Empty<Guid>());
Check(!repeatedConsumption.Succeeded &&
      repeatedConsumption.Failure ==
          Protocol.Core.Simulation.ParasiteAttackFailure.TargetAlreadyDead,
    "a consumed resident cannot be consumed a second time");



// Investigation evidence is personal, weighted by source quality, and never
// exposes the hidden truth of the suspect's identity.
var reporterC = new Resident(Guid.NewGuid(), "Илья", 34, "Техник");
var investigationRelationships = new RelationshipGraph();
investigationRelationships.AdjustTrust(residentA.Id, residentB.Id, 40);
var caseFile = new InvestigationCaseFile(
    residentA.Id,
    residentB.Id,
    new[] { residentA.Id, residentB.Id, reporterC.Id });
Check(caseFile.AddEvidence(new InvestigationEvidence(
        residentA.Id, residentB.Id, "security.corridor.seen", 500, 1.0,
        InvestigationEvidenceKind.DirectObservation)),
    "case file accepts a first direct observation");
Check(!caseFile.AddEvidence(new InvestigationEvidence(
        residentA.Id, residentB.Id, "security.corridor.seen", 510, 1.0,
        InvestigationEvidenceKind.DirectObservation)),
    "the same reporter cannot inflate a case by repeating the same event");
Check(caseFile.AddEvidence(new InvestigationEvidence(
        reporterC.Id, residentB.Id, "medical.log.discrepancy", 520, 0.9,
        InvestigationEvidenceKind.PhysicalTrace)),
    "case file accepts a separate physical trace");
Check(caseFile.AddEvidence(new InvestigationEvidence(
        residentA.Id, residentB.Id, "canteen.heard.claim", 530, 0.8,
        InvestigationEvidenceKind.Rumor)),
    "case file accepts a rumor as weaker evidence");
var assessment = caseFile.Assess();
Check(assessment.DirectObservationCount == 1 &&
      assessment.PhysicalTraceCount == 1 &&
      assessment.RumorCount == 1 &&
      assessment.IndependentReporterCount == 2 &&
      assessment.LatestEvidenceTick == 530,
    "case assessment reports evidence types, independent sources, and latest tick");
Check(assessment.SuspicionScore > 70,
    "independent evidence combines into a high suspicion score without proving guilt");
var accusation = new AccusationService().Issue(caseFile, investigationRelationships);
Check(accusation.Assessment == AccusationAssessment.Plausible &&
      accusation.SuspicionScore == assessment.SuspicionScore &&
      accusation.TrustChange == -9,
    "a case with one direct observation remains plausible rather than confirmed");
Check(investigationRelationships.Get(residentA.Id, residentB.Id).Trust == 31,
    "making an accusation has a measurable social cost");

var weakCase = new InvestigationCaseFile(
    residentA.Id, residentB.Id, new[] { residentA.Id, residentB.Id });
weakCase.AddEvidence(new InvestigationEvidence(
    residentA.Id, residentB.Id, "unverified.canteen.story", 600, 0.2,
    InvestigationEvidenceKind.Rumor));
var weakAccusation = new AccusationService().Issue(
    weakCase, investigationRelationships);
Check(weakAccusation.Assessment == AccusationAssessment.Weak &&
      weakAccusation.TrustChange == -18,
    "an accusation based on a faint rumor is classified as weak and damages trust more");
Check(investigationRelationships.Get(residentA.Id, residentB.Id).Trust == 13,
    "weak accusations have stronger relationship consequences");

var corroboratedCase = new InvestigationCaseFile(
    residentA.Id, residentB.Id, new[] { residentA.Id, residentB.Id, reporterC.Id });
corroboratedCase.AddEvidence(new InvestigationEvidence(
    residentA.Id, residentB.Id, "airlock.direct.report.one", 700, 1.0,
    InvestigationEvidenceKind.DirectObservation));
corroboratedCase.AddEvidence(new InvestigationEvidence(
    reporterC.Id, residentB.Id, "airlock.direct.report.two", 701, 1.0,
    InvestigationEvidenceKind.DirectObservation));
var corroboratedAccusation = new AccusationService().Issue(
    corroboratedCase, investigationRelationships);
Check(corroboratedAccusation.Assessment == AccusationAssessment.StronglySupported &&
      corroboratedAccusation.IndependentReporterCount == 2 &&
      corroboratedAccusation.DirectObservationCount == 2,
    "two independent direct reports can strongly support an accusation without making it infallible");



// Custody is a state machine with an auditable timeline. Physical detention
// and escape conditions are supplied later by the game adapter.
var custody = new Protocol.Core.Simulation.ResidentCustodyRegistry(
    new[] { residentA.Id, residentB.Id, reporterC.Id });
Check(custody.GetStatus(residentA.Id) ==
      Protocol.Core.Simulation.ResidentCustodyStatus.Free,
    "residents begin outside custody");
Check(custody.Detain(residentA.Id, "suspected assault", 800) &&
      custody.IsDetained(residentA.Id),
    "a living station resident can be detained with a recorded reason");
Check(!custody.Detain(residentA.Id, "duplicate detention", 801),
    "a resident already in custody cannot be detained twice");
Check(custody.ResolveEscapeAttempt(
        residentA.Id, succeeded: false, tick: 810) &&
      custody.IsDetained(residentA.Id),
    "a failed escape attempt leaves the resident detained");
Check(custody.ResolveEscapeAttempt(
        residentA.Id, succeeded: true, tick: 820) &&
      custody.GetStatus(residentA.Id) ==
          Protocol.Core.Simulation.ResidentCustodyStatus.Escaped,
    "a successful escape changes custody state");
Check(!custody.ResolveEscapeAttempt(residentA.Id, succeeded: true, tick: 821),
    "an escaped resident cannot resolve another custody escape attempt");
var custodyHistory = custody.GetSnapshot(residentA.Id);
Check(custodyHistory.DetainedAtTick == 800 &&
      custodyHistory.DetentionReason == "suspected assault" &&
      custodyHistory.History.Count == 3 &&
      custodyHistory.History[1].Kind ==
          Protocol.Core.Simulation.CustodyTransitionKind.EscapeAttemptFailed &&
      custodyHistory.History[2].Kind ==
          Protocol.Core.Simulation.CustodyTransitionKind.Escaped,
    "custody snapshot preserves the detention reason and complete transition history");
Check(custody.Detain(residentB.Id, "mistaken accusation", 830) &&
      custody.Release(residentB.Id, "evidence cleared", 840) &&
      custody.GetStatus(residentB.Id) ==
          Protocol.Core.Simulation.ResidentCustodyStatus.Released,
    "a detained resident can be formally released");
Check(!custody.Release(residentB.Id, "duplicate release", 841),
    "a resident cannot be released when not detained");


// Observation-limited parasite decisions and personal NPC suspicion.
var parasiteBrain = new Protocol.Core.Simulation.ParasiteMind(parasiteResident.Id);
var parasiteBrainEngine = new Protocol.Core.Simulation.ParasiteDecisionEngine();
var unseenDecision = parasiteBrainEngine.Decide(
    parasiteBrain,
    new Protocol.Core.Simulation.ParasiteObservation(100, Array.Empty<Protocol.Core.Simulation.ParasiteOpportunity>()));
Check(unseenDecision.Kind == Protocol.Core.Simulation.ParasiteDecisionKind.Observe &&
      unseenDecision.TargetId is null,
    "the parasite cannot select a resident it has not observed");
var crowdedDecision = parasiteBrainEngine.Decide(
    parasiteBrain,
    new Protocol.Core.Simulation.ParasiteObservation(101, new[]
    {
        new Protocol.Core.Simulation.ParasiteOpportunity(
            residentA.Id, true, 2, 1, true, 0.05)
    }));
Check(crowdedDecision.Kind == Protocol.Core.Simulation.ParasiteDecisionKind.Wait &&
      crowdedDecision.TargetId is null,
    "the parasite waits when an observed resident is not isolated");
var blockedSightDecision = parasiteBrainEngine.Decide(
    parasiteBrain,
    new Protocol.Core.Simulation.ParasiteObservation(102, new[]
    {
        new Protocol.Core.Simulation.ParasiteOpportunity(
            residentA.Id, true, 2, 0, false, 0.05)
    }));
Check(blockedSightDecision.Kind == Protocol.Core.Simulation.ParasiteDecisionKind.Wait,
    "the parasite cannot act on a candidate without line of sight");
var riskyDecision = parasiteBrainEngine.Decide(
    parasiteBrain,
    new Protocol.Core.Simulation.ParasiteObservation(103, new[]
    {
        new Protocol.Core.Simulation.ParasiteOpportunity(
            residentA.Id, true, 2, 0, true, 0.9)
    }));
Check(riskyDecision.Kind == Protocol.Core.Simulation.ParasiteDecisionKind.Wait,
    "the parasite waits when suspicion risk is too high");
var playerOpportunity = parasiteBrainEngine.Decide(
    parasiteBrain,
    new Protocol.Core.Simulation.ParasiteObservation(104, new[]
    {
        new Protocol.Core.Simulation.ParasiteOpportunity(
            residentA.Id, true, 4, 0, true, 0.2)
    }));
Check(playerOpportunity.Kind == Protocol.Core.Simulation.ParasiteDecisionKind.PursueOpportunity &&
      playerOpportunity.TargetId == residentA.Id &&
      parasiteBrain.PreferredTargetId == residentA.Id,
    "the parasite may choose the human player when that person is actually observed");
Check(parasiteBrain.ConsecutiveWaits == 0,
    "a selected opportunity resets the parasite's wait counter");

var uninformedMind = new Protocol.Core.Simulation.ResidentMind(residentA);
var suspicionService = new SuspicionResponseService();
var suspicionBeforeKnowledge = suspicionService.Evaluate(
    uninformedMind, residentB.Id, new RelationshipGraph());
Check(suspicionBeforeKnowledge.Score == 0 &&
      suspicionBeforeKnowledge.Response == SuspicionResponse.Unconcerned,
    "an NPC does not suspect someone based on events absent from their own journal");

var personalConcernJournal = new MemoryJournal();
personalConcernJournal.Remember(new MemoryEntry(
    residentB.Id, "crime.corridor.first-hand", 1100, 1.0, MemoryOrigin.Observation));
personalConcernJournal.Hear(new MemoryEntry(
    residentB.Id, "crime.canteen.heard", 1090, 0.8, MemoryOrigin.Observation),
    1110, 0.75);
var concernedMind = new Protocol.Core.Simulation.ResidentMind(
    residentA, memories: personalConcernJournal);
var suspicionGraph = new RelationshipGraph();
suspicionGraph.AdjustTrust(residentA.Id, residentB.Id, -40);
var personalSuspicion = suspicionService.Evaluate(
    concernedMind, residentB.Id, suspicionGraph);
Check(personalSuspicion.DirectObservations == 1 &&
      personalSuspicion.Rumors == 1 &&
      personalSuspicion.Score > 50 &&
      personalSuspicion.Response == SuspicionResponse.Avoid,
    "personal observations weigh more than rumors when an NPC evaluates concern");

var strongConcernJournal = new MemoryJournal();
strongConcernJournal.Remember(new MemoryEntry(
    residentB.Id, "crime.lab.first-hand", 1120, 1.0, MemoryOrigin.Observation));
strongConcernJournal.Remember(new MemoryEntry(
    residentB.Id, "crime.airlock.first-hand", 1121, 1.0, MemoryOrigin.Observation));
var reportingMind = new Protocol.Core.Simulation.ResidentMind(
    witness, memories: strongConcernJournal);
var consequenceGraph = new RelationshipGraph();
var consequence = new SocialConsequenceService().ProcessKnownConcern(
    reportingMind, residentB.Id, consequenceGraph);
Check(consequence.ShouldOpenInvestigation &&
      consequence.TrustChange < 0 &&
      consequenceGraph.Get(witness.Id, residentB.Id).Trust == consequence.TrustChange,
    "a resident may report a concern based on multiple personal observations without proving guilt");
Check(consequenceGraph.Get(residentA.Id, residentB.Id).Trust == 0,
    "social consequences do not spread automatically to uninvolved residents");

Console.WriteLine("All Protocol.Core smoke checks passed.");
