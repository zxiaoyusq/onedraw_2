using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace OneStrokeDemon.ConfigExporter.Tests;

/// <summary>
/// 锁定工作簿面向策划的中文可读性，不让英文API表头或字段契约说明退化为无语义占位文案。
/// </summary>
public sealed class WorkbookDocumentationTests
{
    private static readonly Regex ChineseText = new("[\\u3400-\\u9fff]", RegexOptions.CultureInvariant);
    private static readonly Regex PlaceholderDescription = new(
        "^[A-Za-z0-9_]+ 表的 [A-Za-z0-9_]+ 字段[。]?$",
        RegexOptions.CultureInvariant);
    private static readonly Regex VagueDescription = new(
        "代码与配置共享|稳定枚举值|取值必须来自[A-Za-z0-9_]+枚举[。]?$|取值来自[A-Za-z0-9_]+枚举[。]?$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// 验证每个Sheet都有中文用途说明，且每个第4行英文API字段在第3行拥有同列中文名称。
    /// </summary>
    [Fact]
    public void EverySheetAndHeaderHasVisibleChineseDocumentation()
    {
        var repository = TestRepository.Find();
        using var document = SpreadsheetDocument.Open(repository.WorkbookPath, isEditable: false);
        WorkbookPart workbookPart = document.WorkbookPart
            ?? throw new InvalidDataException("Workbook part is missing.");
        Workbook workbook = workbookPart.Workbook
            ?? throw new InvalidDataException("Workbook metadata is missing.");
        IReadOnlyList<string> sharedStrings = ReadSharedStrings(workbookPart);
        Sheet[] sheets = workbook.Sheets?.Elements<Sheet>().ToArray()
            ?? Array.Empty<Sheet>();

        Assert.Equal(31, sheets.Length);
        int documentedHeaderCount = 0;
        foreach (Sheet sheet in sheets)
        {
            string sheetName = sheet.Name?.Value
                ?? throw new InvalidDataException("Sheet name is missing.");
            WorksheetPart worksheetPart = GetWorksheetPart(workbookPart, sheet);
            IReadOnlyDictionary<int, string> purpose = ReadRow(
                worksheetPart,
                sharedStrings,
                rowIndex: 2);
            Assert.True(
                purpose.TryGetValue(0, out string? purposeText) && ChineseText.IsMatch(purposeText),
                $"{sheetName}!A2 must contain a Chinese purpose description.");

            if (string.Equals(sheetName, "README", StringComparison.Ordinal))
            {
                continue;
            }

            IReadOnlyDictionary<int, string> labels = ReadRow(
                worksheetPart,
                sharedStrings,
                rowIndex: 3);
            IReadOnlyDictionary<int, string> headers = ReadRow(
                worksheetPart,
                sharedStrings,
                rowIndex: 4);
            foreach ((int column, string header) in headers.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)))
            {
                Assert.True(
                    labels.TryGetValue(column, out string? label) && ChineseText.IsMatch(label),
                    $"{sheetName}.{header} must have a same-column Chinese label in row 3.");
                documentedHeaderCount++;
            }
        }

        Assert.Equal(291, documentedHeaderCount);
    }

    /// <summary>
    /// 验证281个业务字段说明均与可见中文名称一致，并包含实际用途而非模板占位句。
    /// </summary>
    [Fact]
    public void FieldDictionaryDescriptionsAreSpecificAndMatchVisibleLabels()
    {
        var repository = TestRepository.Find();
        using var document = SpreadsheetDocument.Open(repository.WorkbookPath, isEditable: false);
        WorkbookPart workbookPart = document.WorkbookPart
            ?? throw new InvalidDataException("Workbook part is missing.");
        Workbook workbook = workbookPart.Workbook
            ?? throw new InvalidDataException("Workbook metadata is missing.");
        IReadOnlyList<string> sharedStrings = ReadSharedStrings(workbookPart);
        Dictionary<string, Sheet> sheets = (workbook.Sheets?.Elements<Sheet>()
                ?? Enumerable.Empty<Sheet>())
            .ToDictionary(
                sheet => sheet.Name?.Value
                    ?? throw new InvalidDataException("Sheet name is missing."),
                StringComparer.Ordinal);
        WorksheetPart dictionaryPart = GetWorksheetPart(workbookPart, sheets["FieldDictionary"]);
        Worksheet dictionaryWorksheet = dictionaryPart.Worksheet
            ?? throw new InvalidDataException("FieldDictionary worksheet is missing.");
        SheetData sheetData = dictionaryWorksheet.GetFirstChild<SheetData>()
            ?? throw new InvalidDataException("FieldDictionary sheet data is missing.");

        int documentedFieldCount = 0;
        foreach (Row row in sheetData.Elements<Row>().Where(item => item.RowIndex?.Value >= 5))
        {
            IReadOnlyDictionary<int, string> values = ReadRow(row, sharedStrings);
            if (!values.TryGetValue(0, out string? sheetName) || string.IsNullOrWhiteSpace(sheetName))
            {
                continue;
            }

            string field = values[1];
            string description = values[9];
            Assert.True(ChineseText.IsMatch(description), $"{sheetName}.{field} description must contain Chinese.");
            Assert.False(
                PlaceholderDescription.IsMatch(description),
                $"{sheetName}.{field} still uses a placeholder description: {description}");
            Assert.False(
                VagueDescription.IsMatch(description),
                $"{sheetName}.{field} still uses a vague description: {description}");

            WorksheetPart targetPart = GetWorksheetPart(workbookPart, sheets[sheetName]);
            IReadOnlyDictionary<int, string> headers = ReadRow(targetPart, sharedStrings, rowIndex: 4);
            int column = headers.Single(pair => string.Equals(pair.Value, field, StringComparison.Ordinal)).Key;
            IReadOnlyDictionary<int, string> labels = ReadRow(targetPart, sharedStrings, rowIndex: 3);
            string label = labels[column];
            Assert.StartsWith($"{label}：", description, StringComparison.Ordinal);
            documentedFieldCount++;
        }

        Assert.Equal(281, documentedFieldCount);
    }

    /// <summary>
    /// 验证枚举字段逐值说明业务效果，并锁定当前已知“仅传递未消费”等实现边界，避免文档把预期写成已实现。
    /// </summary>
    [Fact]
    public void EnumFieldsAndValuesExplainBusinessBehaviorAndRuntimeLimits()
    {
        var repository = TestRepository.Find();
        using var document = SpreadsheetDocument.Open(repository.WorkbookPath, isEditable: false);
        WorkbookPart workbookPart = document.WorkbookPart
            ?? throw new InvalidDataException("Workbook part is missing.");
        Workbook workbook = workbookPart.Workbook
            ?? throw new InvalidDataException("Workbook metadata is missing.");
        IReadOnlyList<string> sharedStrings = ReadSharedStrings(workbookPart);
        Dictionary<string, Sheet> sheets = (workbook.Sheets?.Elements<Sheet>()
                ?? Enumerable.Empty<Sheet>())
            .ToDictionary(
                sheet => sheet.Name?.Value
                    ?? throw new InvalidDataException("Sheet name is missing."),
                StringComparer.Ordinal);

        WorksheetPart enumsPart = GetWorksheetPart(workbookPart, sheets["Enums"]);
        SheetData enumsData = enumsPart.Worksheet?.GetFirstChild<SheetData>()
            ?? throw new InvalidDataException("Enums sheet data is missing.");
        var valuesByGroup = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        int documentedEnumCount = 0;
        foreach (Row row in enumsData.Elements<Row>().Where(item => item.RowIndex?.Value >= 5))
        {
            IReadOnlyDictionary<int, string> values = ReadRow(row, sharedStrings);
            if (!values.TryGetValue(0, out string? group) || string.IsNullOrWhiteSpace(group))
            {
                continue;
            }

            string value = values[1];
            string description = values[3];
            Assert.True(ChineseText.IsMatch(description), $"{group}.{value} must contain Chinese business meaning.");
            Assert.False(VagueDescription.IsMatch(description), $"{group}.{value} is still vague: {description}");
            Assert.NotEqual(value, description);
            if (!valuesByGroup.TryGetValue(group, out List<string>? groupValues))
            {
                groupValues = new List<string>();
                valuesByGroup.Add(group, groupValues);
            }

            groupValues.Add(value);
            documentedEnumCount++;
        }

        Assert.Equal(98, documentedEnumCount);

        WorksheetPart dictionaryPart = GetWorksheetPart(workbookPart, sheets["FieldDictionary"]);
        SheetData dictionaryData = dictionaryPart.Worksheet?.GetFirstChild<SheetData>()
            ?? throw new InvalidDataException("FieldDictionary sheet data is missing.");
        var descriptions = new Dictionary<string, string>(StringComparer.Ordinal);
        int enumFieldCount = 0;
        foreach (Row row in dictionaryData.Elements<Row>().Where(item => item.RowIndex?.Value >= 5))
        {
            IReadOnlyDictionary<int, string> values = ReadRow(row, sharedStrings);
            if (!values.TryGetValue(0, out string? sheetName) || string.IsNullOrWhiteSpace(sheetName))
            {
                continue;
            }

            string field = values[1];
            string description = values[9];
            descriptions.Add($"{sheetName}.{field}", description);
            if (!values.TryGetValue(7, out string? enumGroup) || string.IsNullOrWhiteSpace(enumGroup))
            {
                continue;
            }

            Assert.True(valuesByGroup.TryGetValue(enumGroup, out List<string>? enumValues),
                $"{sheetName}.{field} references missing enum group {enumGroup}.");
            foreach (string enumValue in enumValues!)
            {
                Assert.Contains($"{enumValue}=", description, StringComparison.Ordinal);
            }

            enumFieldCount++;
        }

        Assert.Equal(25, enumFieldCount);
        Assert.Contains("以哪个业务事件作为startDelaySec的计时基准", descriptions["Waves.startTrigger"]);
        Assert.Contains("不是本波总生成数", descriptions["Waves.maxAlive"]);
        Assert.Contains("单独修改不会自动改变飞行、碰撞层或移动", descriptions["SpawnPoints.lane"]);
        Assert.Contains("DamageOverTime尚无周期扣血执行器", descriptions["Buffs.type"]);
        Assert.Contains("单独修改不会改变伤害", descriptions["Enemies.stanceVulnerability"]);
        Assert.Contains("尚未按该模式采样曲线路径", descriptions["Projectiles.movePatternId"]);
    }

    // 读取共享字符串表，兼容artifact-tool导出的标准OpenXML字符串单元格。
    private static IReadOnlyList<string> ReadSharedStrings(WorkbookPart workbookPart)
    {
        SharedStringTable? table = workbookPart.SharedStringTablePart?.SharedStringTable;
        return table?
            .Elements<SharedStringItem>()
            .Select(item => item.InnerText)
            .ToArray()
            ?? Array.Empty<string>();
    }

    // 将Sheet关系解析为实际WorksheetPart，并对损坏工作簿给出明确失败。
    private static WorksheetPart GetWorksheetPart(WorkbookPart workbookPart, Sheet sheet)
    {
        string relationshipId = sheet.Id?.Value
            ?? throw new InvalidDataException("Sheet relationship id is missing.");
        return workbookPart.GetPartById(relationshipId) as WorksheetPart
            ?? throw new InvalidDataException($"Worksheet part is missing for {sheet.Name?.Value}.");
    }

    // 按Excel行号读取非空单元格，返回零基列号到文本值的稳定映射。
    private static IReadOnlyDictionary<int, string> ReadRow(
        WorksheetPart worksheetPart,
        IReadOnlyList<string> sharedStrings,
        uint rowIndex)
    {
        Worksheet worksheet = worksheetPart.Worksheet
            ?? throw new InvalidDataException("Worksheet metadata is missing.");
        SheetData sheetData = worksheet.GetFirstChild<SheetData>()
            ?? throw new InvalidDataException("Worksheet sheet data is missing.");
        Row row = sheetData.Elements<Row>().Single(item => item.RowIndex?.Value == rowIndex);
        return ReadRow(row, sharedStrings);
    }

    // 读取单行全部文本，保留稀疏列位置以便核对第3行与第4行的同列关系。
    private static IReadOnlyDictionary<int, string> ReadRow(
        Row row,
        IReadOnlyList<string> sharedStrings)
    {
        var values = new Dictionary<int, string>();
        foreach (Cell cell in row.Elements<Cell>())
        {
            string reference = cell.CellReference?.Value
                ?? throw new InvalidDataException("Cell reference is missing.");
            values[ColumnIndex(reference)] = ReadCellValue(cell, sharedStrings);
        }

        return values;
    }

    // 解析共享字符串、内联字符串和普通值，不执行或修改任何工作簿公式。
    private static string ReadCellValue(Cell cell, IReadOnlyList<string> sharedStrings)
    {
        if (cell.DataType?.Value == CellValues.SharedString)
        {
            string rawIndex = cell.CellValue?.Text
                ?? throw new InvalidDataException("Shared string index is missing.");
            return sharedStrings[int.Parse(rawIndex, System.Globalization.CultureInfo.InvariantCulture)];
        }

        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString?.InnerText ?? string.Empty;
        }

        return cell.CellValue?.Text ?? cell.InnerText;
    }

    // 把A、Z、AA等Excel列名转换为零基列号。
    private static int ColumnIndex(string reference)
    {
        int index = 0;
        foreach (char character in reference)
        {
            if (character is < 'A' or > 'Z')
            {
                break;
            }

            index = checked((index * 26) + character - 'A' + 1);
        }

        return index - 1;
    }
}
