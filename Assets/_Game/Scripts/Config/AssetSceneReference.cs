using UnityEngine;

namespace OneStrokeDemon.Config
{
    [CreateAssetMenu(fileName = "scene_reference", menuName = "One Stroke Demon/Scene Reference")]
    public sealed class AssetSceneReference : ScriptableObject
    {
        [SerializeField]
        private string scenePath = string.Empty;

        public string ScenePath => scenePath;

        public string SceneName
        {
            get
            {
                int slash = scenePath.LastIndexOf('/');
                int extension = scenePath.LastIndexOf('.');
                int start = slash < 0 ? 0 : slash + 1;
                int length = extension > start ? extension - start : scenePath.Length - start;
                return length > 0 ? scenePath.Substring(start, length) : string.Empty;
            }
        }

        internal void SetScenePathForEditor(string value)
        {
            scenePath = value ?? string.Empty;
        }
    }
}
