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

var generator = new StationRosterGenerator();
var professions = new[] { "Учёный", "Врач", "Инженер", "Повар", "Техник", "Радист" };
var roster = generator.Generate(seed: 417, professions);

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

var sameSeedRoster = generator.Generate(seed: 417, professions);
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

Console.WriteLine("All Protocol.Core smoke checks passed.");
