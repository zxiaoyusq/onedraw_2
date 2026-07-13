using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Config
{
    [Serializable]
    public sealed class AssetRegistryEntry
    {
        [SerializeField]
        private string assetKey = string.Empty;

        [SerializeField]
        private UnityObject asset;

        internal AssetRegistryEntry(string assetKey, UnityObject asset)
        {
            this.assetKey = assetKey;
            this.asset = asset;
        }

        public string AssetKey => assetKey;

        public UnityObject Asset => asset;
    }
}
