namespace OneStrokeDemon.ConfigExporter.Cli;

internal enum ExporterCommand
{
    Validate,
    Export,
}

internal sealed record CliOptions(
    ExporterCommand Command,
    string InputPath,
    string? OutputPath,
    string? SchemaPath,
    bool Strict)
{
    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            throw new CliParseException("A command is required: validate or export.");
        }

        var command = args[0] switch
        {
            "validate" => ExporterCommand.Validate,
            "export" => ExporterCommand.Export,
            _ => throw new CliParseException($"Unknown command '{args[0]}'."),
        };
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var strict = false;

        for (var index = 1; index < args.Count; index += 1)
        {
            var argument = args[index];
            if (argument == "--strict")
            {
                strict = true;
                continue;
            }

            if (argument is not ("--input" or "--output" or "--schema"))
            {
                throw new CliParseException($"Unknown option '{argument}'.");
            }

            if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new CliParseException($"Option '{argument}' requires a value.");
            }

            if (!values.TryAdd(argument, args[index + 1]))
            {
                throw new CliParseException($"Option '{argument}' was specified more than once.");
            }

            index += 1;
        }

        if (!values.TryGetValue("--input", out var inputPath) || string.IsNullOrWhiteSpace(inputPath))
        {
            throw new CliParseException("--input is required.");
        }

        values.TryGetValue("--output", out var outputPath);
        values.TryGetValue("--schema", out var schemaPath);
        if (command == ExporterCommand.Export && string.IsNullOrWhiteSpace(outputPath))
        {
            throw new CliParseException("export requires --output.");
        }

        return new CliOptions(command, inputPath, outputPath, schemaPath, strict);
    }
}

internal sealed class CliParseException : Exception
{
    public CliParseException(string message)
        : base(message)
    {
    }
}
