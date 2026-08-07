using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 定义当前客户端接受的配置结构版本和内容版本线。
    /// </summary>
    public static class GameplayConfigCompatibility
    {
        /// <summary>客户端支持的唯一结构版本。</summary>
        public const long SupportedSchemaVersion = 7;

        /// <summary>客户端支持的内容主版本。</summary>
        public const int SupportedContentMajor = 0;

        /// <summary>客户端支持的内容次版本。</summary>
        public const int SupportedContentMinor = 7;

        private static readonly Regex ContentVersionPattern = new Regex(
            "^(?<major>[0-9]+)\\.(?<minor>[0-9]+)\\.(?<patch>[0-9]+)(?:-[a-z0-9][a-z0-9.-]*)?$",
            RegexOptions.CultureInvariant);

        /// <summary>验证配置文档是否属于当前客户端支持的结构与内容版本线。</summary>
        internal static void Validate(GameplayConfigDocument document, string source)
        {
            // 结构版本必须精确一致，否则 DTO 字段含义可能已经发生变化。
            if (document.SchemaVersion != SupportedSchemaVersion)
            {
                throw new GameplayConfigException(
                    "CFGRT003",
                    $"Unsupported schema version {document.SchemaVersion}; expected {SupportedSchemaVersion}.",
                    source,
                    "schemaVersion");
            }

            // 内容版本允许补丁号及预发布后缀变化，但主次版本必须可解析。
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

            // 同一 0.7.x 内容线可在不升级客户端代码的前提下加载。
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
