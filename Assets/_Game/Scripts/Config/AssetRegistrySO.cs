using System.Collections.Generic;
using UnityEngine;

namespace OneStrokeDemon.Config
{
    [CreateAssetMenu(fileName = "asset_registry", menuName = "One Stroke Demon/Asset Registry")]
    public sealed class AssetRegistrySO : ScriptableObject
    {
        [SerializeField]
        private List<AssetRegistryEntry> entries = new List<AssetRegistryEntry>();

        internal IReadOnlyList<AssetRegistryEntry> Entries =>
            entries ?? (IReadOnlyList<AssetRegistryEntry>)System.Array.Empty<AssetRegistryEntry>();

        internal void ReplaceEntriesForEditor(IEnumerable<AssetRegistryEntry> replacement)
        {
            entries ??= new List<AssetRegistryEntry>();
            entries.Clear();
            entries.AddRange(replacement);
        }
    }
}
