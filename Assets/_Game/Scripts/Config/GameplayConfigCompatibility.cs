using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OneStrokeDemon.Config
{
    public static class GameplayConfigCompatibility
    {
        public const long SupportedSchemaVersion = 1;
        public const int SupportedContentMajor = 0;
        public const int SupportedContentMinor = 1;

        private static readonly Regex ContentVersionPattern = new Regex(
            "^(?<major>[0-9]+)\\.(?<minor>[0-9]+)\\.(?<patch>[0-9]+)(?:-[a-z0-9][a-z0-9.-]*)?$",
            RegexOptions.CultureInvariant);

        internal static void Validate(GameplayConfigDocument document, string source)
        {
            if (document.SchemaVersion != SupportedSchemaVersion)
            {
                throw new GameplayConfigException(
                    "CFGRT003",
                    $"Unsupported schema version {document.SchemaVersion}; expected {SupportedSchemaVersion}.",
                    source,
                    "schemaVersion");
            }

            Match match = ContentVersionPattern.Match(document.ContentVersion ?? string.Empty);
            if (!match.Success ||
                !int.TryParse(match.Groups["major"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int major) ||
                !int.TryParse(match.Groups["minor"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int minor))
            {
                throw new GameplayConfigException(
                    "CFGRT004",
                    $"Malformed content version '{document.ContentVersion}'.",
                    source,
                    "contentVersion");
            }

            if (major != SupportedContentMajor || minor != SupportedContentMinor)
            {
                throw new GameplayConfigException(
                    "CFGRT004",
                    $"Incompatible content version '{document.ContentVersion}'; supported line is " +
                    $"{SupportedContentMajor}.{SupportedContentMinor}.x.",
                    source,
                    "contentVersion");
            }
        }
    }
}
