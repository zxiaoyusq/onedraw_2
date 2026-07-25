using System.Text;
using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Services;

namespace OneStrokeDemon.ConfigExporter.Tests;

// 验证三份受管配置产物可确定性生成、严格比对且不会被验证流程隐式改写。
public sealed class ConfigPipelineE2ETests
{
    private const string ExpectedContentHash =
        "9cc48fcb5f3b45cff68dd0bfc09cf533d808b26cc956553bc5b060cfa5113abb";

    [Fact]
    // 验证两次导出字节一致，并与仓库受管产物（含中文注释）完全相同。
    public void GeneratedJsonHashAndIdsAreDeterministicAndMatchTrackedArtifacts()
    {
        var repository = TestRepository.Find();
        using var temporaryDirectory = new TemporaryDirectory();
        var first = Paths(temporaryDirectory.Path, "first");
        var second = Paths(temporaryDirectory.Path, "second");
        var service = new ConfigExporterService();

        var firstResult = service.Generate(
            repository.WorkbookPath,
            first.Json,
            first.Hash,
            first.Ids,
            repository.SchemaPath);
        var secondResult = service.Generate(
            repository.WorkbookPath,
            second.Json,
            second.Hash,
            second.Ids,
            repository.SchemaPath);
        var trackedResult = service.VerifyGenerated(
            repository.WorkbookPath,
            repository.RuntimeJsonPath,
            repository.RuntimeHashPath,
            repository.ConfigIdsPath,
            repository.SchemaPath);

        Assert.Equal(ExpectedContentHash, firstResult.ContentHash);
        Assert.Equal(firstResult.ContentHash, secondResult.ContentHash);
        Assert.Equal(firstResult.ContentHash, trackedResult.ContentHash);
        Assert.Equal(28, firstResult.ConfigIdSetCount);
        Assert.Equal(380, firstResult.ConfigIdConstantCount);
        Assert.Equal(File.ReadAllBytes(first.Json), File.ReadAllBytes(second.Json));
        Assert.Equal(File.ReadAllBytes(first.Hash), File.ReadAllBytes(second.Hash));
        Assert.Equal(File.ReadAllBytes(first.Ids), File.ReadAllBytes(second.Ids));
        Assert.Equal(File.ReadAllBytes(first.Json), File.ReadAllBytes(repository.RuntimeJsonPath));
        Assert.Equal(File.ReadAllBytes(first.Hash), File.ReadAllBytes(repository.RuntimeHashPath));
        Assert.Equal(File.ReadAllBytes(first.Ids), File.ReadAllBytes(repository.ConfigIdsPath));
        Assert.Equal($"{ExpectedContentHash}\n", File.ReadAllText(first.Hash, Encoding.UTF8));

        var idsBytes = File.ReadAllBytes(first.Ids);
        Assert.False(idsBytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
        var idsText = Encoding.UTF8.GetString(idsBytes);
        Assert.DoesNotContain("\r", idsText, StringComparison.Ordinal);
        Assert.Contains("此文件由配置导出器自动生成，请勿手工修改", idsText, StringComparison.Ordinal);
        Assert.Contains("汇总来自Players.playerId的稳定配置ID", idsText, StringComparison.Ordinal);
        Assert.Contains("配置ID：player_moyan。", idsText, StringComparison.Ordinal);
        Assert.Contains("public const string PlayerMoyan = \"player_moyan\";", idsText, StringComparison.Ordinal);
        Assert.Contains("public const string SceneBattle = \"scene_battle\";", idsText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("gameplay-json")]
    [InlineData("content-hash")]
    [InlineData("config-ids")]
    // 验证任一受管产物漂移都会失败，且验证命令不会偷偷覆盖现场文件。
    public void VerifyGeneratedRejectsEveryArtifactDriftWithoutRewriting(string artifactName)
    {
        var repository = TestRepository.Find();
        using var temporaryDirectory = new TemporaryDirectory();
        var paths = Paths(temporaryDirectory.Path, "drift");
        var service = new ConfigExporterService();
        service.Generate(
            repository.WorkbookPath,
            paths.Json,
            paths.Hash,
            paths.Ids,
            repository.SchemaPath);
        var path = artifactName switch
        {
            "gameplay-json" => paths.Json,
            "content-hash" => paths.Hash,
            "config-ids" => paths.Ids,
            _ => throw new ArgumentOutOfRangeException(nameof(artifactName)),
        };
        File.AppendAllText(path, "drift", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var bytesBeforeVerify = File.ReadAllBytes(path);

        var exception = Assert.Throws<ConfigExportException>(() => service.VerifyGenerated(
            repository.WorkbookPath,
            paths.Json,
            paths.Hash,
            paths.Ids,
            repository.SchemaPath));

        Assert.Equal("CFG013", exception.Code);
        Assert.Contains(artifactName, exception.Message, StringComparison.Ordinal);
        Assert.Contains("drift detected", exception.Message, StringComparison.Ordinal);
        Assert.Equal(bytesBeforeVerify, File.ReadAllBytes(path));
    }

    [Fact]
    // 验证三个输出路径不能互相别名，避免一个产物覆盖另一个产物。
    public void GenerateRejectsAliasedOutputPathsBeforeWriting()
    {
        var repository = TestRepository.Find();
        using var temporaryDirectory = new TemporaryDirectory();
        var sharedPath = Path.Combine(temporaryDirectory.Path, "shared-output");
        var service = new ConfigExporterService();

        var exception = Assert.Throws<ConfigExportException>(() => service.Generate(
            repository.WorkbookPath,
            sharedPath,
            sharedPath,
            Path.Combine(temporaryDirectory.Path, "ConfigIds.g.cs"),
            repository.SchemaPath));

        Assert.Equal("CFG013", exception.Code);
        Assert.Contains("must be distinct", exception.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(sharedPath));
    }

    // 为一次隔离导出构造互不冲突的三份临时产物路径。
    private static ArtifactPaths Paths(string root, string prefix)
    {
        return new ArtifactPaths(
            Path.Combine(root, $"{prefix}-gameplay_config.json"),
            Path.Combine(root, $"{prefix}-gameplay_config.hash"),
            Path.Combine(root, $"{prefix}-ConfigIds.g.cs"));
    }

    private sealed record ArtifactPaths(string Json, string Hash, string Ids);
}
