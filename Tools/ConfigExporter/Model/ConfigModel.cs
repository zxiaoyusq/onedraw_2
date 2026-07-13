namespace OneStrokeDemon.ConfigExporter.Model;

internal sealed record RawWorkbook(IReadOnlyList<RawSheet> Sheets)
{
    public RawSheet GetRequiredSheet(string name)
    {
        return Sheets.FirstOrDefault(sheet => string.Equals(sheet.Name, name, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Raw workbook does not contain sheet '{name}'.");
    }
}

internal sealed record RawSheet(
    string Name,
    IReadOnlyList<string> Headers,
    IReadOnlyList<RawRow> Rows);

internal sealed record RawRow(
    uint ExcelRowNumber,
    IReadOnlyList<string?> Cells);

internal enum ConfigValueKind
{
    String,
    Integer,
    Float,
    Boolean,
}

internal sealed record FieldDefinition(
    string SheetName,
    string FieldName,
    ConfigValueKind Kind,
    bool Required,
    int Order,
    uint DictionaryRowNumber);

internal sealed class ConfigRow
{
    public ConfigRow(
        uint excelRowNumber,
        IReadOnlyList<string> fieldOrder,
        IReadOnlyDictionary<string, object?> values)
    {
        ExcelRowNumber = excelRowNumber;
        FieldOrder = fieldOrder;
        Values = values;
    }

    public uint ExcelRowNumber { get; }

    public IReadOnlyList<string> FieldOrder { get; }

    public IReadOnlyDictionary<string, object?> Values { get; }

    public object? GetValue(string fieldName)
    {
        return Values.TryGetValue(fieldName, out var value)
            ? value
            : throw new InvalidOperationException($"Row does not contain field '{fieldName}'.");
    }
}

internal sealed record ConfigTable(
    SheetContract Contract,
    IReadOnlyList<string> FieldOrder,
    IReadOnlyList<ConfigRow> Rows);

internal sealed class ConfigDocument
{
    public ConfigDocument(
        long schemaVersion,
        string contentVersion,
        IReadOnlyList<ConfigTable> tables)
    {
        SchemaVersion = schemaVersion;
        ContentVersion = contentVersion;
        Tables = tables;
    }

    public long SchemaVersion { get; }

    public string ContentVersion { get; }

    public IReadOnlyList<ConfigTable> Tables { get; }

    public ConfigTable GetRequiredTable(string sheetName)
    {
        return Tables.FirstOrDefault(table => string.Equals(table.Contract.SheetName, sheetName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Document does not contain table '{sheetName}'.");
    }
}

public sealed record ExportResult(
    string? OutputPath,
    string ContentHash,
    long SchemaVersion,
    string ContentVersion,
    IReadOnlyDictionary<string, int> RecordCounts,
    int OutputBytes);

public sealed record GeneratedArtifactResult(
    string JsonPath,
    string HashPath,
    string ConfigIdsPath,
    string ContentHash,
    long SchemaVersion,
    string ContentVersion,
    IReadOnlyDictionary<string, int> RecordCounts,
    int JsonBytes,
    int HashBytes,
    int ConfigIdsBytes,
    int ConfigIdSetCount,
    int ConfigIdConstantCount);
