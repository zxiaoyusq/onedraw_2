using System.Text;
using OneStrokeDemon.ConfigExporter.Excel;
using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Generation;
using OneStrokeDemon.ConfigExporter.IO;
using OneStrokeDemon.ConfigExporter.Model;
using OneStrokeDemon.ConfigExporter.Processing;
using OneStrokeDemon.ConfigExporter.Serialization;
using OneStrokeDemon.ConfigExporter.Validation;

namespace OneStrokeDemon.ConfigExporter.Services;

public sealed class ConfigExporterService
{
    private readonly OpenXmlWorkbookReader _workbookReader;
    private readonly ConfigDocumentBuilder _documentBuilder;
    private readonly ConfigValidator _validator;
    private readonly StableJsonSerializer _serializer;
    private readonly OutputVerifier _outputVerifier;
    private readonly AtomicFileWriter _atomicFileWriter;
    private readonly ConfigIdsGenerator _configIdsGenerator;
    private readonly GeneratedArtifactDriftVerifier _artifactDriftVerifier;

    public ConfigExporterService()
        : this(
            new OpenXmlWorkbookReader(),
            new ConfigDocumentBuilder(new SchemaContractValidator()),
            new ConfigValidator(),
            new StableJsonSerializer(),
            new OutputVerifier(),
            new AtomicFileWriter(),
            new ConfigIdsGenerator(),
            new GeneratedArtifactDriftVerifier())
    {
    }

    internal ConfigExporterService(
        OpenXmlWorkbookReader workbookReader,
        ConfigDocumentBuilder documentBuilder,
        ConfigValidator validator,
        StableJsonSerializer serializer,
        OutputVerifier outputVerifier,
        AtomicFileWriter atomicFileWriter,
        ConfigIdsGenerator configIdsGenerator,
        GeneratedArtifactDriftVerifier artifactDriftVerifier)
    {
        _workbookReader = workbookReader;
        _documentBuilder = documentBuilder;
        _validator = validator;
        _serializer = serializer;
        _outputVerifier = outputVerifier;
        _atomicFileWriter = atomicFileWriter;
        _configIdsGenerator = configIdsGenerator;
        _artifactDriftVerifier = artifactDriftVerifier;
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

    public GeneratedArtifactResult Generate(
        string inputPath,
        string jsonPath,
        string hashPath,
        string configIdsPath,
        string? schemaPath = null)
    {
        var paths = ResolveDistinctArtifactPaths(jsonPath, hashPath, configIdsPath);
        var prepared = Prepare(inputPath, schemaPath);
        var artifacts = CreateGeneratedArtifacts(prepared);
        _atomicFileWriter.Write(
            paths.JsonPath,
            artifacts.JsonBytes,
            temporaryPath => _outputVerifier.VerifyFile(
                temporaryPath,
                prepared.Document,
                prepared.Serialized.ContentHash));
        _atomicFileWriter.Write(
            paths.HashPath,
            artifacts.HashBytes,
            temporaryPath => _artifactDriftVerifier.Verify(
                temporaryPath,
                artifacts.HashBytes,
                "content-hash"));
        _atomicFileWriter.Write(
            paths.ConfigIdsPath,
            artifacts.ConfigIds.Bytes,
            temporaryPath => _artifactDriftVerifier.Verify(
                temporaryPath,
                artifacts.ConfigIds.Bytes,
                "config-ids"));
        return CreateGeneratedResult(paths, prepared, artifacts);
    }

    public GeneratedArtifactResult VerifyGenerated(
        string inputPath,
        string jsonPath,
        string hashPath,
        string configIdsPath,
        string? schemaPath = null)
    {
        var paths = ResolveDistinctArtifactPaths(jsonPath, hashPath, configIdsPath);
        var prepared = Prepare(inputPath, schemaPath);
        var artifacts = CreateGeneratedArtifacts(prepared);
        _artifactDriftVerifier.Verify(paths.JsonPath, artifacts.JsonBytes, "gameplay-json");
        _artifactDriftVerifier.Verify(paths.HashPath, artifacts.HashBytes, "content-hash");
        _artifactDriftVerifier.Verify(paths.ConfigIdsPath, artifacts.ConfigIds.Bytes, "config-ids");
        return CreateGeneratedResult(paths, prepared, artifacts);
    }

    internal PreparedExport Prepare(string inputPath, string? schemaPath)
    {
        var workbook = _workbookReader.Read(inputPath);
        var document = _documentBuilder.Build(workbook, schemaPath);
        _validator.Validate(document, schemaPath);
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

    private GeneratedArtifactSet CreateGeneratedArtifacts(PreparedExport prepared)
    {
        _outputVerifier.VerifyBytes(
            prepared.Serialized.Bytes,
            prepared.Document,
            prepared.Serialized.ContentHash);
        var hashBytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            .GetBytes($"{prepared.Serialized.ContentHash}\n");
        var configIds = _configIdsGenerator.Generate(
            prepared.Document,
            prepared.Serialized.ContentHash);
        return new GeneratedArtifactSet(prepared.Serialized.Bytes, hashBytes, configIds);
    }

    private static GeneratedArtifactResult CreateGeneratedResult(
        GeneratedArtifactPaths paths,
        PreparedExport prepared,
        GeneratedArtifactSet artifacts)
    {
        var counts = prepared.Document.Tables.ToDictionary(
            table => table.Contract.SheetName,
            table => table.Rows.Count,
            StringComparer.Ordinal);
        return new GeneratedArtifactResult(
            paths.JsonPath,
            paths.HashPath,
            paths.ConfigIdsPath,
            prepared.Serialized.ContentHash,
            prepared.Document.SchemaVersion,
            prepared.Document.ContentVersion,
            counts,
            artifacts.JsonBytes.Length,
            artifacts.HashBytes.Length,
            artifacts.ConfigIds.Bytes.Length,
            artifacts.ConfigIds.SetCount,
            artifacts.ConfigIds.ConstantCount);
    }

    private static GeneratedArtifactPaths ResolveDistinctArtifactPaths(
        string jsonPath,
        string hashPath,
        string configIdsPath)
    {
        var paths = new GeneratedArtifactPaths(
            Path.GetFullPath(jsonPath),
            Path.GetFullPath(hashPath),
            Path.GetFullPath(configIdsPath));
        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        if (new[] { paths.JsonPath, paths.HashPath, paths.ConfigIdsPath }
            .Distinct(pathComparer).Count() != 3)
        {
            throw new ConfigExportException(
                "CFG013",
                "Generated JSON, hash, and ConfigIds output paths must be distinct.");
        }

        return paths;
    }
}

internal sealed record PreparedExport(ConfigDocument Document, SerializedConfig Serialized);

internal sealed record GeneratedArtifactPaths(string JsonPath, string HashPath, string ConfigIdsPath);

internal sealed record GeneratedArtifactSet(
    byte[] JsonBytes,
    byte[] HashBytes,
    GeneratedConfigIds ConfigIds);
