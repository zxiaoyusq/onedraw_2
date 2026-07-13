using OneStrokeDemon.ConfigExporter.Cli;
using OneStrokeDemon.ConfigExporter.Services;

namespace OneStrokeDemon.ConfigExporter;

public static class Program
{
    public static int Main(string[] args)
    {
        return new ConfigExporterApplication(new ConfigExporterService())
            .Run(args, Console.Out, Console.Error);
    }
}
