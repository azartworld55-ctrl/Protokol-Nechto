using Protocol.Autonomy;
using Protocol.Social.Domain;

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
    try
    {
        action();
    }
    catch (TException)
    {
        assertions++;
        return;
    }
    throw new InvalidOperationException($"FAILED: {message}");
}

ResidentId Id(int value) =>
    new(Guid.Parse($"00000000-0000-0000-0000-{value:D12}"));

DailySchedule StandardSchedule() =>
    new(new[]
    {
        new ScheduleBlock(8 * 60, 8 * 60, ScheduleActivity.Work),
        new ScheduleBlock(16 * 60, 6 * 60, ScheduleActivity.Leisure),
        new ScheduleBlock(22 * 60, 8 * 60, ScheduleActivity.Sleep)
    });

ResidentAutonomyProfile Profile(
    int id,
    int sociability = 50,
    int discipline = 50,
    IEnumerable<string>? habits = null) =>
    new(Id(id), $"profession-{id}", sociability, discipline, habits ?? Array.Empty<string>());

ResidentAutonomyState Resident(
    int id,
    NeedState? needs = null,
    AutonomyAction? action = null,
    int sociability = 50,
    int discipline = 50,
    IEnumerable<string>? habits = null,
    DailySchedule? schedule = null)
{
    var profile = Profile(id, sociability, discipline, habits);
    return new ResidentAutonomyState(
        profile,
        schedule ?? StandardSchedule(),
        needs ?? new NeedState(100, 100, 100, 100),
        action ?? new AutonomyAction(AutonomyActionKind.FreeTime));
}

AutonomyPolicy Policy() =>
    new(
        new[]
        {
            new NeedTrigger(NeedKind.Hunger, 700, 0),
            new NeedTrigger(NeedKind.Fatigue, 650, 1, PersonalityAxis.Discipline, 1),
            new NeedTrigger(NeedKind.Social, 600, 2, PersonalityAxis.Sociability, 2),
            new NeedTrigger(NeedKind.Stress, 700, 3)
        },
        new[]
        {
            new HabitActionPreference("evening_social", AutonomyActionKind.SeekSocialContact, 0),
            new HabitActionPreference("quiet_hobby", AutonomyActionKind.Leisure, 1)
        },
        new Dictionary<AutonomyActionKind, NeedDelta>
        {
            [AutonomyActionKind.Eat] = new(-400, 0, 0, 0),
            [AutonomyActionKind.Sleep] = new(0, -500, 0, -50),
            [AutonomyActionKind.SeekSocialContact] = new(0, 0, -300, -20),
            [AutonomyActionKind.Decompress] = new(0, 0, 0, -250),
            [AutonomyActionKind.Work] = new(80, 100, 20, 30),
            [AutonomyActionKind.Leisure] = new(20, -20, -20, -80),
            [AutonomyActionKind.FreeTime] = new(10, 10, 10, 10)
        },
        minimumSocialTrust: 40,
        minimumSocialFamiliarity: 40);

StationAutonomyState Station(
    int minuteOfDay = 8 * 60,
    Func<int, ResidentAutonomyState>? factory = null) =>
    new(
        Enumerable.Range(1, StationAutonomyState.ResidentCount)
            .Select(id => factory?.Invoke(id) ?? Resident(id)),
        minuteOfDay);

var policy = Policy();
var relationships = new RelationshipLedger();

Expect<ArgumentOutOfRangeException>(
    () => new NeedState(1001, 0, 0, 0),
    "need values cannot exceed 1000");
Expect<ArgumentOutOfRangeException>(
    () => new NeedState(0, -1, 0, 0),
    "need values cannot be negative");

var saturated = new NeedState(999, 1, 500, 500)
    .Apply(new NeedDelta(long.MaxValue, long.MinValue, long.MaxValue, long.MinValue));
Check(saturated.Hunger == 1000, "positive extreme need delta saturates at 1000");
Check(saturated.Fatigue == 0, "negative extreme need delta saturates at zero");
Check(saturated.Social == 1000 && saturated.Stress == 0, "need axes saturate independently");

var overnight = new DailySchedule(new[]
{
    new ScheduleBlock(22 * 60, 8 * 60, ScheduleActivity.Sleep)
});
Check(overnight.GetActivityAt(23 * 60) == ScheduleActivity.Sleep, "overnight block contains late evening");
Check(overnight.GetActivityAt(5 * 60 + 59) == ScheduleActivity.Sleep, "overnight block crosses midnight");
Check(overnight.GetActivityAt(6 * 60) is null, "schedule end is exclusive");
Check(overnight.GetActivityAt(12 * 60) is null, "unassigned time is free time");
Expect<ArgumentException>(
    () => new DailySchedule(new[]
    {
        new ScheduleBlock(22 * 60, 4 * 60, ScheduleActivity.Sleep),
        new ScheduleBlock(1 * 60, 60, ScheduleActivity.Work)
    }),
    "overlap across midnight is rejected");
Expect<ArgumentOutOfRangeException>(
    () => overnight.GetActivityAt(1440),
    "minute-of-day must be in 0..1439");

var station = Station();
Check(station.Residents.Count == 12, "station state contains exactly twelve residents");
Check(station.Residents.Select(item => item.Profile.Id).Distinct().Count() == 12, "station ids are unique");
Check(station.Residents.SequenceEqual(station.Residents.OrderBy(item => item.Profile.Id.Value)), "station residents use stable id order");

Expect<ArgumentException>(
    () => new StationAutonomyState(Enumerable.Range(1, 11).Select(Resident), 0),
    "eleven residents are rejected");
Expect<ArgumentException>(
    () => new StationAutonomyState(Enumerable.Range(1, 12).Select(_ => Resident(1)), 0),
    "duplicate resident ids are rejected");
Expect<ArgumentException>(
    () => new ResidentAutonomyState(
        Profile(1), StandardSchedule(), new NeedState(0, 0, 0, 0),
        new AutonomyAction(AutonomyActionKind.Work, "different-profession")),
    "work action must match configured profession");
Expect<ArgumentException>(
    () => new StationAutonomyState(
        Enumerable.Range(1, 12).Select(id => id == 1
            ? Resident(id, action: new AutonomyAction(AutonomyActionKind.SeekSocialContact, targetResidentId: Id(99)))
            : Resident(id)),
        0),
    "social intent cannot target a resident outside the station");

var rosterIds = station.Residents.Select(item => item.Profile.Id).ToArray();
var hungryWorker = Resident(1, needs: new NeedState(800, 100, 100, 100));
var hungerDecision = AutonomyDecisionEngine.Decide(hungryWorker, 9 * 60, rosterIds, relationships, policy);
Check(hungerDecision.Kind == AutonomyActionKind.Eat, "urgent hunger overrides a scheduled work block");

var sociableResident = Resident(1, needs: new NeedState(100, 100, 550, 100), sociability: 90);
var sociableDecision = AutonomyDecisionEngine.Decide(sociableResident, 9 * 60, rosterIds, relationships, policy);
Check(sociableDecision.Kind == AutonomyActionKind.SeekSocialContact,
    "configured sociability offset can make social need urgent");

var reservedResident = Resident(1, needs: new NeedState(100, 100, 550, 100), sociability: 10);
var reservedDecision = AutonomyDecisionEngine.Decide(reservedResident, 9 * 60, rosterIds, relationships, policy);
Check(reservedDecision.Kind == AutonomyActionKind.Work,
    "the same social need does not trigger below the personality-adjusted threshold");

var scheduledWorker = Resident(1);
var workDecision = AutonomyDecisionEngine.Decide(scheduledWorker, 9 * 60, rosterIds, relationships, policy);
Check(workDecision.Kind == AutonomyActionKind.Work, "work is selected inside a work block");
Check(workDecision.ProfessionId == "profession-1", "work intent preserves the resident profession id");
var leisureDecision = AutonomyDecisionEngine.Decide(scheduledWorker, 17 * 60, rosterIds, relationships, policy);
Check(leisureDecision.Kind == AutonomyActionKind.Leisure, "leisure is selected outside the work shift");

var habitResident = Resident(1, habits: new[] { "evening_social" });
var habitDecision = AutonomyDecisionEngine.Decide(habitResident, 17 * 60, rosterIds, relationships, policy);
Check(habitDecision.Kind == AutonomyActionKind.SeekSocialContact,
    "a configured habit shapes a non-critical leisure choice");

var socialResident = Resident(1, needs: new NeedState(100, 100, 900, 100));
relationships.Adjust(Id(1), Id(2), 70, 65);
relationships.Adjust(Id(1), Id(3), 80, 65);
relationships.Adjust(Id(1), Id(4), 100, 50);
var socialDecision = AutonomyDecisionEngine.Decide(socialResident, 9 * 60, rosterIds, relationships, policy);
Check(socialDecision.Kind == AutonomyActionKind.SeekSocialContact, "urgent social need produces an intent");
Check(socialDecision.TargetResidentId == Id(3), "social target uses familiarity, then trust, then stable id");

var workingStation = Station(
    15 * 60,
    id => Resident(
        id,
        needs: new NeedState(100, 100, 100, 100),
        action: new AutonomyAction(AutonomyActionKind.Work, $"profession-{id}")));
var advanced = StationAutonomySimulator.AdvanceHour(workingStation, policy, relationships);
Check(advanced.MinuteOfDay == 16 * 60, "hourly simulation advances the clock by sixty minutes");
Check(advanced.Residents.Count == 12, "hourly simulation retains all twelve residents");
var advancedResident = advanced.Residents.Single(item => item.Profile.Id == Id(1));
Check(advancedResident.Needs.Hunger == 180, "previous work action applies configured hunger change");
Check(advancedResident.Needs.Fatigue == 200, "previous work action applies configured fatigue change");
Check(advancedResident.CurrentAction.Kind == AutonomyActionKind.Leisure,
    "next action is selected from the schedule at the new time");

string RunDeterministicSimulation()
{
    var current = Station(
        7 * 60,
        id => Resident(
            id,
            needs: new NeedState(id * 10, id * 5, id * 7, id * 3),
            sociability: id * 7,
            discipline: 100 - id * 5,
            habits: id % 2 == 0 ? new[] { "quiet_hobby" } : Array.Empty<string>()));

    var ledger = new RelationshipLedger();
    for (var id = 1; id < 12; id++)
        ledger.Adjust(Id(id), Id(id + 1), 50 + id, 45 + id);

    for (var hour = 0; hour < 72; hour++)
        current = StationAutonomySimulator.AdvanceHour(current, policy, ledger);

    return string.Join(
        "|",
        current.MinuteOfDay,
        string.Join(
            ";",
            current.Residents.Select(item =>
                $"{item.Profile.Id}:{item.Needs.Hunger},{item.Needs.Fatigue},{item.Needs.Social},{item.Needs.Stress}:{item.CurrentAction.Kind}:{item.CurrentAction.ProfessionId}:{item.CurrentAction.TargetResidentId}")));
}

Check(RunDeterministicSimulation() == RunDeterministicSimulation(),
    "identical initial state and policy reproduce a 72-hour station simulation");

Expect<ArgumentException>(
    () => new AutonomyPolicy(
        new[] { new NeedTrigger(NeedKind.Hunger, 500, 0) },
        Array.Empty<HabitActionPreference>(),
        new Dictionary<AutonomyActionKind, NeedDelta>(),
        0,
        0),
    "policy rejects incomplete need triggers and effects");
Expect<ArgumentException>(
    () => new ResidentAutonomyProfile(Id(1), "engineer", 50, 50, new[] { "habit", "habit" }),
    "duplicate habit ids are rejected");

Console.WriteLine($"All autonomy checks passed: {assertions} assertions.");
