using System.Text.Json;
using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Serialization;

internal sealed class OutputVerifier
{
    public void VerifyFile(string path, ConfigDocument expectedDocument, string expectedHash)
    {
        VerifyBytes(File.ReadAllBytes(path), expectedDocument, expectedHash);
    }

    public void VerifyBytes(ReadOnlySpan<byte> bytes, ConfigDocument expectedDocument, string expectedHash)
    {
        try
        {
            using var json = JsonDocument.Parse(bytes.ToArray());
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw Failure("Generated JSON root is not an object.");
            }

            var actualProperties = root.EnumerateObject().Select(property => property.Name).ToArray();
            if (!actualProperties.SequenceEqual(ConfigContract.TopLevelPropertyOrder, StringComparer.Ordinal))
            {
                throw Failure(
                    $"Generated root property order mismatch: [{string.Join(", ", actualProperties)}].");
            }

            if (root.GetProperty("schemaVersion").GetInt64() != expectedDocument.SchemaVersion)
            {
                throw Failure("Generated schemaVersion does not match the workbook.");
            }

            if (!string.Equals(
                    root.GetProperty("contentVersion").GetString(),
                    expectedDocument.ContentVersion,
                    StringComparison.Ordinal))
            {
                throw Failure("Generated contentVersion does not match the workbook.");
            }

            var declaredHash = root.GetProperty("contentHash").GetString();
            if (!string.Equals(declaredHash, expectedHash, StringComparison.Ordinal))
            {
                throw Failure($"Generated contentHash is '{declaredHash}', expected '{expectedHash}'.");
            }

            foreach (var table in expectedDocument.Tables)
            {
                var array = root.GetProperty(table.Contract.JsonPropertyName);
                if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() != table.Rows.Count)
                {
                    throw Failure(
                        $"Generated array '{table.Contract.JsonPropertyName}' has an unexpected type or record count.");
                }
            }

            var canonical = StableJsonSerializer.CanonicalizeJson(bytes, excludeRootContentHash: true);
            var calculatedHash = StableJsonSerializer.Hash(canonical);
            if (!string.Equals(calculatedHash, expectedHash, StringComparison.Ordinal))
            {
                throw Failure($"Generated content hash self-check failed: calculated '{calculatedHash}'.");
            }
        }
        catch (ConfigExportException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException or InvalidDataException)
        {
            throw Failure($"Generated JSON self-check failed: {exception.Message}", exception);
        }
    }

    private static ConfigExportException Failure(string message, Exception? innerException = null)
    {
        return new ConfigExportException("CFG012", message, innerException: innerException);
    }
}
