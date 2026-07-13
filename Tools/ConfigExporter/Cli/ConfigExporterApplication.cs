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
            if (options.Command is ExporterCommand.Generate or ExporterCommand.Verify)
            {
                return RunGenerated(options, standardOutput);
            }

            var result = options.Command == ExporterCommand.Validate
                ? _service.Validate(options.InputPath, options.SchemaPath)
                : _service.Export(options.InputPath, options.OutputPath!, options.SchemaPath);

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
                standardOutput.WriteLine("VALIDATION_SCOPE=T220_PRODUCTION_CONFIG_CONTRACT");
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

    private int RunGenerated(CliOptions options, TextWriter standardOutput)
    {
        var result = options.Command == ExporterCommand.Generate
            ? _service.Generate(
                options.InputPath,
                options.OutputPath!,
                options.HashOutputPath!,
                options.ConfigIdsOutputPath!,
                options.SchemaPath)
            : _service.VerifyGenerated(
                options.InputPath,
                options.OutputPath!,
                options.HashOutputPath!,
                options.ConfigIdsOutputPath!,
                options.SchemaPath);
        var recordCount = result.RecordCounts.Values.Sum();
        standardOutput.WriteLine(
            options.Command == ExporterCommand.Generate
                ? $"CONFIG_GENERATION_PASS json={result.JsonPath} hash={result.HashPath} ids={result.ConfigIdsPath}"
                : $"CONFIG_GENERATED_VERIFY_PASS json={result.JsonPath} hash={result.HashPath} ids={result.ConfigIdsPath}");
        standardOutput.WriteLine(
            $"schema={result.SchemaVersion} content={result.ContentVersion} hash={result.ContentHash} " +
            $"tables={result.RecordCounts.Count} records={recordCount} strict={options.Strict}");
        standardOutput.WriteLine(
            $"jsonBytes={result.JsonBytes} hashBytes={result.HashBytes} idsBytes={result.ConfigIdsBytes} " +
            $"idSets={result.ConfigIdSetCount} idConstants={result.ConfigIdConstantCount}");
        standardOutput.WriteLine("GENERATED_SCOPE=T250_JSON_HASH_CONFIG_IDS");
        return SuccessExitCode;
    }

    public const string Usage = """
        OneStrokeDemon ConfigExporter

        validate --input <GameConfig.xlsx> [--schema <gameplay.schema.json>] [--strict]
        export   --input <GameConfig.xlsx> --output <gameplay_config.json> [--schema <gameplay.schema.json>] [--strict]
        generate --input <GameConfig.xlsx> --output <gameplay_config.json> --hash-output <gameplay_config.hash>
                 --ids-output <ConfigIds.g.cs> [--schema <gameplay.schema.json>] [--strict]
        verify   --input <GameConfig.xlsx> --output <gameplay_config.json> --hash-output <gameplay_config.hash>
                 --ids-output <ConfigIds.g.cs> [--schema <gameplay.schema.json>] [--strict]

        Validation covers fixed structure, required values, types, ranges, enums, IDs, uniqueness,
        foreign keys, group order, level/wave/spawn completeness, boss coverage, and output determinism.
        Generate writes all managed artifacts from one validated model; verify is read-only and rejects byte drift.
        """;
}
