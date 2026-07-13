using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Validation;

internal sealed record ConfigFieldConstraint(
    string SheetName,
    string FieldName,
    ConfigValueKind Kind,
    bool Required,
    decimal? Minimum,
    decimal? Maximum,
    string EnumType,
    string ForeignKey,
    uint DictionaryRowNumber);

internal static class FieldConstraintCatalog
{
    public static IReadOnlyDictionary<(string Sheet, string Field), ConfigFieldConstraint> Build(
        ConfigDocument document)
    {
        var result = new Dictionary<(string Sheet, string Field), ConfigFieldConstraint>();
        foreach (var row in document.GetRequiredTable(ConfigContract.FieldDictionarySheetName).Rows)
        {
            var sheetName = ConfigValues.String(row, "sheet");
            var fieldName = ConfigValues.String(row, "field");
            var kind = ParseKind(ConfigValues.String(row, "type"), row);
            var required = ParseRequired(ConfigValues.String(row, "required"), row);
            var minimum = row.GetValue("min") as decimal?;
            var maximum = row.GetValue("max") as decimal?;
            var enumType = ConfigValues.String(row, "enumType");
            var foreignKey = ConfigValues.String(row, "foreignKey");
            var description = ConfigValues.String(row, "description");

            if (description.Length == 0)
            {
                throw Failure(
                    "CFG003",
                    "FieldDictionary description is required.",
                    row,
                    "description");
            }

            if ((minimum.HasValue || maximum.HasValue) &&
                kind is not (ConfigValueKind.Integer or ConfigValueKind.Float))
            {
                throw Failure(
                    "CFG002",
                    "Only int and float fields may declare min/max constraints.",
                    row,
                    minimum.HasValue ? "min" : "max");
            }

            if (minimum.HasValue && maximum.HasValue && minimum.Value > maximum.Value)
            {
                throw Failure(
                    "CFG009",
                    $"Declared minimum {minimum.Value} exceeds maximum {maximum.Value}.",
                    row,
                    "min");
            }

            if (enumType.Length > 0 && kind != ConfigValueKind.String)
            {
                throw Failure(
                    "CFG002",
                    "Only string fields may reference an enumType.",
                    row,
                    "enumType");
            }

            if (foreignKey.Length > 0 && kind != ConfigValueKind.String)
            {
                throw Failure(
                    "CFG002",
                    "Only string fields may declare a foreignKey.",
                    row,
                    "foreignKey");
            }

            var constraint = new ConfigFieldConstraint(
                sheetName,
                fieldName,
                kind,
                required,
                minimum,
                maximum,
                enumType,
                foreignKey,
                row.ExcelRowNumber);
            if (!result.TryAdd((sheetName, fieldName), constraint))
            {
                throw Failure(
                    "CFG005",
                    "FieldDictionary contains a duplicate sheet/field contract.",
                    row,
                    "field");
            }
        }

        return result;
    }

    private static ConfigValueKind ParseKind(string value, ConfigRow row)
    {
        return value switch
        {
            "string" => ConfigValueKind.String,
            "int" => ConfigValueKind.Integer,
            "float" => ConfigValueKind.Float,
            "bool" => ConfigValueKind.Boolean,
            _ => throw Failure("CFG002", $"Unknown field type '{value}'.", row, "type"),
        };
    }

    private static bool ParseRequired(string value, ConfigRow row)
    {
        return value switch
        {
            "true" => true,
            "false" => false,
            _ => throw Failure(
                "CFG002",
                $"FieldDictionary required must be 'true' or 'false', actual '{value}'.",
                row,
                "required"),
        };
    }

    private static ConfigExportException Failure(
        string code,
        string message,
        ConfigRow row,
        string field)
    {
        return new ConfigExportException(
            code,
            message,
            ConfigContract.FieldDictionarySheetName,
            row.ExcelRowNumber,
            field);
    }
}
