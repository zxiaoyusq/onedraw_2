using System.Text.RegularExpressions;
using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Validation;

internal static partial class RowConstraintValidator
{
    public static void Validate(ConfigValidationContext context)
    {
        ValidateEnumContractReferences(context);
        ValidatePrimaryKeys(context);
        ValidateRegisteredStrategyEnums(context);
        ValidateRows(context);
    }

    private static void ValidateEnumContractReferences(ConfigValidationContext context)
    {
        foreach (var row in context.Document.GetRequiredTable(ConfigContract.FieldDictionarySheetName).Rows)
        {
            var sheetName = ConfigValues.String(row, "sheet");
            var fieldName = ConfigValues.String(row, "field");
            var constraint = context.GetField(sheetName, fieldName);
            if (constraint.EnumType.Length > 0 && !context.EnumValues.ContainsKey(constraint.EnumType))
            {
                throw new ConfigExportException(
                    "CFG006",
                    $"Enum type '{constraint.EnumType}' is not declared by Enums.",
                    ConfigContract.FieldDictionarySheetName,
                    constraint.DictionaryRowNumber,
                    "enumType");
            }
        }
    }

    private static void ValidatePrimaryKeys(ConfigValidationContext context)
    {
        foreach (var table in context.Document.Tables)
        {
            var keyFields = ConfigContract.GetPrimaryKeyFields(table.Contract.SheetName);
            if (keyFields.Count == 0)
            {
                continue;
            }

            var firstRowsByKey = new Dictionary<string, ConfigRow>(StringComparer.Ordinal);
            foreach (var row in table.Rows)
            {
                var key = string.Join(
                    "\u001f",
                    keyFields.Select(field => ConfigValues.KeyPart(row.GetValue(field))));
                if (firstRowsByKey.TryAdd(key, row))
                {
                    continue;
                }

                var firstRow = firstRowsByKey[key];
                throw new ConfigExportException(
                    "CFG005",
                    $"Duplicate primary key ({string.Join(", ", keyFields)}) also appears on Excel row " +
                    $"{firstRow.ExcelRowNumber}.",
                    table.Contract.SheetName,
                    row.ExcelRowNumber,
                    keyFields[^1]);
            }
        }
    }

    private static void ValidateRegisteredStrategyEnums(ConfigValidationContext context)
    {
        foreach (var contract in ConfigContract.RegisteredStrategyEnums)
        {
            if (!context.EnumRows.TryGetValue(contract.Key, out var rows))
            {
                continue;
            }

            var actualValues = new HashSet<string>(
                rows.Select(row => ConfigValues.String(row, "value")),
                StringComparer.Ordinal);
            var unexpectedValue = actualValues.Except(contract.Value, StringComparer.Ordinal).FirstOrDefault();
            if (unexpectedValue is not null)
            {
                var row = rows.First(candidate => string.Equals(
                    ConfigValues.String(candidate, "value"),
                    unexpectedValue,
                    StringComparison.Ordinal));
                throw new ConfigExportException(
                    "CFG006",
                    $"Strategy enum '{contract.Key}.{unexpectedValue}' is not registered by code.",
                    "Enums",
                    row.ExcelRowNumber,
                    "value");
            }

            var missingValue = contract.Value.Except(actualValues, StringComparer.Ordinal).FirstOrDefault();
            if (missingValue is not null)
            {
                throw new ConfigExportException(
                    "CFG006",
                    $"Code-registered strategy enum '{contract.Key}.{missingValue}' is missing from Enums.",
                    "Enums",
                    rows[0].ExcelRowNumber,
                    "value");
            }
        }
    }

    private static void ValidateRows(ConfigValidationContext context)
    {
        foreach (var table in context.Document.Tables.Where(table => !string.Equals(
                     table.Contract.SheetName,
                     ConfigContract.FieldDictionarySheetName,
                     StringComparison.Ordinal)))
        {
            foreach (var row in table.Rows)
            {
                foreach (var fieldName in table.FieldOrder)
                {
                    var constraint = context.GetField(table.Contract.SheetName, fieldName);
                    var value = row.GetValue(fieldName);
                    if (ConfigValues.IsEmpty(value))
                    {
                        if (constraint.Required)
                        {
                            throw Failure(
                                "CFG003",
                                "Required configuration cell is empty.",
                                table,
                                row,
                                fieldName);
                        }

                        continue;
                    }

                    ValidateStableIdentifier(table, row, fieldName, value!);
                    ValidateDeclaredRange(table, row, constraint, value!);
                    ValidateSemanticRange(table, row, fieldName, value!);
                    ValidateEnum(context, table, row, constraint, value!);
                }
            }
        }
    }

    private static void ValidateStableIdentifier(
        ConfigTable table,
        ConfigRow row,
        string fieldName,
        object value)
    {
        if (value is not string text || !IsStableIdentifierField(fieldName))
        {
            return;
        }

        if (text == "*")
        {
            if (string.Equals(table.Contract.SheetName, "SpawnPoints", StringComparison.Ordinal) &&
                string.Equals(fieldName, "levelId", StringComparison.Ordinal))
            {
                return;
            }

            throw Failure(
                "CFG009",
                "Wildcard '*' is only allowed in SpawnPoints.levelId.",
                table,
                row,
                fieldName);
        }

        if (!StableIdentifierRegex().IsMatch(text))
        {
            throw Failure(
                "CFG009",
                $"Stable ID/key '{text}' must match ^[a-z][a-z0-9_]*$.",
                table,
                row,
                fieldName);
        }
    }

    private static bool IsStableIdentifierField(string fieldName)
    {
        return string.Equals(fieldName, "key", StringComparison.Ordinal) ||
            fieldName.EndsWith("Id", StringComparison.Ordinal) ||
            fieldName.EndsWith("Key", StringComparison.Ordinal);
    }

    private static void ValidateDeclaredRange(
        ConfigTable table,
        ConfigRow row,
        ConfigFieldConstraint constraint,
        object value)
    {
        if (!ConfigValues.TryNumber(value, out var number))
        {
            return;
        }

        if (constraint.Minimum.HasValue && number < constraint.Minimum.Value)
        {
            throw Failure(
                "CFG007",
                $"Value {number} is below declared minimum {constraint.Minimum.Value}.",
                table,
                row,
                constraint.FieldName);
        }

        if (constraint.Maximum.HasValue && number > constraint.Maximum.Value)
        {
            throw Failure(
                "CFG007",
                $"Value {number} exceeds declared maximum {constraint.Maximum.Value}.",
                table,
                row,
                constraint.FieldName);
        }
    }

    private static void ValidateSemanticRange(
        ConfigTable table,
        ConfigRow row,
        string fieldName,
        object value)
    {
        if (!ConfigValues.TryNumber(value, out var number))
        {
            return;
        }

        if (fieldName.EndsWith("Sec", StringComparison.Ordinal) && number < decimal.Zero)
        {
            throw Failure(
                "CFG007",
                "Time values expressed in seconds must be non-negative.",
                table,
                row,
                fieldName);
        }

        if (IsNormalizedCoordinate(fieldName) && (number < decimal.Zero || number > decimal.One))
        {
            throw Failure(
                "CFG007",
                "Normalized Safe Area coordinates must be within [0, 1].",
                table,
                row,
                fieldName);
        }
    }

    private static bool IsNormalizedCoordinate(string fieldName)
    {
        return fieldName.EndsWith("Norm", StringComparison.Ordinal) ||
            fieldName.StartsWith("normalized", StringComparison.Ordinal);
    }

    private static void ValidateEnum(
        ConfigValidationContext context,
        ConfigTable table,
        ConfigRow row,
        ConfigFieldConstraint constraint,
        object value)
    {
        if (constraint.EnumType.Length == 0)
        {
            return;
        }

        var text = value as string
            ?? throw new InvalidOperationException($"Enum field {constraint.SheetName}.{constraint.FieldName} is not text.");
        if (!context.EnumValues[constraint.EnumType].Contains(text))
        {
            throw Failure(
                "CFG006",
                $"Value '{text}' is not declared by enum '{constraint.EnumType}'.",
                table,
                row,
                constraint.FieldName);
        }
    }

    private static ConfigExportException Failure(
        string code,
        string message,
        ConfigTable table,
        ConfigRow row,
        string fieldName)
    {
        return new ConfigExportException(
            code,
            message,
            table.Contract.SheetName,
            row.ExcelRowNumber,
            fieldName);
    }

    [GeneratedRegex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdentifierRegex();
}
