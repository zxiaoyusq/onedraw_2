using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneStrokeDemon.Config;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode.T230
{
    internal static class RuntimeConfigTestFixture
    {
        public const string Source = "test:generated-gameplay-config";

        public static string GeneratedJsonPath => Path.Combine(
            ProjectRoot,
            "Assets",
            "_Game",
            "Config",
            "Generated",
            "gameplay_config.json");

        public static string SchemaPath => Path.Combine(
            ProjectRoot,
            "config",
            "schema",
            "gameplay.schema.json");

        public static string LoadJson()
        {
            return File.ReadAllText(GeneratedJsonPath);
        }

        public static JObject LoadRoot()
        {
            return (JObject)GameplayConfigParser.Parse(LoadJson(), Source).Root.DeepClone();
        }

        public static string MutateAndRehash(Action<JObject> mutation)
        {
            JObject root = LoadRoot();
            mutation(root);
            root["contentHash"] = GameplayConfigHash.Calculate(root);
            return root.ToString(Formatting.None);
        }

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
    }
}
