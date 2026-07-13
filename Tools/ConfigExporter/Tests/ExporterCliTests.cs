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
}
