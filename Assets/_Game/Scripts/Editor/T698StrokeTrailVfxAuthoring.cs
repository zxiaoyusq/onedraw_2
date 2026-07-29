using OneStrokeDemon.Editor.AssetRegistry;
using OneStrokeDemon.Presentation;
using UnityEditor;
using UnityEngine;

namespace OneStrokeDemon.Editor
{
    /// <summary>通过Unity序列化API创建方案C画笔Prefab，禁止手工维护YAML。</summary>
    public static class T698StrokeTrailVfxAuthoring
    {
        public const string PrefabPath = "Assets/_Game/Art/VFX/vfx_slash.prefab";
        public const string MenuPath = "One Stroke Demon/T698/Create Lightning Stroke Trail Prefab";
        private const string CompatibilitySpritePath =
            "Assets/_Game/Art/VFX/Sprites/vfx_slash_arc.png";

        [MenuItem(MenuPath)]
        /// <summary>创建或完全修复外层、主体、核心和固定分支池的Prefab拓扑。</summary>
        public static void CreateOrRepairPrefab()
        {
            GameObject root = null;
            try
            {
                root = new GameObject("vfx_slash");
                LineRenderer outer = root.AddComponent<LineRenderer>();
                root.AddComponent<VfxPoolItem>();
                var view = root.AddComponent<StrokeTrailView>();
                LineRenderer body = CreateRenderer(root.transform, "Body");
                LineRenderer core = CreateRenderer(root.transform, "Core");
                CreateCompatibilitySpriteRenderer(root.transform);

                var branchesRoot = new GameObject("Branches");
                branchesRoot.transform.SetParent(root.transform, false);
                var branches = new LineRenderer[StrokeTrailView.BranchRendererCapacity];
                for (int index = 0; index < branches.Length; index++)
                {
                    branches[index] = CreateRenderer(
                        branchesRoot.transform,
                        $"Branch {index + 1:00}");
                }

                PrepareAuthoringRenderer(outer);
                PrepareAuthoringRenderer(body);
                PrepareAuthoringRenderer(core);
                for (int index = 0; index < branches.Length; index++)
                {
                    PrepareAuthoringRenderer(branches[index]);
                }

                view.ConfigureRenderersForAuthoring(outer, body, core, branches);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    PrefabPath,
                    out bool success);
                if (!success || prefab == null)
                {
                    throw new UnityException(
                        $"Failed to save lightning stroke trail prefab at {PrefabPath}.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AssetRegistryEditorValidator.ValidateCanonical();
                Selection.activeObject = prefab;
                Debug.Log(
                    $"T698_STROKE_TRAIL_PREFAB_PASS path={PrefabPath} " +
                    $"branches={StrokeTrailView.BranchRendererCapacity}");
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static LineRenderer CreateRenderer(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.AddComponent<LineRenderer>();
        }

        // 旧资源验收要求VFX Prefab保留非空SpriteRenderer；运行时关闭它，实际表现仍完全由LineRenderer驱动。
        private static void CreateCompatibilitySpriteRenderer(Transform parent)
        {
            Sprite sprite = null;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(CompatibilitySpritePath);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is Sprite candidate)
                {
                    sprite = candidate;
                    break;
                }
            }

            if (sprite == null)
            {
                throw new UnityException(
                    $"Missing compatibility sprite at {CompatibilitySpritePath}.");
            }

            var child = new GameObject("Compatibility Sprite");
            child.transform.SetParent(parent, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "VFX";
            renderer.enabled = false;
        }

        // Prefab只记录稳定组件拓扑；材质、颜色、宽度和排序均在运行时从配置覆盖。
        private static void PrepareAuthoringRenderer(LineRenderer renderer)
        {
            renderer.enabled = false;
            renderer.positionCount = 0;
            renderer.useWorldSpace = true;
            renderer.loop = false;
        }
    }
}
