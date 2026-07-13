using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Excel;

internal sealed class OpenXmlWorkbookReader
{
    public RawWorkbook Read(string inputPath)
    {
        var fullPath = Path.GetFullPath(inputPath);
        if (!File.Exists(fullPath))
        {
            throw new ConfigExportException("CFG000", $"Input workbook does not exist: {fullPath}");
        }

        try
        {
            using var document = SpreadsheetDocument.Open(fullPath, isEditable: false);
            var workbookPart = document.WorkbookPart
                ?? throw new ConfigExportException("CFG001", "Workbook part is missing.");
            var workbook = workbookPart.Workbook
                ?? throw new ConfigExportException("CFG001", "Workbook metadata is missing.");
            var sheets = workbook.Sheets?.Elements<Sheet>().ToArray()
                ?? Array.Empty<Sheet>();
            var sharedStrings = ReadSharedStrings(workbookPart);
            var rawSheets = new List<RawSheet>(sheets.Length);

            foreach (var sheet in sheets)
            {
                var sheetName = sheet.Name?.Value;
                var relationshipId = sheet.Id?.Value;
                if (string.IsNullOrEmpty(sheetName) || string.IsNullOrEmpty(relationshipId))
                {
                    throw new ConfigExportException("CFG001", "Workbook contains a sheet without name or relationship id.");
                }

                var worksheetPart = workbookPart.GetPartById(relationshipId) as WorksheetPart
                    ?? throw new ConfigExportException("CFG001", "Worksheet part is missing.", sheetName);
                rawSheets.Add(string.Equals(sheetName, ConfigContract.ReadmeSheetName, StringComparison.Ordinal)
                    ? new RawSheet(sheetName, Array.Empty<string>(), Array.Empty<RawRow>())
                    : ReadSheet(sheetName, worksheetPart, sharedStrings));
            }

            return new RawWorkbook(rawSheets);
        }
        catch (ConfigExportException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            throw new ConfigExportException("CFG000", $"Unable to read workbook: {fullPath}", innerException: exception);
        }
    }

    private static RawSheet ReadSheet(
        string sheetName,
        WorksheetPart worksheetPart,
        IReadOnlyList<string> sharedStrings)
    {
        var worksheet = worksheetPart.Worksheet
            ?? throw new ConfigExportException("CFG001", "Worksheet metadata is missing.", sheetName);
        var sheetData = worksheet.GetFirstChild<SheetData>()
            ?? throw new ConfigExportException("CFG001", "Sheet data is missing.", sheetName);
        var rows = sheetData.Elements<Row>().ToArray();
        var headerRow = rows.FirstOrDefault(row => row.RowIndex?.Value == 4)
            ?? throw new ConfigExportException("CFG002", "Header row 4 is missing.", sheetName, row: 4);
        var headerCells = ReadCellMap(headerRow, sharedStrings, sheetName);
        var lastHeaderIndex = headerCells
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => pair.Key)
            .DefaultIfEmpty(-1)
            .Max();
        if (lastHeaderIndex < 0)
        {
            throw new ConfigExportException("CFG002", "Header row 4 is empty.", sheetName, row: 4);
        }

        var headers = Enumerable.Range(0, lastHeaderIndex + 1)
            .Select(index => headerCells.TryGetValue(index, out var value) ? value.Trim() : string.Empty)
            .ToArray();
        var dataRows = new List<RawRow>();

        foreach (var row in rows.Where(row => row.RowIndex?.Value >= 5))
        {
            var rowNumber = row.RowIndex!.Value;
            var cellMap = ReadCellMap(row, sharedStrings, sheetName);
            var extraCell = cellMap.FirstOrDefault(pair => pair.Key >= headers.Length && !string.IsNullOrWhiteSpace(pair.Value));
            if (!extraCell.Equals(default(KeyValuePair<int, string>)))
            {
                throw new ConfigExportException(
                    "CFG002",
                    "Data exists beyond the declared header range.",
                    sheetName,
                    rowNumber,
                    ColumnName(extraCell.Key));
            }

            var values = Enumerable.Range(0, headers.Length)
                .Select(index => cellMap.TryGetValue(index, out var value) ? value : null)
                .ToArray();
            if (string.IsNullOrWhiteSpace(values[0]))
            {
                if (values.Any(value => !string.IsNullOrWhiteSpace(value)))
                {
                    throw new ConfigExportException(
                        "CFG003",
                        "A row with an empty first field contains other data.",
                        sheetName,
                        rowNumber,
                        headers[0]);
                }

                continue;
            }

            dataRows.Add(new RawRow(rowNumber, values));
        }

        return new RawSheet(sheetName, headers, dataRows);
    }

    private static Dictionary<int, string> ReadCellMap(
        Row row,
        IReadOnlyList<string> sharedStrings,
        string sheetName)
    {
        var result = new Dictionary<int, string>();
        foreach (var cell in row.Elements<Cell>())
        {
            var reference = cell.CellReference?.Value;
            if (string.IsNullOrEmpty(reference))
            {
                throw new ConfigExportException(
                    "CFG004",
                    "Cell reference is missing.",
                    sheetName,
                    row.RowIndex?.Value);
            }

            var columnIndex = ColumnIndex(reference);
            result[columnIndex] = ReadCellValue(cell, sharedStrings, sheetName, row.RowIndex?.Value);
        }

        return result;
    }

    private static string ReadCellValue(
        Cell cell,
        IReadOnlyList<string> sharedStrings,
        string sheetName,
        uint? rowNumber)
    {
        var dataType = cell.DataType?.Value;
        if (dataType == CellValues.Error)
        {
            throw new ConfigExportException(
                "CFG004",
                $"Excel error cell cannot be exported: {cell.CellValue?.InnerText}",
                sheetName,
                rowNumber,
                cell.CellReference?.Value);
        }

        if (dataType == CellValues.SharedString)
        {
            var rawIndex = cell.CellValue?.InnerText;
            if (!int.TryParse(rawIndex, out var index) || index < 0 || index >= sharedStrings.Count)
            {
                throw new ConfigExportException(
                    "CFG004",
                    $"Shared string index is invalid: {rawIndex}",
                    sheetName,
                    rowNumber,
                    cell.CellReference?.Value);
            }

            return sharedStrings[index];
        }

        if (dataType == CellValues.InlineString)
        {
            return cell.InlineString?.InnerText ?? string.Empty;
        }

        return cell.CellValue?.InnerText ?? string.Empty;
    }

    private static IReadOnlyList<string> ReadSharedStrings(WorkbookPart workbookPart)
    {
        var table = workbookPart.SharedStringTablePart?.SharedStringTable;
        return table is null
            ? Array.Empty<string>()
            : table.Elements<SharedStringItem>().Select(item => item.InnerText).ToArray();
    }

    private static int ColumnIndex(string cellReference)
    {
        var index = 0;
        var sawLetter = false;
        foreach (var character in cellReference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            sawLetter = true;
            index = checked((index * 26) + (char.ToUpperInvariant(character) - 'A' + 1));
        }

        if (!sawLetter)
        {
            throw new FormatException($"Invalid cell reference '{cellReference}'.");
        }

        return index - 1;
    }

    private static string ColumnName(int zeroBasedColumnIndex)
    {
        var value = zeroBasedColumnIndex + 1;
        var result = string.Empty;
        while (value > 0)
        {
            var remainder = (value - 1) % 26;
            result = (char)('A' + remainder) + result;
            value = (value - 1) / 26;
        }

        return result;
    }
}
