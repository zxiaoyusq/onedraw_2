using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 按导出合同生成确定性的规范 JSON，并校验配置声明的 SHA-256。
    /// </summary>
    internal static class GameplayConfigHash
    {
        /// <summary>验证声明哈希格式，并与当前根对象的规范化哈希比较。</summary>
        public static void Verify(JObject root, string declaredHash, string source)
        {
            if (!IsLowerHexSha256(declaredHash))
            {
                throw new GameplayConfigException(
                    "CFGRT005",
                    $"contentHash '{declaredHash}' is not a lowercase SHA-256 value.",
                    source,
                    "contentHash");
            }

            string calculatedHash = Calculate(root);
            if (!string.Equals(declaredHash, calculatedHash, StringComparison.Ordinal))
            {
                throw new GameplayConfigException(
                    "CFGRT005",
                    $"contentHash mismatch; declared {declaredHash}, calculated {calculatedHash}.",
                    source,
                    "contentHash");
            }
        }

        /// <summary>计算忽略根级 contentHash 字段后的规范化小写 SHA-256。</summary>
        internal static string Calculate(JObject root)
        {
            // 先按固定文化和无空白格式写出规范 JSON，确保平台间结果一致。
            var text = new StringBuilder(256 * 1024);
            using (var stringWriter = new StringWriter(text, CultureInfo.InvariantCulture))
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                jsonWriter.CloseOutput = false;
                jsonWriter.Culture = CultureInfo.InvariantCulture;
                jsonWriter.Formatting = Formatting.None;
                jsonWriter.StringEscapeHandling = StringEscapeHandling.Default;
                WriteCanonicalToken(jsonWriter, root, isRoot: true);
                jsonWriter.Flush();
            }

            // 对规范 JSON 的 UTF-8 字节计算 SHA-256，并输出固定两位小写十六进制。
            byte[] bytes = Encoding.UTF8.GetBytes(text.ToString());
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(bytes);
                var result = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                {
                    result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }

                return result.ToString();
            }
        }

        /// <summary>递归写出属性排序、数组保序且数值格式固定的规范 JSON 令牌。</summary>
        private static void WriteCanonicalToken(JsonWriter writer, JToken token, bool isRoot)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    writer.WriteStartObject();
                    // 根级 contentHash 不参与自身哈希；对象属性按序数规则排序。
                    foreach (JProperty property in ((JObject)token).Properties()
                                 .Where(property => !(isRoot && property.Name == "contentHash"))
                                 .OrderBy(property => property.Name, StringComparer.Ordinal))
                    {
                        writer.WritePropertyName(property.Name);
                        WriteCanonicalToken(writer, property.Value, isRoot: false);
                    }

                    writer.WriteEndObject();
                    return;
                case JTokenType.Array:
                    writer.WriteStartArray();
                    // 数组顺序具有配置语义，规范化时必须保持原有顺序。
                    foreach (JToken item in (JArray)token)
                    {
                        WriteCanonicalToken(writer, item, isRoot: false);
                    }

                    writer.WriteEndArray();
                    return;
                case JTokenType.Integer:
                    writer.WriteRawValue(Convert.ToInt64(((JValue)token).Value, CultureInfo.InvariantCulture)
                        .ToString(CultureInfo.InvariantCulture));
                    return;
                case JTokenType.Float:
                    decimal number = Convert.ToDecimal(((JValue)token).Value, CultureInfo.InvariantCulture);
                    // G29 去除多余尾零，同时把负零归一化为零。
                    writer.WriteRawValue((number == decimal.Zero ? decimal.Zero : number)
                        .ToString("G29", CultureInfo.InvariantCulture));
                    return;
                case JTokenType.String:
                    writer.WriteValue((string)((JValue)token).Value);
                    return;
                case JTokenType.Boolean:
                    writer.WriteValue((bool)((JValue)token).Value);
                    return;
                case JTokenType.Null:
                    writer.WriteNull();
                    return;
                default:
                    throw new JsonSerializationException($"Unsupported configuration token {token.Type}.");
            }
        }

        /// <summary>判断字符串是否恰好为 64 位小写十六进制 SHA-256。</summary>
        private static bool IsLowerHexSha256(string value)
        {
            if (value == null || value.Length != 64)
            {
                return false;
            }

            foreach (char character in value)
            {
                bool valid = character >= '0' && character <= '9' || character >= 'a' && character <= 'f';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
