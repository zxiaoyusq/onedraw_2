using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Excel;
using OneStrokeDemon.ConfigExporter.Model;
using OneStrokeDemon.ConfigExporter.Processing;

namespace OneStrokeDemon.ConfigExporter.Tests;

public sealed class ExporterHeaderTests
{
    [Fact]
    public void HeaderDriftFailsWithPreciseDiagnostic()
    {
        var repository = TestRepository.Find();
        var workbook = new OpenXmlWorkbookReader().Read(repository.WorkbookPath);
        var mutatedSheets = workbook.Sheets.Select(sheet =>
        {
            if (!string.Equals(sheet.Name, "Enemies", StringComparison.Ordinal))
            {
                return sheet;
            }

            var headers = sheet.Headers.ToArray();
            headers[0] = "enemy_id";
            return new RawSheet(sheet.Name, headers, sheet.Rows);
        }).ToArray();
        var mutatedWorkbook = new RawWorkbook(mutatedSheets);
        var builder = new ConfigDocumentBuilder(new SchemaContractValidator());

        var exception = Assert.Throws<ConfigExportException>(
            () => builder.Build(mutatedWorkbook, repository.SchemaPath));

        Assert.Equal("CFG002", exception.Code);
        Assert.Equal("Enemies", exception.Sheet);
        Assert.Equal((uint)4, exception.Row);
        Assert.Equal("enemyId", exception.Field);
        Assert.Contains("Expected 'enemyId', actual 'enemy_id'", exception.Message, StringComparison.Ordinal);
    }
}
