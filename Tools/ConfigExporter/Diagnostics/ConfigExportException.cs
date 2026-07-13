namespace OneStrokeDemon.ConfigExporter.Diagnostics;

public sealed class ConfigExportException : Exception
{
    public ConfigExportException(
        string code,
        string message,
        string? sheet = null,
        uint? row = null,
        string? field = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Sheet = sheet;
        Row = row;
        Field = field;
    }

    public string Code { get; }

    public string? Sheet { get; }

    public uint? Row { get; }

    public string? Field { get; }

    public string ToDiagnosticString()
    {
        var location = new List<string>();
        if (!string.IsNullOrEmpty(Sheet))
        {
            location.Add($"sheet={Sheet}");
        }

        if (Row.HasValue)
        {
            location.Add($"row={Row.Value}");
        }

        if (!string.IsNullOrEmpty(Field))
        {
            location.Add($"field={Field}");
        }

        var suffix = location.Count == 0 ? string.Empty : $" [{string.Join(", ", location)}]";
        return $"{Code}{suffix}: {Message}";
    }
}
