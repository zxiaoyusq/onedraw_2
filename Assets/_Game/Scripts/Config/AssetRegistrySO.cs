using System.Collections.Generic;
using UnityEngine;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 保存配置资源键与 Unity 资源引用的唯一序列化注册表。
    /// </summary>
    [CreateAssetMenu(fileName = "asset_registry", menuName = "One Stroke Demon/Asset Registry")]
    public sealed class AssetRegistrySO : ScriptableObject
    {
        // 此处只保存资源引用；资源类型和是否必需等规则仍以配置表 AssetManifest 为准。
        [SerializeField]
        private List<AssetRegistryEntry> entries = new List<AssetRegistryEntry>();

        /// <summary>以只读视图返回所有注册项，并兼容旧资源中列表为空的情况。</summary>
        internal IReadOnlyList<AssetRegistryEntry> Entries =>
            entries ?? (IReadOnlyList<AssetRegistryEntry>)System.Array.Empty<AssetRegistryEntry>();

        /// <summary>由编辑器导入流程整体替换注册项，避免运行时形成第二套资源清单。</summary>
        internal void ReplaceEntriesForEditor(IEnumerable<AssetRegistryEntry> replacement)
        {
            entries ??= new List<AssetRegistryEntry>();
            entries.Clear();
            entries.AddRange(replacement);
        }
    }
}
