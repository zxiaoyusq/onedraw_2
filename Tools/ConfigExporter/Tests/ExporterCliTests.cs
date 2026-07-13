using OneStrokeDemon.ConfigExporter.Cli;
using OneStrokeDemon.ConfigExporter.Services;

namespace OneStrokeDemon.ConfigExporter.Tests;

public sealed class ExporterCliTests
{
    [Fact]
    public void MissingArgumentsReturnCommandLineFailure()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var application = new ConfigExporterApplication(new ConfigExporterService());

        var exitCode = application.Run(Array.Empty<string>(), output, error);

        Assert.Equal(ConfigExporterApplication.CommandLineErrorExitCode, exitCode);
        Assert.Contains("CLI001", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MissingWorkbookReturnsConfigFailure()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var application = new ConfigExporterApplication(new ConfigExporterService());

        var exitCode = application.Run(
            new[] { "validate", "--input", $"missing-{Guid.NewGuid():N}.xlsx", "--strict" },
            output,
            error);

        Assert.Equal(ConfigExporterApplication.ConfigErrorExitCode, exitCode);
        Assert.Contains("CFG000", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyGeneratedReportsPassForTrackedArtifacts()
    {
        var repository = TestRepository.Find();
        var output = new StringWriter();
        var error = new StringWriter();
        var application = new ConfigExporterApplication(new ConfigExporterService());

        var exitCode = application.Run(
            new[]
            {
                "verify",
                "--input", repository.WorkbookPath,
                "--output", repository.RuntimeJsonPath,
                "--hash-output", repository.RuntimeHashPath,
                "--ids-output", repository.ConfigIdsPath,
                "--schema", repository.SchemaPath,
                "--strict",
            },
            output,
            error);

        Assert.Equal(ConfigExporterApplication.SuccessExitCode, exitCode);
        Assert.Contains("CONFIG_GENERATED_VERIFY_PASS", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("idSets=27 idConstants=315", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public void VerifyGeneratedDriftReturnsConfigFailure()
    {
        var repository = TestRepository.Find();
        using var temporaryDirectory = new TemporaryDirectory();
        var jsonPath = Path.Combine(temporaryDirectory.Path, "gameplay_config.json");
        var hashPath = Path.Combine(temporaryDirectory.Path, "gameplay_config.hash");
        var idsPath = Path.Combine(temporaryDirectory.Path, "ConfigIds.g.cs");
        var service = new ConfigExporterService();
        service.Generate(
            repository.WorkbookPath,
            jsonPath,
            hashPath,
            idsPath,
            repository.SchemaPath);
        File.AppendAllText(hashPath, "drift");
        var output = new StringWriter();
        var error = new StringWriter();
        var application = new ConfigExporterApplication(service);

        var exitCode = application.Run(
            new[]
            {
                "verify",
                "--input", repository.WorkbookPath,
                "--output", jsonPath,
                "--hash-output", hashPath,
                "--ids-output", idsPath,
                "--schema", repository.SchemaPath,
            },
            output,
            error);

        Assert.Equal(ConfigExporterApplication.ConfigErrorExitCode, exitCode);
        Assert.Contains("CFG013", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("content-hash", error.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("PASS", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateRequiresAllManagedOutputPaths()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var application = new ConfigExporterApplication(new ConfigExporterService());

        var exitCode = application.Run(
            new[] { "generate", "--input", "GameConfig.xlsx", "--output", "gameplay.json" },
            output,
            error);

        Assert.Equal(ConfigExporterApplication.CommandLineErrorExitCode, exitCode);
        Assert.Contains("requires --output, --hash-output, and --ids-output", error.ToString(), StringComparison.Ordinal);
    }
}
