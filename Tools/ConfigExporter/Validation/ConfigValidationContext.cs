using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Validation;

internal sealed class ConfigValidationContext
{
    public ConfigValidationContext(ConfigDocument document)
    {
        Document = document;
        Fields = FieldConstraintCatalog.Build(document);
        EnumRows = document.GetRequiredTable("Enums").Rows
            .GroupBy(row => ConfigValues.String(row, "enumType"), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ConfigRow>)group.ToArray(),
                StringComparer.Ordinal);
        EnumValues = EnumRows.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlySet<string>)new HashSet<string>(
                pair.Value.Select(row => ConfigValues.String(row, "value")),
                StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    public ConfigDocument Document { get; }

    public IReadOnlyDictionary<(string Sheet, string Field), ConfigFieldConstraint> Fields { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<ConfigRow>> EnumRows { get; }

    public IReadOnlyDictionary<string, IReadOnlySet<string>> EnumValues { get; }

    public ConfigFieldConstraint GetField(string sheetName, string fieldName)
    {
        return Fields.TryGetValue((sheetName, fieldName), out var field)
            ? field
            : throw new InvalidOperationException($"No field contract for {sheetName}.{fieldName}.");
    }
}
