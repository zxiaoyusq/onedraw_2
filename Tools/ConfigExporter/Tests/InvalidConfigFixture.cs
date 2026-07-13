using System.Text.Json;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Tests;

internal sealed record InvalidConfigFixture(IReadOnlyList<InvalidConfigCase> Cases)
{
    public static InvalidConfigFixture Load(string path)
    {
        return JsonSerializer.Deserialize<InvalidConfigFixture>(
                   File.ReadAllBytes(path),
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException($"Invalid empty config fixture: {path}");
    }
}

internal sealed record InvalidConfigCase(
    string Id,
    string ExpectedCode,
    string ExpectedSheet,
    string ExpectedField,
    IReadOnlyList<WorkbookMutation> Mutations);

internal sealed record WorkbookMutation(
    string Operation,
    string Sheet,
    IReadOnlyDictionary<string, string> Match,
    IReadOnlyDictionary<string, string>? Values);

internal static class RawWorkbookMutator
{
    public static RawWorkbook Apply(RawWorkbook source, InvalidConfigCase testCase)
    {
        var sheets = source.Sheets.ToDictionary(sheet => sheet.Name, Clone, StringComparer.Ordinal);
        foreach (var mutation in testCase.Mutations)
        {
            if (!sheets.TryGetValue(mutation.Sheet, out var sheet))
            {
                throw new InvalidDataException($"Fixture {testCase.Id} names unknown sheet {mutation.Sheet}.");
            }

            sheets[mutation.Sheet] = mutation.Operation switch
            {
                "set" => SetValues(testCase.Id, sheet, mutation),
                "remove" => RemoveRows(testCase.Id, sheet, mutation),
                "copy" => CopyRow(testCase.Id, sheet, mutation),
                _ => throw new InvalidDataException(
                    $"Fixture {testCase.Id} uses unknown operation {mutation.Operation}."),
            };
        }

        return new RawWorkbook(source.Sheets.Select(sheet => sheets[sheet.Name]).ToArray());
    }

    private static RawSheet SetValues(string caseId, RawSheet sheet, WorkbookMutation mutation)
    {
        var matchingRows = MatchingRows(sheet, mutation.Match).ToArray();
        if (matchingRows.Length != 1)
        {
            throw new InvalidDataException(
                $"Fixture {caseId} set expected one {sheet.Name} row, found {matchingRows.Length}.");
        }

        var values = mutation.Values
            ?? throw new InvalidDataException($"Fixture {caseId} set operation requires values.");
        var rows = sheet.Rows.Select(row => ReferenceEquals(row, matchingRows[0])
                ? ReplaceValues(caseId, sheet, row, values)
                : row)
            .ToArray();
        return sheet with { Rows = rows };
    }

    private static RawSheet RemoveRows(string caseId, RawSheet sheet, WorkbookMutation mutation)
    {
        var matchingRows = MatchingRows(sheet, mutation.Match).ToHashSet();
        if (matchingRows.Count == 0)
        {
            throw new InvalidDataException($"Fixture {caseId} remove matched no {sheet.Name} rows.");
        }

        return sheet with { Rows = sheet.Rows.Where(row => !matchingRows.Contains(row)).ToArray() };
    }

    private static RawSheet CopyRow(string caseId, RawSheet sheet, WorkbookMutation mutation)
    {
        var matchingRows = MatchingRows(sheet, mutation.Match).ToArray();
        if (matchingRows.Length != 1)
        {
            throw new InvalidDataException(
                $"Fixture {caseId} copy expected one {sheet.Name} row, found {matchingRows.Length}.");
        }

        var values = mutation.Values
            ?? throw new InvalidDataException($"Fixture {caseId} copy operation requires values.");
        var nextRowNumber = sheet.Rows.Select(row => row.ExcelRowNumber).DefaultIfEmpty(4U).Max() + 1U;
        var copied = ReplaceValues(
            caseId,
            sheet,
            matchingRows[0] with { ExcelRowNumber = nextRowNumber },
            values);
        return sheet with { Rows = sheet.Rows.Append(copied).ToArray() };
    }

    private static IEnumerable<RawRow> MatchingRows(
        RawSheet sheet,
        IReadOnlyDictionary<string, string> match)
    {
        var indexes = match.ToDictionary(
            pair => HeaderIndex(sheet, pair.Key),
            pair => pair.Value);
        return sheet.Rows.Where(row => indexes.All(pair => string.Equals(
            row.Cells[pair.Key]?.Trim() ?? string.Empty,
            pair.Value,
            StringComparison.Ordinal)));
    }

    private static RawRow ReplaceValues(
        string caseId,
        RawSheet sheet,
        RawRow row,
        IReadOnlyDictionary<string, string> values)
    {
        var cells = row.Cells.ToArray();
        foreach (var pair in values)
        {
            var index = HeaderIndex(sheet, pair.Key);
            if (index >= cells.Length)
            {
                throw new InvalidDataException(
                    $"Fixture {caseId} row {row.ExcelRowNumber} has no cell for {sheet.Name}.{pair.Key}.");
            }

            cells[index] = pair.Value;
        }

        return row with { Cells = cells };
    }

    private static int HeaderIndex(RawSheet sheet, string fieldName)
    {
        for (var index = 0; index < sheet.Headers.Count; index += 1)
        {
            if (string.Equals(sheet.Headers[index], fieldName, StringComparison.Ordinal))
            {
                return index;
            }
        }

        throw new InvalidDataException($"Unknown fixture field {sheet.Name}.{fieldName}.");
    }

    private static RawSheet Clone(RawSheet sheet)
    {
        return new RawSheet(
            sheet.Name,
            sheet.Headers.ToArray(),
            sheet.Rows.Select(row => new RawRow(row.ExcelRowNumber, row.Cells.ToArray())).ToArray());
    }
}
