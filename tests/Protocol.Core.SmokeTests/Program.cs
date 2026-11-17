using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Protocol.Core.Domain;
using Protocol.Determinism;
using Protocol.ProfessionConfig;

var checks = 0;

void Check(bool condition, string message)
{
    checks++;
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
        Check(true, message);
        return;
    }

    Check(false, message);
}

var randomVector = new StableRandom(0);
Check(randomVector.NextUInt64() == 0xE220A8397B1DCDAFUL, "SplitMix64 vector 1 is stable");
Check(randomVector.NextUInt64() == 0x6E789E6AA1B965F4UL, "SplitMix64 vector 2 is stable");
Check(randomVector.NextUInt64() == 0x06C45D188009454FUL, "SplitMix64 vector 3 is stable");
Check(randomVector.NextUInt64() == 0xF88BB8A8724C81ECUL, "SplitMix64 vector 4 is stable");

var boundedRandom = new StableRandom(long.MinValue);
for (var index = 0; index < 1000; index++)
{
    var value = boundedRandom.NextInt(7);
    Check(value is >= 0 and < 7, "bounded random values remain inside the requested interval");
}

var doubleRandom = new StableRandom(long.MaxValue);
for (var index = 0; index < 1000; index++)
{
    var value = doubleRandom.NextDouble();
    Check(value is >= 0.0 and < 1.0, "random doubles remain inside the half-open unit interval");
}

Expect<ArgumentOutOfRangeException>(() => new StableRandom(1).NextInt(0), "zero-sized random range is rejected");
Expect<ArgumentOutOfRangeException>(() => new StableRandom(1).NextInt(-1), "negative random range is rejected");

var configuration = new ProfessionQuotaConfiguration(
[
    new ProfessionQuota("test-profession-alpha", 3),
    new ProfessionQuota("test-profession-beta", 4),
    new ProfessionQuota("test-profession-gamma", 5)
]);

var generator = new StationRosterGenerator();
const long seed = 20261118;
var roster = generator.Generate(seed, configuration);
var repeatedRoster = generator.Generate(seed, configuration);
var differentSeedRoster = generator.Generate(seed + 1, configuration);

Check(roster.Residents.Count == StationRoster.RequiredResidentCount, "generated roster contains exactly twelve residents");
Check(roster.Residents.Select(resident => resident.Id).Distinct().Count() == 12, "resident IDs are unique");
Check(roster.Residents.Select(resident => resident.Name).Distinct(StringComparer.Ordinal).Count() == 12, "resident names are unique");
Check(roster.Residents.All(resident => resident.Age is >= 20 and <= 65), "generated ages remain in the configured adult range");
Check(roster.PlayerResidentId != roster.ParasiteResidentId, "player and parasite identities are distinct");
Check(roster.Residents.Any(resident => resident.Id == roster.PlayerResidentId), "player identity belongs to the roster");
Check(roster.Residents.Any(resident => resident.Id == roster.ParasiteResidentId), "parasite identity belongs to the roster");

foreach (var resident in roster.Residents)
{
    var personality = resident.Personality;
    Check(
        new[]
        {
            personality.Curiosity,
            personality.Sociability,
            personality.Caution,
            personality.Empathy,
            personality.Discipline
        }.All(value => value is >= 0 and <= 100),
        "all personality dimensions are bounded");

    Check(resident.Habits.Count is >= 1 and <= 3, "each resident receives one to three habits");
    Check(resident.Habits.Distinct().Count() == resident.Habits.Count, "a resident has no duplicate habits");
    Check(resident.Traits.Count is >= 1 and <= 2, "each resident receives one or two individual traits");
    Check(resident.Traits.Distinct().Count() == resident.Traits.Count, "a resident has no duplicate individual traits");
}

foreach (var quota in configuration.Quotas)
{
    Check(
        roster.Residents.Count(resident => resident.ProfessionId == quota.ProfessionId) == quota.Count,
        $"profession quota is exact for {quota.ProfessionId}");
}

static string Snapshot(StationRoster value)
{
    var residents = value.Residents.Select(resident =>
        string.Join(
            "|",
            new[]
            {
                resident.Id.ToString("N"),
                resident.Name,
                resident.Age.ToString(CultureInfo.InvariantCulture),
                resident.ProfessionId,
                resident.Personality.Curiosity.ToString(CultureInfo.InvariantCulture),
                resident.Personality.Sociability.ToString(CultureInfo.InvariantCulture),
                resident.Personality.Caution.ToString(CultureInfo.InvariantCulture),
                resident.Personality.Empathy.ToString(CultureInfo.InvariantCulture),
                resident.Personality.Discipline.ToString(CultureInfo.InvariantCulture),
                string.Join(",", resident.Habits),
                string.Join(",", resident.Traits)
            }));

    return string.Join("\n", residents) +
           $"\nplayer={value.PlayerResidentId:N}\nparasite={value.ParasiteResidentId:N}";
}

var snapshot = Snapshot(roster);
var snapshotHash = Convert.ToHexString(
    SHA256.HashData(Encoding.UTF8.GetBytes(snapshot))).ToLowerInvariant();

Check(snapshot == Snapshot(repeatedRoster), "the complete roster is reproducible for a fixed seed and configuration");
Check(snapshotHash == "ceb0c2ec3677af20bef6a671758021887f94d9425d8417ac8d242d26fc4c5715",
    "the complete roster matches the cross-runtime golden snapshot for the fixed seed");
Check(snapshot != Snapshot(differentSeedRoster), "a different seed changes the generated roster");

var readOnlyResidents = roster.Residents;
Check(readOnlyResidents is ReadOnlyCollection<Resident>, "roster residents are exposed through a read-only wrapper");
Expect<NotSupportedException>(
    () => ((ICollection<Resident>)readOnlyResidents).Add(roster.Residents[0]),
    "callers cannot append to the roster through an ICollection cast");

var originalArray = roster.Residents.ToArray();
var snapshotRoster = new StationRoster(originalArray, roster.PlayerResidentId, roster.ParasiteResidentId);
originalArray[0] = originalArray[1];
Check(snapshotRoster.Residents[0].Id != snapshotRoster.Residents[1].Id, "roster construction takes an isolated snapshot");

Expect<ArgumentException>(
    () => new StationRoster(roster.Residents.Take(11), roster.PlayerResidentId, roster.ParasiteResidentId),
    "a roster with eleven residents is rejected");

var duplicateResidents = roster.Residents.ToArray();
duplicateResidents[1] = duplicateResidents[0];
Expect<ArgumentException>(
    () => new StationRoster(duplicateResidents, roster.PlayerResidentId, roster.ParasiteResidentId),
    "duplicate resident IDs are rejected");

Expect<ArgumentException>(
    () => new StationRoster(roster.Residents, roster.PlayerResidentId, roster.PlayerResidentId),
    "the player cannot also be the parasite");

Expect<ArgumentOutOfRangeException>(
    () => new ResidentPersonality(101, 50, 50, 50, 50),
    "personality values above one hundred are rejected");

Expect<ArgumentException>(
    () => new Resident(
        Guid.NewGuid(),
        "Duplicate habit",
        30,
        "test-profession-alpha",
        habits: [ResidentHabit.DrinksCoffee, ResidentHabit.DrinksCoffee]),
    "duplicate habits are rejected");

Expect<ArgumentException>(
    () => new Resident(
        Guid.NewGuid(),
        "Duplicate trait",
        30,
        "test-profession-alpha",
        traits: [ResidentTrait.Observant, ResidentTrait.Observant]),
    "duplicate individual traits are rejected");

Expect<ArgumentException>(
    () => generator.Generate(seed, null!),
    "resident generation requires an explicit profession configuration");

var extremeSeedRoster = generator.Generate(long.MaxValue, configuration);
Check(extremeSeedRoster.Residents.Count == 12, "maximum Int64 seed generates a valid roster");
var minimumSeedRoster = generator.Generate(long.MinValue, configuration);
Check(minimumSeedRoster.Residents.Count == 12, "minimum Int64 seed generates a valid roster");

Console.WriteLine($"All resident-core checks passed ({checks} assertions).");
