using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 把导出的标准 JSON 严格解析为原始令牌树和强类型配置文档。
    /// </summary>
    internal static class GameplayConfigParser
    {
        /// <summary>拒绝空输入、注释、重复字段、未知字段和根对象后的额外内容。</summary>
        public static ParsedGameplayConfig Parse(string json, string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("A non-empty configuration source is required.", nameof(source));
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new GameplayConfigException(
                    "CFGRT001",
                    "Configuration JSON is empty.",
                    source,
                    "root");
            }

            try
            {
                JObject root;
                // 第一遍保留 JObject 供规范化哈希计算，并在读取阶段拒绝重复属性。
                using (var stringReader = new StringReader(json))
                using (var jsonReader = new StrictJsonTextReader(stringReader))
                {
                    jsonReader.CloseInput = false;
                    jsonReader.Culture = CultureInfo.InvariantCulture;
                    jsonReader.DateParseHandling = DateParseHandling.None;
                    jsonReader.FloatParseHandling = FloatParseHandling.Decimal;
                    root = JObject.Load(
                        jsonReader,
                        new JsonLoadSettings
                        {
                            CommentHandling = CommentHandling.Ignore,
                            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                            LineInfoHandling = LineInfoHandling.Load,
                        });

                    // 根对象之后不能再出现第二个 JSON 值或其他有效令牌。
                    if (jsonReader.Read())
                    {
                        throw new JsonReaderException("Unexpected content follows the root object.");
                    }
                }

                // 第二遍按 OptIn DTO 严格反序列化，未知或缺失成员直接使整份配置失败。
                var serializer = JsonSerializer.Create(new JsonSerializerSettings
                {
                    CheckAdditionalContent = true,
                    Culture = CultureInfo.InvariantCulture,
                    DateParseHandling = DateParseHandling.None,
                    FloatParseHandling = FloatParseHandling.Decimal,
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Error,
                    NullValueHandling = NullValueHandling.Include,
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    TypeNameHandling = TypeNameHandling.None,
                });
                GameplayConfigDocument document = root.ToObject<GameplayConfigDocument>(serializer);
                if (document == null)
                {
                    throw new JsonSerializationException("Configuration root deserialized to null.");
                }

                return new ParsedGameplayConfig(root, document);
            }
            // 已带稳定错误码的配置异常保持原样，避免被笼统的 JSON 合同错误覆盖。
            catch (GameplayConfigException)
            {
                throw;
            }
            // 将 JSON.NET、格式和数值溢出错误统一包装成可定位的运行时合同错误。
            catch (Exception exception) when (
                exception is JsonException || exception is FormatException || exception is OverflowException)
            {
                throw new GameplayConfigException(
                    "CFGRT002",
                    $"JSON contract failed: {exception.Message}",
                    source,
                    "root",
                    exception);
            }
        }

        /// <summary>扩展 JSON.NET 读取器，在令牌层明确禁止注释。</summary>
        private sealed class StrictJsonTextReader : JsonTextReader
        {
            /// <summary>使用给定文本读取器创建严格 JSON 读取器。</summary>
            public StrictJsonTextReader(TextReader reader)
                : base(reader)
            {
            }

            /// <summary>读取下一个令牌，并在发现注释时立即失败。</summary>
            public override bool Read()
            {
                bool hasToken = base.Read();
                if (hasToken && TokenType == JsonToken.Comment)
                {
                    throw new JsonReaderException("JSON comments are not allowed in runtime configuration.");
                }

                return hasToken;
            }
        }
    }

    /// <summary>
    /// 同时保存原始 JSON 根令牌和强类型文档，供哈希验证与索引构建分别使用。
    /// </summary>
    internal sealed class ParsedGameplayConfig
    {
        /// <summary>创建一份已完成严格解析的配置结果。</summary>
        public ParsedGameplayConfig(JObject root, GameplayConfigDocument document)
        {
            Root = root;
            Document = document;
        }

        /// <summary>获取保留原始数值形态的 JSON 根令牌。</summary>
        public JObject Root { get; }

        /// <summary>获取强类型配置文档。</summary>
        public GameplayConfigDocument Document { get; }
    }
}
