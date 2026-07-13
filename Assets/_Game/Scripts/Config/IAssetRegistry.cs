using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Config
{
    public interface IAssetRegistry
    {
        int Count { get; }

        UnityObject GetObject(string assetKey);
        T Get<T>(string assetKey) where T : UnityObject;
        GameObject GetPrefab(string assetKey);
        Sprite GetSprite(string assetKey);
        AudioClip GetAudioClip(string assetKey);
        AssetSceneReference GetScene(string assetKey);
    }
}
