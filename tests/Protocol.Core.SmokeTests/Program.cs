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

Console.WriteLine("All Protocol.Core smoke checks passed.");
