using System.Globalization;
using OneStrokeDemon.ConfigExporter.Excel;
using OneStrokeDemon.ConfigExporter.Model;
using OneStrokeDemon.ConfigExporter.Processing;
using OneStrokeDemon.ConfigExporter.Serialization;
using OneStrokeDemon.ConfigExporter.Services;

namespace OneStrokeDemon.ConfigExporter.Tests;

public sealed class ExporterDeterminismTests
{
    private const string ExpectedContentHash =
        "19dc788f890f995adb94458f74894b89514f85f3bfc9429659ddd2421a72f733";

    [Fact]
    public void SameInputExportsByteIdenticalJsonWithFrozenHash()
    {
        var repository = TestRepository.Find();
        using var temporaryDirectory = new TemporaryDirectory();
        var firstPath = Path.Combine(temporaryDirectory.Path, "first.json");
        var secondPath = Path.Combine(temporaryDirectory.Path, "second.json");
        var service = new ConfigExporterService();

        var first = service.Export(repository.WorkbookPath, firstPath, repository.SchemaPath);
        var second = service.Export(repository.WorkbookPath, secondPath, repository.SchemaPath);

        Assert.Equal(ExpectedContentHash, first.ContentHash);
        Assert.Equal(first.ContentHash, second.ContentHash);
        Assert.Equal(File.ReadAllBytes(firstPath), File.ReadAllBytes(secondPath));

        var exportedCanonical = StableJsonSerializer.CanonicalizeJson(
            File.ReadAllBytes(firstPath),
            excludeRootContentHash: false);
        var sampleCanonical = StableJsonSerializer.CanonicalizeJson(
            File.ReadAllBytes(repository.SamplePath),
            excludeRootContentHash: false);
        Assert.Equal(sampleCanonical, exportedCanonical);
    }

    [Fact]
    public void ExportDoesNotDependOnCurrentCulture()
    {
        var repository = TestRepository.Find();
        using var temporaryDirectory = new TemporaryDirectory();
        var invariantPath = Path.Combine(temporaryDirectory.Path, "invariant.json");
        var frenchPath = Path.Combine(temporaryDirectory.Path, "fr-FR.json");
        var service = new ConfigExporterService();
        service.Export(repository.WorkbookPath, invariantPath, repository.SchemaPath);

        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            service.Export(repository.WorkbookPath, frenchPath, repository.SchemaPath);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        Assert.Equal(File.ReadAllBytes(invariantPath), File.ReadAllBytes(frenchPath));
    }

    [Fact]
    public void SourceRowOrderDoesNotAffectStableOutput()
    {
        var repository = TestRepository.Find();
        var reader = new OpenXmlWorkbookReader();
        var source = reader.Read(repository.WorkbookPath);
        var reversed = new RawWorkbook(source.Sheets.Select(sheet => new RawSheet(
            sheet.Name,
            sheet.Headers,
            sheet.Rows.Reverse().ToArray())).ToArray());
        var builder = new ConfigDocumentBuilder(new SchemaContractValidator());
        var serializer = new StableJsonSerializer();

        var sourceOutput = serializer.Serialize(builder.Build(source, repository.SchemaPath));
        var reversedOutput = serializer.Serialize(builder.Build(reversed, repository.SchemaPath));

        Assert.Equal(ExpectedContentHash, reversedOutput.ContentHash);
        Assert.Equal(sourceOutput.Bytes, reversedOutput.Bytes);
    }
}
