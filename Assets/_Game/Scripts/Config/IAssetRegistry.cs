using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 按配置中的稳定资源键提供运行时 Unity 资源查询。
    /// </summary>
    public interface IAssetRegistry
    {
        /// <summary>获取已注册资源总数。</summary>
        int Count { get; }

        /// <summary>按资源键获取不限定具体类型的 Unity 对象。</summary>
        UnityObject GetObject(string assetKey);

        /// <summary>按资源键获取指定 Unity 对象类型，类型不匹配时抛出注册表异常。</summary>
        T Get<T>(string assetKey) where T : UnityObject;

        /// <summary>按资源键获取预制体。</summary>
        GameObject GetPrefab(string assetKey);

        /// <summary>按资源键获取精灵。</summary>
        Sprite GetSprite(string assetKey);

        /// <summary>按资源键获取音频片段。</summary>
        AudioClip GetAudioClip(string assetKey);

        /// <summary>按资源键获取场景引用。</summary>
        AssetSceneReference GetScene(string assetKey);
    }
}
