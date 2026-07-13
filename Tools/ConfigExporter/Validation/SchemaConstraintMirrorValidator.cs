using System.Text.Json;
using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Validation;

internal static class SchemaConstraintMirrorValidator
{
    public static void Validate(ConfigValidationContext context, string? schemaPath)
    {
        if (string.IsNullOrWhiteSpace(schemaPath))
        {
            return;
        }

        try
        {
            using var schema = JsonDocument.Parse(File.ReadAllBytes(Path.GetFullPath(schemaPath)));
            var definitions = schema.RootElement.GetProperty("$defs");
            foreach (var constraint in context.Fields.Values)
            {
                var property = definitions
                    .GetProperty($"{constraint.SheetName}Row")
                    .GetProperty("properties")
                    .GetProperty(constraint.FieldName);
                ValidateTypes(constraint, property);
                ValidateBound(constraint, property, minimum: true);
                ValidateBound(constraint, property, minimum: false);
                ValidateEnum(context, constraint, property);
            }

            ValidateFieldDictionaryDefinition(definitions.GetProperty("FieldDictionaryRow"));
        }
        catch (ConfigExportException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is JsonException or KeyNotFoundException or InvalidOperationException or IOException)
        {
            throw new ConfigExportException(
                "CFG002",
                $"Schema value contract cannot be read: {exception.Message}",
                innerException: exception);
        }
    }

    private static void ValidateTypes(ConfigFieldConstraint constraint, JsonElement property)
    {
        var expected = ExpectedTypes(constraint.Kind, constraint.Required);
        var actual = ReadTypes(property);
        if (!expected.SetEquals(actual))
        {
            throw Failure(
                constraint,
                "type",
                $"Schema types [{string.Join(", ", actual.Order(StringComparer.Ordinal))}] do not match " +
                $"FieldDictionary types [{string.Join(", ", expected.Order(StringComparer.Ordinal))}].");
        }
    }

    private static void ValidateBound(
        ConfigFieldConstraint constraint,
        JsonElement property,
        bool minimum)
    {
        var schemaName = minimum ? "minimum" : "maximum";
        var dictionaryName = minimum ? "min" : "max";
        var expected = minimum ? constraint.Minimum : constraint.Maximum;
        decimal? actual = property.TryGetProperty(schemaName, out var bound)
            ? bound.GetDecimal()
            : null;
        if (expected != actual)
        {
            throw Failure(
                constraint,
                dictionaryName,
                $"Schema {schemaName} '{Display(actual)}' does not match " +
                $"FieldDictionary {dictionaryName} '{Display(expected)}'.");
        }
    }

    private static void ValidateEnum(
        ConfigValidationContext context,
        ConfigFieldConstraint constraint,
        JsonElement property)
    {
        var actual = ReadStringEnum(property);
        var expected = constraint.EnumType.Length == 0
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(context.EnumValues[constraint.EnumType], StringComparer.Ordinal);
        if (!expected.SetEquals(actual))
        {
            throw Failure(
                constraint,
                "enumType",
                $"Schema enum [{string.Join(", ", actual.Order(StringComparer.Ordinal))}] does not match " +
                $"Enums.{constraint.EnumType} [{string.Join(", ", expected.Order(StringComparer.Ordinal))}].");
        }
    }

    private static void ValidateFieldDictionaryDefinition(JsonElement definition)
    {
        var properties = definition.GetProperty("properties");
        foreach (var field in ConfigContract.FieldDictionaryFields)
        {
            var actual = ReadTypes(properties.GetProperty(field.FieldName));
            var expected = ExpectedTypes(field.Kind, field.Required);
            if (!expected.SetEquals(actual))
            {
                throw new ConfigExportException(
                    "CFG002",
                    $"FieldDictionaryRow.{field.FieldName} schema types do not match the exporter contract.",
                    ConfigContract.FieldDictionarySheetName,
                    row: 4,
                    field.FieldName);
            }
        }

        RequireStaticEnum(properties, "type", new HashSet<string>(
            new[] { "string", "int", "float", "bool" },
            StringComparer.Ordinal));
        RequireStaticEnum(properties, "required", new HashSet<string>(
            new[] { "true", "false" },
            StringComparer.Ordinal));
    }

    private static void RequireStaticEnum(
        JsonElement properties,
        string fieldName,
        IReadOnlySet<string> expected)
    {
        var actual = ReadStringEnum(properties.GetProperty(fieldName));
        if (!expected.SetEquals(actual))
        {
            throw new ConfigExportException(
                "CFG002",
                $"FieldDictionaryRow.{fieldName} schema enum does not match the exporter contract.",
                ConfigContract.FieldDictionarySheetName,
                row: 4,
                fieldName);
        }
    }

    private static HashSet<string> ExpectedTypes(ConfigValueKind kind, bool required)
    {
        var result = new HashSet<string>(StringComparer.Ordinal)
        {
            kind switch
            {
                ConfigValueKind.String => "string",
                ConfigValueKind.Integer => "integer",
                ConfigValueKind.Float => "number",
                ConfigValueKind.Boolean => "boolean",
                _ => throw new InvalidOperationException($"Unsupported field kind {kind}."),
            },
        };
        if (!required && kind != ConfigValueKind.String)
        {
            result.Add("null");
        }

        return result;
    }

    private static HashSet<string> ReadTypes(JsonElement property)
    {
        var type = property.GetProperty("type");
        return type.ValueKind switch
        {
            JsonValueKind.String => new HashSet<string>(new[] { type.GetString()! }, StringComparer.Ordinal),
            JsonValueKind.Array => new HashSet<string>(
                type.EnumerateArray().Select(value => value.GetString()!),
                StringComparer.Ordinal),
            _ => throw new InvalidOperationException("Schema type must be a string or string array."),
        };
    }

    private static HashSet<string> ReadStringEnum(JsonElement property)
    {
        if (!property.TryGetProperty("enum", out var values))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return new HashSet<string>(
            values.EnumerateArray().Select(value => value.GetString()!),
            StringComparer.Ordinal);
    }

    private static string Display(decimal? value)
    {
        return value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "<absent>";
    }

    private static ConfigExportException Failure(
        ConfigFieldConstraint constraint,
        string field,
        string message)
    {
        return new ConfigExportException(
            "CFG002",
            message,
            ConfigContract.FieldDictionarySheetName,
            constraint.DictionaryRowNumber,
            field);
    }
}
