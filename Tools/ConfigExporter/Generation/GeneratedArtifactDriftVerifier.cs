using System.Security.Cryptography;
using OneStrokeDemon.ConfigExporter.Diagnostics;

namespace OneStrokeDemon.ConfigExporter.Generation;

internal sealed class GeneratedArtifactDriftVerifier
{
    public void Verify(string path, ReadOnlySpan<byte> expectedBytes, string artifactName)
    {
        var fullPath = Path.GetFullPath(path);
        try
        {
            if (!File.Exists(fullPath))
            {
                throw Failure($"Generated artifact '{artifactName}' is missing: {fullPath}");
            }

            var actualBytes = File.ReadAllBytes(fullPath);
            if (actualBytes.AsSpan().SequenceEqual(expectedBytes))
            {
                return;
            }

            var firstDifference = FirstDifference(actualBytes, expectedBytes);
            throw Failure(
                $"Generated artifact drift detected for '{artifactName}' at byte {firstDifference}: {fullPath}. " +
                $"actualBytes={actualBytes.Length} expectedBytes={expectedBytes.Length} " +
                $"actualSha256={Hash(actualBytes)} expectedSha256={Hash(expectedBytes)}");
        }
        catch (ConfigExportException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw Failure(
                $"Unable to read generated artifact '{artifactName}' at '{fullPath}': {exception.Message}",
                exception);
        }
    }

    private static int FirstDifference(ReadOnlySpan<byte> actual, ReadOnlySpan<byte> expected)
    {
        var sharedLength = Math.Min(actual.Length, expected.Length);
        for (var index = 0; index < sharedLength; index += 1)
        {
            if (actual[index] != expected[index])
            {
                return index;
            }
        }

        return sharedLength;
    }

    private static string Hash(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static ConfigExportException Failure(string message, Exception? innerException = null)
    {
        return new ConfigExportException("CFG013", message, innerException: innerException);
    }
}
