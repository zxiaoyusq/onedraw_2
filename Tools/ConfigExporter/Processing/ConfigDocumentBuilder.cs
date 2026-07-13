using System.Globalization;
using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Processing;

internal sealed class ConfigDocumentBuilder
{
    private readonly SchemaContractValidator _schemaValidator;

    public ConfigDocumentBuilder(SchemaContractValidator schemaValidator)
    {
        _schemaValidator = schemaValidator;
    }

    public ConfigDocument Build(RawWorkbook workbook, string? schemaPath)
    {
        ValidateWorkbookSheetOrder(workbook);

        var rawFieldDictionary = workbook.GetRequiredSheet(ConfigContract.FieldDictionarySheetName);
        ValidateHeaders(
            rawFieldDictionary,
            ConfigContract.FieldDictionaryFields.Select(field => field.FieldName).ToArray());
        var fieldDictionaryRows = SortFieldDictionaryRows(
            ParseRows(rawFieldDictionary, ConfigContract.FieldDictionaryFields),
            workbook);
        var fieldsBySheet = BuildFieldDefinitions(fieldDictionaryRows);

        foreach (var contract in ConfigContract.DataSheets.Where(
                     contract => !string.Equals(
                         contract.SheetName,
                         ConfigContract.FieldDictionarySheetName,
                         StringComparison.Ordinal)))
        {
            var rawSheet = workbook.GetRequiredSheet(contract.SheetName);
            if (!fieldsBySheet.TryGetValue(contract.SheetName, out var definitions))
            {
                throw new ConfigExportException(
                    "CFG002",
                    "FieldDictionary does not describe this sheet.",
                    contract.SheetName,
                    row: 4);
            }

            ValidateHeaders(rawSheet, definitions.Select(field => field.FieldName).ToArray());
        }

        var unexpectedDictionarySheets = fieldsBySheet.Keys
            .Except(
                ConfigContract.DataSheets
                    .Where(contract => !string.Equals(
                        contract.SheetName,
                        ConfigContract.FieldDictionarySheetName,
                        StringComparison.Ordinal))
                    .Select(contract => contract.SheetName),
                StringComparer.Ordinal)
            .ToArray();
        if (unexpectedDictionarySheets.Length > 0)
        {
            throw new ConfigExportException(
                "CFG002",
                $"FieldDictionary contains unknown sheet names: {string.Join(", ", unexpectedDictionarySheets)}.",
                ConfigContract.FieldDictionarySheetName);
        }

        var tables = new List<ConfigTable>(ConfigContract.DataSheets.Count);
        foreach (var contract in ConfigContract.DataSheets)
        {
            if (string.Equals(contract.SheetName, ConfigContract.FieldDictionarySheetName, StringComparison.Ordinal))
            {
                tables.Add(new ConfigTable(
                    contract,
                    ConfigContract.FieldDictionaryFields.Select(field => field.FieldName).ToArray(),
                    fieldDictionaryRows));
                continue;
            }

            var definitions = fieldsBySheet[contract.SheetName];
            var rows = ParseRows(workbook.GetRequiredSheet(contract.SheetName), definitions);
            tables.Add(new ConfigTable(
                contract,
                definitions.Select(field => field.FieldName).ToArray(),
                SortRows(rows, contract.SortFields)));
        }

        var global = tables.Single(table => string.Equals(table.Contract.SheetName, "Global", StringComparison.Ordinal));
        var schemaVersion = GetSingleGlobalValue<long>(global, "config_schema_version", "intValue");
        var contentVersion = GetSingleGlobalValue<string>(global, "content_version", "stringValue");
        var document = new ConfigDocument(schemaVersion, contentVersion, tables);

        if (!string.IsNullOrWhiteSpace(schemaPath))
        {
            _schemaValidator.Validate(schemaPath, workbook, schemaVersion);
        }

        return document;
    }

    private static void ValidateWorkbookSheetOrder(RawWorkbook workbook)
    {
        var actual = workbook.Sheets.Select(sheet => sheet.Name).ToArray();
        if (actual.SequenceEqual(ConfigContract.WorkbookSheetOrder, StringComparer.Ordinal))
        {
            return;
        }

        throw new ConfigExportException(
            "CFG001",
            $"Sheet names/order mismatch. Expected [{string.Join(", ", ConfigContract.WorkbookSheetOrder)}], " +
            $"actual [{string.Join(", ", actual)}].");
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<FieldDefinition>> BuildFieldDefinitions(
        IReadOnlyList<ConfigRow> fieldDictionaryRows)
    {
        var result = new Dictionary<string, List<FieldDefinition>>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in fieldDictionaryRows)
        {
            var sheetName = RequireString(row, "sheet");
            var fieldName = RequireString(row, "field");
            var uniqueKey = $"{sheetName}\u001f{fieldName}";
            if (!seen.Add(uniqueKey))
            {
                throw new ConfigExportException(
                    "CFG002",
                    "FieldDictionary contains a duplicate sheet/field entry.",
                    ConfigContract.FieldDictionarySheetName,
                    row.ExcelRowNumber,
                    "field");
            }

            if (string.Equals(sheetName, ConfigContract.FieldDictionarySheetName, StringComparison.Ordinal))
            {
                throw new ConfigExportException(
                    "CFG002",
                    "FieldDictionary must not recursively describe itself.",
                    ConfigContract.FieldDictionarySheetName,
                    row.ExcelRowNumber,
                    "sheet");
            }

            if (!result.TryGetValue(sheetName, out var definitions))
            {
                definitions = new List<FieldDefinition>();
                result.Add(sheetName, definitions);
            }

            definitions.Add(new FieldDefinition(
                sheetName,
                fieldName,
                ParseKind(RequireString(row, "type"), row.ExcelRowNumber),
                ParseRequired(RequireString(row, "required"), row.ExcelRowNumber),
                definitions.Count,
                row.ExcelRowNumber));
        }

        return result.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<FieldDefinition>)pair.Value,
            StringComparer.Ordinal);
    }

    private static IReadOnlyList<ConfigRow> ParseRows(
        RawSheet sheet,
        IReadOnlyList<FieldDefinition> definitions)
    {
        var fieldOrder = definitions.Select(definition => definition.FieldName).ToArray();
        var result = new List<ConfigRow>(sheet.Rows.Count);
        foreach (var rawRow in sheet.Rows)
        {
            if (rawRow.Cells.Count != definitions.Count)
            {
                throw new ConfigExportException(
                    "CFG002",
                    $"Row has {rawRow.Cells.Count} cells but {definitions.Count} headers are declared.",
                    sheet.Name,
                    rawRow.ExcelRowNumber);
            }

            var values = new Dictionary<string, object?>(definitions.Count, StringComparer.Ordinal);
            for (var index = 0; index < definitions.Count; index += 1)
            {
                var definition = definitions[index];
                values.Add(
                    definition.FieldName,
                    ParseValue(rawRow.Cells[index], definition, sheet.Name, rawRow.ExcelRowNumber));
            }

            result.Add(new ConfigRow(rawRow.ExcelRowNumber, fieldOrder, values));
        }

        return result;
    }

    private static object? ParseValue(
        string? rawValue,
        FieldDefinition definition,
        string sheetName,
        uint rowNumber)
    {
        var value = rawValue?.Trim() ?? string.Empty;
        if (definition.Kind == ConfigValueKind.String)
        {
            return value;
        }

        if (value.Length == 0)
        {
            return null;
        }

        switch (definition.Kind)
        {
            case ConfigValueKind.Integer:
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                {
                    return integer;
                }

                break;
            case ConfigValueKind.Float:
                if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    return number;
                }

                break;
            case ConfigValueKind.Boolean:
                if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1")
                {
                    return true;
                }

                if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) || value == "0")
                {
                    return false;
                }

                break;
            default:
                throw new InvalidOperationException($"Unsupported value kind {definition.Kind}.");
        }

        throw new ConfigExportException(
            "CFG004",
            $"Value '{value}' cannot be parsed as {definition.Kind} using InvariantCulture.",
            sheetName,
            rowNumber,
            definition.FieldName);
    }

    private static IReadOnlyList<ConfigRow> SortRows(
        IReadOnlyList<ConfigRow> rows,
        IReadOnlyList<string> sortFields)
    {
        if (sortFields.Count == 0)
        {
            return rows;
        }

        return rows.OrderBy(row => row, new ConfigRowComparer(sortFields)).ToArray();
    }

    private static IReadOnlyList<ConfigRow> SortFieldDictionaryRows(
        IReadOnlyList<ConfigRow> rows,
        RawWorkbook workbook)
    {
        var sheetRanks = ConfigContract.DataSheets
            .Where(contract => !string.Equals(
                contract.SheetName,
                ConfigContract.FieldDictionarySheetName,
                StringComparison.Ordinal))
            .Select((contract, index) => (contract.SheetName, Index: index))
            .ToDictionary(pair => pair.SheetName, pair => pair.Index, StringComparer.Ordinal);
        var fieldRanks = sheetRanks.Keys.ToDictionary(
            sheetName => sheetName,
            sheetName => (IReadOnlyDictionary<string, int>)workbook.GetRequiredSheet(sheetName).Headers
                .Select((fieldName, index) => (FieldName: fieldName, Index: index))
                .ToDictionary(pair => pair.FieldName, pair => pair.Index, StringComparer.Ordinal),
            StringComparer.Ordinal);

        foreach (var row in rows)
        {
            var sheetName = RequireString(row, "sheet");
            if (!sheetRanks.ContainsKey(sheetName))
            {
                throw new ConfigExportException(
                    "CFG002",
                    $"FieldDictionary contains unknown sheet '{sheetName}'.",
                    ConfigContract.FieldDictionarySheetName,
                    row.ExcelRowNumber,
                    "sheet");
            }
        }

        foreach (var sheetName in sheetRanks.Keys)
        {
            var declaredFields = rows
                .Where(row => string.Equals(RequireString(row, "sheet"), sheetName, StringComparison.Ordinal))
                .Select(row => RequireString(row, "field"))
                .ToArray();
            var headers = workbook.GetRequiredSheet(sheetName).Headers;
            var missingHeader = declaredFields.Except(headers, StringComparer.Ordinal).FirstOrDefault();
            var unexpectedHeader = headers.Except(declaredFields, StringComparer.Ordinal).FirstOrDefault();
            if (missingHeader is not null || unexpectedHeader is not null)
            {
                throw new ConfigExportException(
                    "CFG002",
                    $"Header mismatch. Expected '{missingHeader ?? "<none>"}', " +
                    $"actual '{unexpectedHeader ?? "<missing>"}'.",
                    sheetName,
                    row: 4,
                    field: missingHeader);
            }
        }

        return rows
            .OrderBy(row => sheetRanks[RequireString(row, "sheet")])
            .ThenBy(row => fieldRanks[RequireString(row, "sheet")][RequireString(row, "field")])
            .ToArray();
    }

    private static void ValidateHeaders(RawSheet sheet, IReadOnlyList<string> expected)
    {
        if (sheet.Headers.SequenceEqual(expected, StringComparer.Ordinal))
        {
            return;
        }

        var mismatchIndex = Enumerable.Range(0, Math.Max(sheet.Headers.Count, expected.Count))
            .FirstOrDefault(index =>
                index >= sheet.Headers.Count ||
                index >= expected.Count ||
                !string.Equals(sheet.Headers[index], expected[index], StringComparison.Ordinal));
        var actualValue = mismatchIndex < sheet.Headers.Count ? sheet.Headers[mismatchIndex] : "<missing>";
        var expectedValue = mismatchIndex < expected.Count ? expected[mismatchIndex] : "<none>";
        throw new ConfigExportException(
            "CFG002",
            $"Header mismatch at column {mismatchIndex + 1}. Expected '{expectedValue}', actual '{actualValue}'.",
            sheet.Name,
            row: 4,
            field: expectedValue);
    }

    private static T GetSingleGlobalValue<T>(ConfigTable global, string key, string valueField)
    {
        var rows = global.Rows
            .Where(row => string.Equals(row.GetValue("key") as string, key, StringComparison.Ordinal))
            .ToArray();
        if (rows.Length != 1 || rows[0].GetValue(valueField) is not T value)
        {
            throw new ConfigExportException(
                "CFG011",
                $"Global key '{key}' must appear exactly once with a {typeof(T).Name} value in '{valueField}'.",
                "Global",
                rows.FirstOrDefault()?.ExcelRowNumber,
                valueField);
        }

        return value;
    }

    private static string RequireString(ConfigRow row, string fieldName)
    {
        if (row.GetValue(fieldName) is string value && value.Length > 0)
        {
            return value;
        }

        throw new ConfigExportException(
            "CFG002",
            "FieldDictionary contract metadata cannot be empty.",
            ConfigContract.FieldDictionarySheetName,
            row.ExcelRowNumber,
            fieldName);
    }

    private static ConfigValueKind ParseKind(string value, uint rowNumber)
    {
        return value switch
        {
            "string" => ConfigValueKind.String,
            "int" => ConfigValueKind.Integer,
            "float" => ConfigValueKind.Float,
            "bool" => ConfigValueKind.Boolean,
            _ => throw new ConfigExportException(
                "CFG002",
                $"Unknown FieldDictionary type '{value}'.",
                ConfigContract.FieldDictionarySheetName,
                rowNumber,
                "type"),
        };
    }

    private static bool ParseRequired(string value, uint rowNumber)
    {
        return value switch
        {
            "true" => true,
            "false" => false,
            _ => throw new ConfigExportException(
                "CFG002",
                $"FieldDictionary required must be 'true' or 'false', actual '{value}'.",
                ConfigContract.FieldDictionarySheetName,
                rowNumber,
                "required"),
        };
    }

    private sealed class ConfigRowComparer : IComparer<ConfigRow>
    {
        private readonly IReadOnlyList<string> _sortFields;

        public ConfigRowComparer(IReadOnlyList<string> sortFields)
        {
            _sortFields = sortFields;
        }

        public int Compare(ConfigRow? left, ConfigRow? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            foreach (var field in _sortFields)
            {
                var result = CompareValues(left.GetValue(field), right.GetValue(field));
                if (result != 0)
                {
                    return result;
                }
            }

            foreach (var field in left.FieldOrder)
            {
                var result = CompareValues(left.GetValue(field), right.GetValue(field));
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }

        private static int CompareValues(object? left, object? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            return (left, right) switch
            {
                (string leftString, string rightString) => StringComparer.Ordinal.Compare(leftString, rightString),
                (long leftInteger, long rightInteger) => leftInteger.CompareTo(rightInteger),
                (decimal leftNumber, decimal rightNumber) => leftNumber.CompareTo(rightNumber),
                (bool leftBoolean, bool rightBoolean) => leftBoolean.CompareTo(rightBoolean),
                _ => throw new InvalidOperationException(
                    $"Cannot compare {left.GetType().Name} with {right.GetType().Name}."),
            };
        }
    }
}
