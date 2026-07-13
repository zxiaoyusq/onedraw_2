using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Excel;
using OneStrokeDemon.ConfigExporter.Processing;
using OneStrokeDemon.ConfigExporter.Validation;

namespace OneStrokeDemon.ConfigExporter.Tests;

public sealed class ConfigValidationTests
{
    public static IEnumerable<object[]> InvalidCaseIds()
    {
        return LoadFixture().Cases.Select(testCase => new object[] { testCase.Id });
    }

    [Fact]
    public void FrozenWorkbookPassesProductionValidation()
    {
        var repository = TestRepository.Find();
        var workbook = new OpenXmlWorkbookReader().Read(repository.WorkbookPath);
        var document = new ConfigDocumentBuilder(new SchemaContractValidator())
            .Build(workbook, repository.SchemaPath);

        new ConfigValidator().Validate(document, repository.SchemaPath);
    }

    [Theory]
    [MemberData(nameof(InvalidCaseIds))]
    public void InvalidWorkbookFailsWithStableCellDiagnostic(string caseId)
    {
        var repository = TestRepository.Find();
        var testCase = LoadFixture().Cases.Single(candidate => string.Equals(
            candidate.Id,
            caseId,
            StringComparison.Ordinal));
        var source = new OpenXmlWorkbookReader().Read(repository.WorkbookPath);
        var invalid = RawWorkbookMutator.Apply(source, testCase);

        var exception = Assert.Throws<ConfigExportException>(() =>
        {
            var document = new ConfigDocumentBuilder(new SchemaContractValidator())
                .Build(invalid, repository.SchemaPath);
            new ConfigValidator().Validate(document, repository.SchemaPath);
        });

        Assert.Equal(testCase.ExpectedCode, exception.Code);
        Assert.Equal(testCase.ExpectedSheet, exception.Sheet);
        Assert.Equal(testCase.ExpectedField, exception.Field);
        Assert.NotNull(exception.Row);
        Assert.True(exception.Row >= 5, $"Expected an Excel data row, actual {exception.Row}.");
        Assert.Contains($"sheet={testCase.ExpectedSheet}", exception.ToDiagnosticString(), StringComparison.Ordinal);
        Assert.Contains($"field={testCase.ExpectedField}", exception.ToDiagnosticString(), StringComparison.Ordinal);
    }

    private static InvalidConfigFixture LoadFixture()
    {
        return InvalidConfigFixture.Load(TestRepository.Find().InvalidConfigCasesPath);
    }
}
