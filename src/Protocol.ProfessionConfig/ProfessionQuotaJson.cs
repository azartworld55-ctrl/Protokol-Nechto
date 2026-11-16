using System.Text;
using System.Text.Json;

namespace Protocol.ProfessionConfig;

/// <summary>
/// Strict JSON reader/writer for the small, versioned profession quota file.
/// Unknown fields and duplicate property names are rejected to catch typos.
/// </summary>
public static class ProfessionQuotaJson
{
    public const int CurrentSchemaVersion = 1;

    public static ProfessionQuotaConfiguration Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Profession quota JSON is required.", nameof(json));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        EnsureObject(root, "Configuration");
        EnsureUniqueAndAllowedProperties(root, "schemaVersion", "quotas");

        var versionElement = GetRequiredProperty(root, "schemaVersion");
        if (versionElement.ValueKind != JsonValueKind.Number ||
            !versionElement.TryGetInt32(out var version))
            throw new FormatException("schemaVersion must be an integer.");

        if (version != CurrentSchemaVersion)
            throw new NotSupportedException(
                $"Profession quota schema version {version} is not supported.");

        var quotasElement = GetRequiredProperty(root, "quotas");
        if (quotasElement.ValueKind != JsonValueKind.Array)
            throw new FormatException("quotas must be an array.");

        var quotas = new List<ProfessionQuota>();
        foreach (var quotaElement in quotasElement.EnumerateArray())
        {
            EnsureObject(quotaElement, "Quota entry");
            EnsureUniqueAndAllowedProperties(quotaElement, "professionId", "count");

            var idElement = GetRequiredProperty(quotaElement, "professionId");
            if (idElement.ValueKind != JsonValueKind.String)
                throw new FormatException("professionId must be a string.");

            var countElement = GetRequiredProperty(quotaElement, "count");
            if (countElement.ValueKind != JsonValueKind.Number ||
                !countElement.TryGetInt32(out var count))
                throw new FormatException("count must be an integer.");

            quotas.Add(new ProfessionQuota(idElement.GetString() ?? string.Empty, count));
        }

        return new ProfessionQuotaConfiguration(quotas);
    }

    public static string Serialize(ProfessionQuotaConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(
                   stream,
                   new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", CurrentSchemaVersion);
            writer.WriteStartArray("quotas");

            foreach (var quota in configuration.Quotas)
            {
                writer.WriteStartObject();
                writer.WriteString("professionId", quota.ProfessionId);
                writer.WriteNumber("count", quota.Count);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void EnsureObject(JsonElement element, string description)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new FormatException($"{description} must be a JSON object.");
    }

    private static void EnsureUniqueAndAllowedProperties(
        JsonElement element,
        params string[] allowedNames)
    {
        var allowed = allowedNames.ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in element.EnumerateObject())
        {
            if (!seen.Add(property.Name))
                throw new FormatException($"Duplicate JSON property '{property.Name}'.");

            if (!allowed.Contains(property.Name))
                throw new FormatException($"Unknown JSON property '{property.Name}'.");
        }
    }

    private static JsonElement GetRequiredProperty(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            throw new FormatException($"Required JSON property '{name}' is missing.");

        return value;
    }
}
