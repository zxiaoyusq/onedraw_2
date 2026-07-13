using System.Text.Json;
using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Processing;

internal sealed class SchemaContractValidator
{
    public void Validate(string schemaPath, RawWorkbook workbook, long schemaVersion)
    {
        var fullPath = Path.GetFullPath(schemaPath);
        if (!File.Exists(fullPath))
        {
            throw new ConfigExportException("CFG000", $"Schema file does not exist: {fullPath}");
        }

        try
        {
            using var schema = JsonDocument.Parse(File.ReadAllBytes(fullPath));
            var root = schema.RootElement;
            var rootProperties = root.GetProperty("properties");
            RequireSequence(
                rootProperties.EnumerateObject().Select(property => property.Name),
                ConfigContract.TopLevelPropertyOrder,
                "Schema root properties");
            RequireSequence(
                root.GetProperty("required").EnumerateArray().Select(item => item.GetString() ?? string.Empty),
                ConfigContract.TopLevelPropertyOrder,
                "Schema root required list");

            var schemaConst = rootProperties.GetProperty("schemaVersion").GetProperty("const").GetInt64();
            if (schemaConst != schemaVersion)
            {
                throw new ConfigExportException(
                    "CFG011",
                    $"Schema const is {schemaConst}, workbook schema version is {schemaVersion}.");
            }

            var definitions = root.GetProperty("$defs");
            foreach (var contract in ConfigContract.DataSheets)
            {
                var rawSheet = workbook.GetRequiredSheet(contract.SheetName);
                var definition = definitions.GetProperty(contract.SchemaDefinitionName);
                RequireSequence(
                    definition.GetProperty("properties").EnumerateObject().Select(property => property.Name),
                    rawSheet.Headers,
                    $"{contract.SchemaDefinitionName}.properties",
                    contract.SheetName);
                RequireSequence(
                    definition.GetProperty("required").EnumerateArray().Select(item => item.GetString() ?? string.Empty),
                    rawSheet.Headers,
                    $"{contract.SchemaDefinitionName}.required",
                    contract.SheetName);
            }
        }
        catch (ConfigExportException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new ConfigExportException(
                "CFG002",
                $"Schema contract cannot be read: {exception.Message}",
                innerException: exception);
        }
    }

    private static void RequireSequence(
        IEnumerable<string> actualValues,
        IEnumerable<string> expectedValues,
        string label,
        string? sheetName = null)
    {
        var actual = actualValues.ToArray();
        var expected = expectedValues.ToArray();
        if (actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            return;
        }

        throw new ConfigExportException(
            "CFG002",
            $"{label} mismatch. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
            sheetName,
            row: sheetName is null ? null : 4);
    }
}
