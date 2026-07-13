using OneStrokeDemon.ConfigExporter.Diagnostics;

namespace OneStrokeDemon.ConfigExporter.IO;

internal sealed class AtomicFileWriter
{
    public void Write(string outputPath, ReadOnlySpan<byte> content, Action<string> verifyTemporaryFile)
    {
        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath)
            ?? throw new ConfigExportException("CFG000", $"Output path has no directory: {fullOutputPath}");
        Directory.CreateDirectory(outputDirectory);
        var temporaryPath = $"{fullOutputPath}.tmp";

        try
        {
            File.Delete(temporaryPath);
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 64 * 1024,
                       FileOptions.SequentialScan))
            {
                stream.Write(content);
                stream.Flush(flushToDisk: true);
            }

            verifyTemporaryFile(temporaryPath);
            File.Move(temporaryPath, fullOutputPath, overwrite: true);
        }
        catch (ConfigExportException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ConfigExportException(
                "CFG000",
                $"Unable to atomically write output '{fullOutputPath}': {exception.Message}",
                innerException: exception);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
