using UnityEngine;

namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 以 Unity 资源保存可构建场景路径，并提供不含目录和扩展名的场景名。
    /// </summary>
    [CreateAssetMenu(fileName = "scene_reference", menuName = "One Stroke Demon/Scene Reference")]
    public sealed class AssetSceneReference : ScriptableObject
    {
        /// <summary>项目内场景资源路径。</summary>
        [SerializeField]
        private string scenePath = string.Empty;

        /// <summary>获取项目内场景资源路径。</summary>
        public string ScenePath => scenePath;

        /// <summary>获取可传给场景加载 API 的无扩展名场景名。</summary>
        public string SceneName
        {
            get
            {
                // 同时兼容完整 Assets 路径、裸文件名和无扩展名输入。
                int slash = scenePath.LastIndexOf('/');
                int extension = scenePath.LastIndexOf('.');
                int start = slash < 0 ? 0 : slash + 1;
                int length = extension > start ? extension - start : scenePath.Length - start;
                return length > 0 ? scenePath.Substring(start, length) : string.Empty;
            }
        }

        /// <summary>由编辑器同步流程写入场景路径，空值统一保存为空字符串。</summary>
        internal void SetScenePathForEditor(string value)
        {
            scenePath = value ?? string.Empty;
        }
    }
}
