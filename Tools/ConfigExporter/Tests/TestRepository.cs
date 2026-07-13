namespace OneStrokeDemon.ConfigExporter.Tests;

internal sealed class TestRepository
{
    private TestRepository(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }

    public string WorkbookPath => Path.Combine(RootPath, "Design", "Config", "GameConfig.xlsx");

    public string SchemaPath => Path.Combine(RootPath, "config", "schema", "gameplay.schema.json");

    public string SamplePath => Path.Combine(RootPath, "config", "examples", "gameplay_config.sample.json");

    public string RuntimeJsonPath => Path.Combine(
        RootPath,
        "Assets",
        "_Game",
        "Config",
        "Generated",
        "gameplay_config.json");

    public string RuntimeHashPath => Path.Combine(
        RootPath,
        "Assets",
        "_Game",
        "Config",
        "Generated",
        "gameplay_config.hash");

    public string ConfigIdsPath => Path.Combine(
        RootPath,
        "Assets",
        "_Game",
        "Scripts",
        "Config",
        "Generated",
        "ConfigIds.g.cs");

    public string InvalidConfigCasesPath => Path.Combine(
        RootPath,
        "Tools",
        "ConfigExporter",
        "Tests",
        "Fixtures",
        "invalid-config-cases.json");

    public static TestRepository Find()
    {
        foreach (var startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "project-index.yaml")))
                {
                    return new TestRepository(directory.FullName);
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing project-index.yaml.");
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "one-stroke-demon-config-exporter-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
