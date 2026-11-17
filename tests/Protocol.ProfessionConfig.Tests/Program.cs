using System.Text.Json;
using Protocol.ProfessionConfig;

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

// Synthetic test fixture only. These values are not proposed game-design quotas.
const string fixture = """
{
  "schemaVersion": 1,
  "quotas": [
    { "professionId": "medical", "count": 2 },
    { "professionId": "engineering", "count": 3 },
    { "professionId": "research", "count": 4 },
    { "professionId": "kitchen", "count": 1 },
    { "professionId": "security", "count": 2 }
  ]
}
""";

Expect<ArgumentException>(
    () => ProfessionQuotaJson.Deserialize(null!),
    "null JSON input is rejected");

Expect<ArgumentException>(
    () => ProfessionQuotaJson.Deserialize(" \r\n\t "),
    "whitespace-only JSON input is rejected");

var normalizedQuota = new ProfessionQuota("  medical-role  ", 12);
Check(normalizedQuota.ProfessionId == "medical-role",
    "profession ids are trimmed at the domain boundary");

var configuration = ProfessionQuotaJson.Deserialize(fixture);
Check(configuration.Quotas.Count == 5,
    "all explicitly configured profession quotas are loaded");
Check(configuration.Quotas.Sum(quota => quota.Count) == 12,
    "configuration total is exactly twelve");

var allocator = new ProfessionQuotaAllocator();
var allocation = allocator.Allocate(configuration, seed: 417);
Check(allocation.Count == 12,
    "allocator always produces twelve profession assignments");
Check(allocation.SequenceEqual(new[]
    {
        "research", "research", "engineering", "engineering",
        "research", "security", "kitchen", "security",
        "medical", "engineering", "medical", "research"
    }),
    "fixed seed produces a stable cross-runtime allocation sequence");

var counts = allocation
    .GroupBy(profession => profession, StringComparer.OrdinalIgnoreCase)
    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
foreach (var quota in configuration.Quotas)
{
    Check(counts.TryGetValue(quota.ProfessionId, out var actual) &&
          actual == quota.Count,
        $"allocated count matches quota for {quota.ProfessionId}");
}

Check(allocator.Allocate(configuration, seed: 417).SequenceEqual(allocation),
    "repeating a seed and configuration reproduces the same allocation");
Check(!allocator.Allocate(configuration, seed: 418).SequenceEqual(allocation),
    "a different seed changes the shuffled assignment order for this fixture");

var serialized = ProfessionQuotaJson.Serialize(configuration);
var roundTripped = ProfessionQuotaJson.Deserialize(serialized);
Check(roundTripped.Quotas.SequenceEqual(configuration.Quotas),
    "JSON serialization round-trips the complete quota configuration");

var twelveDistinctQuotas = Enumerable.Range(1, 12)
    .Select(index => new ProfessionQuota($"profession-{index}", 1))
    .ToArray();
var maximumEntryConfiguration = new ProfessionQuotaConfiguration(twelveDistinctQuotas);
var maximumEntryAllocation = allocator.Allocate(maximumEntryConfiguration, seed: 12);
Check(maximumEntryConfiguration.Quotas.Count == 12 &&
      maximumEntryAllocation.Count == 12 &&
      maximumEntryAllocation.Distinct(StringComparer.Ordinal).Count() == 12,
    "twelve distinct one-resident quotas are supported without losing assignments");

var escapedIdConfiguration = new ProfessionQuotaConfiguration(new[]
{
    new ProfessionQuota("research \"north\" / Δ", 12)
});
var escapedIdRoundTrip = ProfessionQuotaJson.Deserialize(
    ProfessionQuotaJson.Serialize(escapedIdConfiguration));
Check(escapedIdRoundTrip.Quotas.Single().ProfessionId == "research \"north\" / Δ",
    "JSON serialization safely round-trips quoted and Unicode profession ids");

var mutableSource = new List<ProfessionQuota>
{
    new("alpha", 6),
    new("beta", 6)
};
var snapshotted = new ProfessionQuotaConfiguration(mutableSource);
mutableSource.Clear();
Check(snapshotted.Quotas.Count == 2 &&
      snapshotted.Quotas.Sum(quota => quota.Count) == 12,
    "configuration snapshots the caller's collection");

var quotaView = (IList<ProfessionQuota>)configuration.Quotas;
Check(quotaView.IsReadOnly, "quota collection is exposed as read-only");
Expect<NotSupportedException>(
    () => quotaView[0] = new ProfessionQuota("tampered", 12),
    "callers cannot mutate the configuration through its read-only view");

var assignmentView = (IList<string>)allocation;
Check(assignmentView.IsReadOnly, "allocation is exposed as read-only");
Expect<NotSupportedException>(
    () => assignmentView[0] = "tampered",
    "callers cannot mutate the allocated assignments through their read-only view");

Expect<ArgumentNullException>(
    () => new ProfessionQuotaConfiguration(null!),
    "null quota collections are rejected");

Expect<ArgumentException>(
    () => new ProfessionQuotaConfiguration(new ProfessionQuota[]
    {
        null!,
        new("alpha", 12)
    }),
    "null entries inside a quota collection are rejected");

Expect<ArgumentException>(
    () => new ProfessionQuotaConfiguration(new[]
    {
        new ProfessionQuota("alpha", 5),
        new ProfessionQuota("beta", 6)
    }),
    "configuration rejects a total below twelve");

Expect<ArgumentException>(
    () => new ProfessionQuotaConfiguration(new[]
    {
        new ProfessionQuota("alpha", 7),
        new ProfessionQuota("beta", 6)
    }),
    "configuration rejects a total above twelve");

Expect<ArgumentException>(
    () => new ProfessionQuotaConfiguration(new[]
    {
        new ProfessionQuota("Medical", 6),
        new ProfessionQuota("medical", 6)
    }),
    "configuration rejects duplicate profession ids case-insensitively");

Expect<ArgumentException>(
    () => new ProfessionQuotaConfiguration(new[]
    {
        new ProfessionQuota("alpha ", 6),
        new ProfessionQuota(" ALPHA", 6)
    }),
    "duplicate ids are detected after trimming");

Expect<ArgumentException>(
    () => new ProfessionQuotaConfiguration(new[]
    {
        new ProfessionQuota("alpha", int.MaxValue),
        new ProfessionQuota("beta", int.MaxValue)
    }),
    "quota totals are accumulated without 32-bit integer overflow");

Expect<ArgumentOutOfRangeException>(
    () => new ProfessionQuota("alpha", 0),
    "a quota cannot be zero");

Expect<ArgumentOutOfRangeException>(
    () => new ProfessionQuota("alpha", -1),
    "a quota cannot be negative");

Expect<ArgumentException>(
    () => new ProfessionQuota("  ", 12),
    "a profession id cannot be blank");

Expect<ArgumentException>(
    () => new ProfessionQuotaConfiguration(Array.Empty<ProfessionQuota>()),
    "an empty quota configuration is rejected");

Expect<ArgumentException>(
    () => ProfessionQuotaJson.Deserialize("""{"schemaVersion":1,"quotas":[]}"""),
    "JSON with an empty quota list is rejected by domain validation");

Expect<NotSupportedException>(
    () => ProfessionQuotaJson.Deserialize("""{"schemaVersion":2,"quotas":[]}"""),
    "unknown schema versions are rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize("[]"),
    "a non-object JSON root is rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize("""{"quotas":[]}"""),
    "a missing schemaVersion property is rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize("""{"schemaVersion":1}"""),
    "a missing quotas property is rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize("""{"schemaVersion":"1","quotas":[]}"""),
    "a string schemaVersion is rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize("""{"schemaVersion":1.5,"quotas":[]}"""),
    "a fractional schemaVersion is rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize("""{"schemaVersion":1,"quotas":{}}"""),
    "a non-array quotas property is rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize("""{"schemaVersion":1,"quotas":[null]}"""),
    "a non-object quota entry is rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize(
        """{"schemaVersion":1,"quotas":[{"professionId":12,"count":12}]}"""),
    "a non-string profession id is rejected");

Expect<ArgumentException>(
    () => ProfessionQuotaJson.Deserialize(
        """{"schemaVersion":1,"quotas":[{"professionId":"   ","count":12}]}"""),
    "a whitespace-only profession id is rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize(
        """{"schemaVersion":1,"quotas":[{"professionId":"alpha","count":1.5}]}"""),
    "a fractional quota count is rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize(
        """{"schemaVersion":1,"quotas":[{"professionId":"alpha","count":2147483648}]}"""),
    "a quota count outside Int32 range is rejected");

Expect<ArgumentOutOfRangeException>(
    () => ProfessionQuotaJson.Deserialize(
        """{"schemaVersion":1,"quotas":[{"professionId":"alpha","count":0}]}"""),
    "a zero quota count is rejected during deserialization");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize(
        """{"schemaVersion":1,"quotas":[],"quotaz":[]}"""),
    "unknown JSON properties are rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize(
        """{"schemaVersion":1,"schemaVersion":1,"quotas":[]}"""),
    "duplicate JSON properties are rejected");

Expect<FormatException>(
    () => ProfessionQuotaJson.Deserialize(
        """{"schemaVersion":1,"quotas":[{"professionId":"alpha","count":12,"counts":12}]}"""),
    "unknown fields in quota entries are rejected");

Expect<JsonException>(
    () => ProfessionQuotaJson.Deserialize("{"),
    "malformed JSON is rejected by the JSON parser");

Expect<ArgumentNullException>(
    () => allocator.Allocate(null!, seed: 0),
    "the allocator rejects a null configuration");

var minSeedAllocation = allocator.Allocate(configuration, long.MinValue);
var maxSeedAllocation = allocator.Allocate(configuration, long.MaxValue);
Check(minSeedAllocation.Count == 12 && maxSeedAllocation.Count == 12,
    "extreme signed 64-bit seeds still produce twelve assignments");
Check(allocator.Allocate(configuration, long.MinValue).SequenceEqual(minSeedAllocation) &&
      allocator.Allocate(configuration, long.MaxValue).SequenceEqual(maxSeedAllocation),
    "extreme signed 64-bit seeds are reproducible");

for (var seed = -256; seed <= 256; seed++)
{
    var generated = allocator.Allocate(configuration, seed);
    Check(generated.Count == 12,
        $"seed {seed} produces exactly twelve assignments");

    var generatedCounts = generated
        .GroupBy(profession => profession, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

    foreach (var quota in configuration.Quotas)
    {
        Check(generatedCounts.TryGetValue(quota.ProfessionId, out var actual) &&
              actual == quota.Count,
            $"seed {seed} preserves the quota for {quota.ProfessionId}");
    }
}

var composer = new ProfessionRosterComposer(allocator);
var unassignedResidents = Enumerable.Range(1, 12)
    .Select(index => new ResidentDraft(
        Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}"),
        $"Resident {index}",
        "unassigned"))
    .ToArray();

var composedRoster = composer.Compose(
    unassignedResidents,
    configuration,
    seed: 417,
    static (resident, professionId) => resident with { ProfessionId = professionId });

Check(composedRoster.Count == 12,
    "roster composer returns exactly twelve residents");

var composedCounts = composedRoster
    .GroupBy(resident => resident.ProfessionId, StringComparer.OrdinalIgnoreCase)
    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
foreach (var quota in configuration.Quotas)
{
    Check(composedCounts.TryGetValue(quota.ProfessionId, out var actual) &&
          actual == quota.Count,
        $"composed roster exactly matches the quota for {quota.ProfessionId}");
}

Check(composedRoster.Select(resident => resident.Id)
        .SequenceEqual(unassignedResidents.Select(resident => resident.Id)) &&
      composedRoster.Select(resident => resident.Name)
        .SequenceEqual(unassignedResidents.Select(resident => resident.Name)),
    "roster composition preserves resident identity and input order");

Check(unassignedResidents.All(resident => resident.ProfessionId == "unassigned"),
    "roster composition does not mutate the caller's resident objects");

var repeatedRoster = composer.Compose(
    unassignedResidents,
    configuration,
    seed: 417,
    static (resident, professionId) => resident with { ProfessionId = professionId });
Check(composedRoster.SequenceEqual(repeatedRoster),
    "the same seed reproduces the complete composed roster");

var alternateRoster = composer.Compose(
    unassignedResidents,
    configuration,
    seed: 418,
    static (resident, professionId) => resident with { ProfessionId = professionId });
Check(!composedRoster.Select(resident => resident.ProfessionId)
        .SequenceEqual(alternateRoster.Select(resident => resident.ProfessionId)),
    "a different seed changes profession placement without changing quotas");

var composedView = (IList<ResidentDraft>)composedRoster;
Check(composedView.IsReadOnly,
    "composed roster is exposed as a read-only collection");
Expect<NotSupportedException>(
    () => composedView[0] = unassignedResidents[0],
    "callers cannot replace residents through the composed roster view");

Expect<ArgumentException>(
    () => composer.Compose(
        unassignedResidents.Take(11).ToArray(),
        configuration,
        seed: 417,
        static (resident, professionId) => resident with { ProfessionId = professionId }),
    "roster composer rejects eleven residents");

Expect<ArgumentException>(
    () => composer.Compose(
        unassignedResidents.Append(unassignedResidents[0]).ToArray(),
        configuration,
        seed: 417,
        static (resident, professionId) => resident with { ProfessionId = professionId }),
    "roster composer rejects thirteen residents");

Expect<ArgumentNullException>(
    () => composer.Compose<ResidentDraft>(
        null!,
        configuration,
        seed: 417,
        static (resident, professionId) => resident with { ProfessionId = professionId }),
    "roster composer rejects a null resident collection");

var rosterWithNullEntry = unassignedResidents.ToArray();
rosterWithNullEntry[1] = null!;
Expect<ArgumentException>(
    () => composer.Compose(
        rosterWithNullEntry,
        configuration,
        seed: 417,
        static (resident, professionId) => resident with { ProfessionId = professionId }),
    "roster composer rejects null resident entries");

Expect<ArgumentNullException>(
    () => composer.Compose(
        unassignedResidents,
        configuration,
        seed: 417,
        null!),
    "roster composer rejects a null assignment callback");

Expect<InvalidOperationException>(
    () => composer.Compose<ResidentDraft>(
        unassignedResidents,
        configuration,
        seed: 417,
        static (_, _) => null!),
    "roster composer rejects a callback that returns null");

var callbackInvocationCount = 0;
_ = composer.Compose(
    unassignedResidents,
    configuration,
    seed: 417,
    (resident, professionId) =>
    {
        callbackInvocationCount++;
        return resident with { ProfessionId = professionId };
    });
Check(callbackInvocationCount == 12,
    "assignment callback is invoked exactly once for each validated resident");

var sourceArray = unassignedResidents.ToArray();
var firstSourceId = sourceArray[0].Id;
var snapshotIsolatedRoster = composer.Compose(
    sourceArray,
    configuration,
    seed: 417,
    (resident, professionId) =>
    {
        if (resident.Id == firstSourceId)
            sourceArray[0] = sourceArray[1];

        return resident with { ProfessionId = professionId };
    });
Check(snapshotIsolatedRoster[0].Id == firstSourceId,
    "composition uses a complete input snapshot even if the source collection changes in the callback");

var inconsistentShortList = new InconsistentReadOnlyList<ResidentDraft>(
    unassignedResidents.Take(11).ToArray(),
    reportedCount: 12);
var invalidListCallbackCalls = 0;
Expect<ArgumentException>(
    () => composer.Compose(
        inconsistentShortList,
        configuration,
        seed: 417,
        (resident, professionId) =>
        {
            invalidListCallbackCalls++;
            return resident with { ProfessionId = professionId };
        }),
    "roster composer rejects a collection whose enumerator yields fewer items than Count");
Check(invalidListCallbackCalls == 0,
    "assignment callback is not invoked for an under-enumerated collection");

var inconsistentLongList = new InconsistentReadOnlyList<ResidentDraft>(
    unassignedResidents.Append(unassignedResidents[0]).ToArray(),
    reportedCount: 12);
Expect<ArgumentException>(
    () => composer.Compose(
        inconsistentLongList,
        configuration,
        seed: 417,
        static (resident, professionId) => resident with { ProfessionId = professionId }),
    "roster composer rejects a collection whose enumerator yields more items than Count");

var inconsistentDeclaredCount = new InconsistentReadOnlyList<ResidentDraft>(
    unassignedResidents,
    reportedCount: 11);
Expect<ArgumentException>(
    () => composer.Compose(
        inconsistentDeclaredCount,
        configuration,
        seed: 417,
        static (resident, professionId) => resident with { ProfessionId = professionId }),
    "roster composer rejects a collection whose Count is not twelve even if it enumerates twelve");

Console.WriteLine("All profession quota checks passed, including roster composition and collection-boundary checks.");

sealed record ResidentDraft(Guid Id, string Name, string ProfessionId);

sealed class InconsistentReadOnlyList<T>(
    IReadOnlyList<T> items,
    int reportedCount) : IReadOnlyList<T>
{
    public int Count => reportedCount;

    public T this[int index] => items[index];

    public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
