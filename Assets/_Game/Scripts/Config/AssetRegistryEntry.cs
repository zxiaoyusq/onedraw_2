using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 将一个稳定资源键绑定到一个 Unity 资源对象。
    /// </summary>
    [Serializable]
    public sealed class AssetRegistryEntry
    {
        /// <summary>配置表 AssetManifest 中定义的稳定资源键。</summary>
        [SerializeField]
        private string assetKey = string.Empty;

        /// <summary>与资源键对应的 Unity 资源引用。</summary>
        [SerializeField]
        private UnityObject asset;

        /// <summary>创建供编辑器构建和测试使用的注册项。</summary>
        internal AssetRegistryEntry(string assetKey, UnityObject asset)
        {
            this.assetKey = assetKey;
            this.asset = asset;
        }

        /// <summary>获取稳定资源键。</summary>
        public string AssetKey => assetKey;

        /// <summary>获取绑定的 Unity 资源对象。</summary>
        public UnityObject Asset => asset;
    }
}
