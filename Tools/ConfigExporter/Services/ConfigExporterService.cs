using OneStrokeDemon.ConfigExporter.Excel;
using OneStrokeDemon.ConfigExporter.IO;
using OneStrokeDemon.ConfigExporter.Model;
using OneStrokeDemon.ConfigExporter.Processing;
using OneStrokeDemon.ConfigExporter.Serialization;

namespace OneStrokeDemon.ConfigExporter.Services;

public sealed class ConfigExporterService
{
    private readonly OpenXmlWorkbookReader _workbookReader;
    private readonly ConfigDocumentBuilder _documentBuilder;
    private readonly StableJsonSerializer _serializer;
    private readonly OutputVerifier _outputVerifier;
    private readonly AtomicFileWriter _atomicFileWriter;

    public ConfigExporterService()
        : this(
            new OpenXmlWorkbookReader(),
            new ConfigDocumentBuilder(new SchemaContractValidator()),
            new StableJsonSerializer(),
            new OutputVerifier(),
            new AtomicFileWriter())
    {
    }

    internal ConfigExporterService(
        OpenXmlWorkbookReader workbookReader,
        ConfigDocumentBuilder documentBuilder,
        StableJsonSerializer serializer,
        OutputVerifier outputVerifier,
        AtomicFileWriter atomicFileWriter)
    {
        _workbookReader = workbookReader;
        _documentBuilder = documentBuilder;
        _serializer = serializer;
        _outputVerifier = outputVerifier;
        _atomicFileWriter = atomicFileWriter;
    }

    public ExportResult Validate(string inputPath, string? schemaPath = null)
    {
        var prepared = Prepare(inputPath, schemaPath);
        _outputVerifier.VerifyBytes(prepared.Serialized.Bytes, prepared.Document, prepared.Serialized.ContentHash);
        return CreateResult(outputPath: null, prepared);
    }

    public ExportResult Export(string inputPath, string outputPath, string? schemaPath = null)
    {
        var prepared = Prepare(inputPath, schemaPath);
        var fullOutputPath = Path.GetFullPath(outputPath);
        _atomicFileWriter.Write(
            fullOutputPath,
            prepared.Serialized.Bytes,
            temporaryPath => _outputVerifier.VerifyFile(
                temporaryPath,
                prepared.Document,
                prepared.Serialized.ContentHash));
        return CreateResult(fullOutputPath, prepared);
    }

    internal PreparedExport Prepare(string inputPath, string? schemaPath)
    {
        var workbook = _workbookReader.Read(inputPath);
        var document = _documentBuilder.Build(workbook, schemaPath);
        var serialized = _serializer.Serialize(document);
        return new PreparedExport(document, serialized);
    }

    private static ExportResult CreateResult(string? outputPath, PreparedExport prepared)
    {
        var counts = prepared.Document.Tables.ToDictionary(
            table => table.Contract.SheetName,
            table => table.Rows.Count,
            StringComparer.Ordinal);
        return new ExportResult(
            outputPath,
            prepared.Serialized.ContentHash,
            prepared.Document.SchemaVersion,
            prepared.Document.ContentVersion,
            counts,
            prepared.Serialized.Bytes.Length);
    }
}

internal sealed record PreparedExport(ConfigDocument Document, SerializedConfig Serialized);
