using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneStrokeDemon.Config
{
    internal static class GameplayConfigParser
    {
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

                    if (jsonReader.Read())
                    {
                        throw new JsonReaderException("Unexpected content follows the root object.");
                    }
                }

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
            catch (GameplayConfigException)
            {
                throw;
            }
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

        private sealed class StrictJsonTextReader : JsonTextReader
        {
            public StrictJsonTextReader(TextReader reader)
                : base(reader)
            {
            }

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

    internal sealed class ParsedGameplayConfig
    {
        public ParsedGameplayConfig(JObject root, GameplayConfigDocument document)
        {
            Root = root;
            Document = document;
        }

        public JObject Root { get; }

        public GameplayConfigDocument Document { get; }
    }
}
