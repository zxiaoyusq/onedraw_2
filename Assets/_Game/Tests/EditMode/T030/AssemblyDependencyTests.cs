using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OneStrokeDemon.Tests.EditMode
{
    public sealed class AssemblyDependencyTests
    {
        private static readonly IReadOnlyDictionary<string, string[]> ExpectedRuntimeDependencies =
            new Dictionary<string, string[]>
            {
                ["OneStrokeDemon.Core"] = Array.Empty<string>(),
                ["OneStrokeDemon.Config"] = new[] { "OneStrokeDemon.Core" },
                ["OneStrokeDemon.Input"] = new[] { "OneStrokeDemon.Core" },
                ["OneStrokeDemon.Combat"] = new[]
                    { "OneStrokeDemon.Core", "OneStrokeDemon.Config", "OneStrokeDemon.Input" },
                ["OneStrokeDemon.Actors"] = new[]
                    { "OneStrokeDemon.Core", "OneStrokeDemon.Config", "OneStrokeDemon.Combat" },
                ["OneStrokeDemon.Skills"] = new[]
                    { "OneStrokeDemon.Core", "OneStrokeDemon.Config", "OneStrokeDemon.Combat", "OneStrokeDemon.Actors" },
                ["OneStrokeDemon.Levels"] = new[]
                    { "OneStrokeDemon.Core", "OneStrokeDemon.Config", "OneStrokeDemon.Actors", "OneStrokeDemon.Skills" },
                ["OneStrokeDemon.Presentation"] = new[]
                {
                    "OneStrokeDemon.Core", "OneStrokeDemon.Config", "OneStrokeDemon.Combat",
                    "OneStrokeDemon.Actors", "OneStrokeDemon.Levels"
                },
                ["OneStrokeDemon.Platform"] = new[] { "OneStrokeDemon.Core" },
                ["OneStrokeDemon.Bootstrap"] = new[]
                {
                    "OneStrokeDemon.Core", "OneStrokeDemon.Config", "OneStrokeDemon.Input",
                    "OneStrokeDemon.Combat", "OneStrokeDemon.Actors", "OneStrokeDemon.Skills",
                    "OneStrokeDemon.Levels", "OneStrokeDemon.Presentation", "OneStrokeDemon.Platform"
                }
            };

        [Test]
        public void RuntimeAssemblyDefinitionsMatchTechnicalDependencyGraphWithoutCycles()
        {
            var definitions = LoadRuntimeDefinitions();
            Assert.That(definitions.Keys, Is.EquivalentTo(ExpectedRuntimeDependencies.Keys));

            foreach (var pair in ExpectedRuntimeDependencies)
            {
                string[] projectReferences = definitions[pair.Key].references
                    .Where(reference => reference.StartsWith("OneStrokeDemon.", StringComparison.Ordinal))
                    .ToArray();
                Assert.That(projectReferences, Is.EquivalentTo(pair.Value), pair.Key);
                Assert.That(definitions[pair.Key].autoReferenced, Is.False, pair.Key);
            }

            AssertGraphIsAcyclic(definitions);
        }

        private static Dictionary<string, AssemblyDefinitionData> LoadRuntimeDefinitions()
        {
            var result = new Dictionary<string, AssemblyDefinitionData>();
            foreach (string guid in AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets/_Game/Scripts" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/Editor/OneStrokeDemon.Editor.asmdef", StringComparison.Ordinal))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                var definition = JsonUtility.FromJson<AssemblyDefinitionData>(asset.text);
                result.Add(definition.name, definition);
            }

            return result;
        }

        private static void AssertGraphIsAcyclic(IReadOnlyDictionary<string, AssemblyDefinitionData> graph)
        {
            var visiting = new HashSet<string>();
            var visited = new HashSet<string>();
            foreach (string assemblyName in graph.Keys)
            {
                Visit(assemblyName, graph, visiting, visited);
            }
        }

        private static void Visit(
            string assemblyName,
            IReadOnlyDictionary<string, AssemblyDefinitionData> graph,
            ISet<string> visiting,
            ISet<string> visited)
        {
            if (visited.Contains(assemblyName))
            {
                return;
            }

            Assert.That(visiting.Add(assemblyName), Is.True, $"Assembly dependency cycle reaches {assemblyName}");
            foreach (string dependency in graph[assemblyName].references.Where(graph.ContainsKey))
            {
                Visit(dependency, graph, visiting, visited);
            }

            visiting.Remove(assemblyName);
            visited.Add(assemblyName);
        }

        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            public string name;
            public string[] references = Array.Empty<string>();
            public bool autoReferenced = true;
        }
    }
}
