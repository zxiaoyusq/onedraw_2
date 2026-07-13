using System.Globalization;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Serialization;

internal sealed record SerializedConfig(byte[] Bytes, string ContentHash);

internal sealed class StableJsonSerializer
{
    private static readonly JsonWriterOptions CompactWriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
        SkipValidation = false,
    };

    private static readonly JsonWriterOptions IndentedWriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = true,
        SkipValidation = false,
    };

    public SerializedConfig Serialize(ConfigDocument document)
    {
        var canonicalBytes = SerializeCanonicalDocument(document);
        var contentHash = Hash(canonicalBytes);
        var outputBytes = SerializeIndentedDocument(document, contentHash);
        return new SerializedConfig(outputBytes, contentHash);
    }

    public static byte[] CanonicalizeJson(ReadOnlySpan<byte> jsonBytes, bool excludeRootContentHash)
    {
        using var json = JsonDocument.Parse(jsonBytes.ToArray());
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, CompactWriterOptions))
        {
            WriteCanonicalElement(writer, json.RootElement, excludeRootContentHash, isRoot: true);
        }

        return stream.ToArray();
    }

    public static string Hash(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static byte[] SerializeCanonicalDocument(ConfigDocument document)
    {
        var rootProperties = new List<(string Name, Action<Utf8JsonWriter> WriteValue)>
        {
            ("schemaVersion", writer => writer.WriteNumberValue(document.SchemaVersion)),
            ("contentVersion", writer => writer.WriteStringValue(document.ContentVersion)),
        };
        rootProperties.AddRange(document.Tables.Select(table => (
            table.Contract.JsonPropertyName,
            (Action<Utf8JsonWriter>)(writer => WriteTable(writer, table, canonicalFields: true)))));

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, CompactWriterOptions))
        {
            writer.WriteStartObject();
            foreach (var property in rootProperties.OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                writer.WritePropertyName(property.Name);
                property.WriteValue(writer);
            }

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static byte[] SerializeIndentedDocument(ConfigDocument document, string contentHash)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, IndentedWriterOptions))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", document.SchemaVersion);
            writer.WriteString("contentVersion", document.ContentVersion);
            writer.WriteString("contentHash", contentHash);
            foreach (var table in document.Tables)
            {
                writer.WritePropertyName(table.Contract.JsonPropertyName);
                WriteTable(writer, table, canonicalFields: false);
            }

            writer.WriteEndObject();
        }

        stream.WriteByte((byte)'\n');
        return stream.ToArray();
    }

    private static void WriteTable(Utf8JsonWriter writer, ConfigTable table, bool canonicalFields)
    {
        writer.WriteStartArray();
        foreach (var row in table.Rows)
        {
            writer.WriteStartObject();
            IEnumerable<string> fields = canonicalFields
                ? row.Values.Keys.OrderBy(field => field, StringComparer.Ordinal)
                : row.FieldOrder;
            foreach (var field in fields)
            {
                writer.WritePropertyName(field);
                WriteValue(writer, row.GetValue(field));
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                return;
            case string text:
                writer.WriteStringValue(text);
                return;
            case long integer:
                writer.WriteNumberValue(integer);
                return;
            case decimal number:
                writer.WriteRawValue(FormatDecimal(number), skipInputValidation: true);
                return;
            case bool boolean:
                writer.WriteBooleanValue(boolean);
                return;
            default:
                throw new InvalidOperationException($"Unsupported JSON value type {value.GetType().FullName}.");
        }
    }

    private static void WriteCanonicalElement(
        Utf8JsonWriter writer,
        JsonElement element,
        bool excludeRootContentHash,
        bool isRoot)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject()
                             .Where(property => !(isRoot && excludeRootContentHash && property.NameEquals("contentHash")))
                             .OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalElement(writer, property.Value, excludeRootContentHash, isRoot: false);
                }

                writer.WriteEndObject();
                return;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonicalElement(writer, item, excludeRootContentHash, isRoot: false);
                }

                writer.WriteEndArray();
                return;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                return;
            case JsonValueKind.Number:
                writer.WriteRawValue(NormalizeJsonNumber(element.GetRawText()), skipInputValidation: true);
                return;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                return;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                return;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                return;
            default:
                throw new InvalidDataException($"Unsupported JSON token {element.ValueKind}.");
        }
    }

    private static string NormalizeJsonNumber(string rawValue)
    {
        if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            return integer.ToString(CultureInfo.InvariantCulture);
        }

        if (decimal.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return FormatDecimal(number);
        }

        throw new InvalidDataException($"JSON number is outside the supported deterministic decimal range: {rawValue}");
    }

    private static string FormatDecimal(decimal value)
    {
        var normalized = value == decimal.Zero ? decimal.Zero : value;
        return normalized.ToString("G29", CultureInfo.InvariantCulture);
    }
}
