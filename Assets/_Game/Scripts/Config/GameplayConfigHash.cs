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
    internal static class GameplayConfigHash
    {
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

        internal static string Calculate(JObject root)
        {
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

        private static void WriteCanonicalToken(JsonWriter writer, JToken token, bool isRoot)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    writer.WriteStartObject();
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
