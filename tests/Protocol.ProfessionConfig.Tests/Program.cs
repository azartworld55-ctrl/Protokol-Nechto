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

Console.WriteLine("All profession quota checks passed.");
