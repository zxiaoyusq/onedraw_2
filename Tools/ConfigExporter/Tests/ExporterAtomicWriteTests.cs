using System.Text;
using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.IO;

namespace OneStrokeDemon.ConfigExporter.Tests;

public sealed class ExporterAtomicWriteTests
{
    [Fact]
    public void VerificationFailurePreservesExistingOutputAndRemovesTemporaryFile()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var outputPath = Path.Combine(temporaryDirectory.Path, "gameplay.json");
        File.WriteAllText(outputPath, "old-output", Encoding.UTF8);
        var writer = new AtomicFileWriter();

        var exception = Assert.Throws<ConfigExportException>(() => writer.Write(
            outputPath,
            Encoding.UTF8.GetBytes("new-output"),
            _ => throw new ConfigExportException("CFG012", "Injected self-check failure.")));

        Assert.Equal("CFG012", exception.Code);
        Assert.Equal("old-output", File.ReadAllText(outputPath, Encoding.UTF8));
        Assert.False(File.Exists($"{outputPath}.tmp"));
    }

    [Fact]
    public void SuccessfulVerificationReplacesExistingOutput()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var outputPath = Path.Combine(temporaryDirectory.Path, "gameplay.json");
        File.WriteAllText(outputPath, "old-output", Encoding.UTF8);
        var writer = new AtomicFileWriter();

        writer.Write(
            outputPath,
            Encoding.UTF8.GetBytes("new-output"),
            temporaryPath => Assert.Equal("new-output", File.ReadAllText(temporaryPath, Encoding.UTF8)));

        Assert.Equal("new-output", File.ReadAllText(outputPath, Encoding.UTF8));
        Assert.False(File.Exists($"{outputPath}.tmp"));
    }
}
