using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Services;

namespace OneStrokeDemon.ConfigExporter.Cli;

internal sealed class ConfigExporterApplication
{
    public const int SuccessExitCode = 0;
    public const int CommandLineErrorExitCode = 2;
    public const int ConfigErrorExitCode = 3;
    public const int UnexpectedErrorExitCode = 4;

    private readonly ConfigExporterService _service;

    public ConfigExporterApplication(ConfigExporterService service)
    {
        _service = service;
    }

    public int Run(IReadOnlyList<string> args, TextWriter standardOutput, TextWriter standardError)
    {
        if (args.Count == 1 && args[0] is "--help" or "-h")
        {
            standardOutput.WriteLine(Usage);
            return SuccessExitCode;
        }

        try
        {
            var options = CliOptions.Parse(args);
            var result = options.Command switch
            {
                ExporterCommand.Validate => _service.Validate(options.InputPath, options.SchemaPath),
                ExporterCommand.Export => _service.Export(
                    options.InputPath,
                    options.OutputPath!,
                    options.SchemaPath),
                _ => throw new InvalidOperationException($"Unsupported command {options.Command}."),
            };

            var recordCount = result.RecordCounts.Values.Sum();
            standardOutput.WriteLine(
                options.Command == ExporterCommand.Export
                    ? $"CONFIG_EXPORT_PASS output={result.OutputPath} bytes={result.OutputBytes}"
                    : "CONFIG_VALIDATION_PASS");
            standardOutput.WriteLine(
                $"schema={result.SchemaVersion} content={result.ContentVersion} hash={result.ContentHash} " +
                $"tables={result.RecordCounts.Count} records={recordCount} strict={options.Strict}");
            if (options.Command == ExporterCommand.Validate)
            {
                standardOutput.WriteLine("VALIDATION_SCOPE=T210_EXPORTABILITY_HEADER_TYPE_DETERMINISM_ONLY");
            }

            return SuccessExitCode;
        }
        catch (CliParseException exception)
        {
            standardError.WriteLine($"CLI001: {exception.Message}");
            standardError.WriteLine(Usage);
            return CommandLineErrorExitCode;
        }
        catch (ConfigExportException exception)
        {
            standardError.WriteLine(exception.ToDiagnosticString());
            return ConfigErrorExitCode;
        }
        catch (Exception exception)
        {
            standardError.WriteLine($"UNEXPECTED: {exception.Message}");
            return UnexpectedErrorExitCode;
        }
    }

    public const string Usage = """
        OneStrokeDemon ConfigExporter

        validate --input <GameConfig.xlsx> [--schema <gameplay.schema.json>] [--strict]
        export   --input <GameConfig.xlsx> --output <gameplay_config.json> [--schema <gameplay.schema.json>] [--strict]

        T210 validates exportability, fixed sheets/headers, value parsing, schema/header alignment,
        deterministic ordering/hash, and output self-check. T220 adds full content semantics.
        """;
}
