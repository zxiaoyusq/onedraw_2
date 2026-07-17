namespace OneStrokeDemon.Editor.AssetRegistry
{
    // 定义 AssetRegistryPaths 的编辑器工具职责，集中管理资源生成、验证或构建入口。
    public static class AssetRegistryPaths
    {
        public const string GeneratedConfig = "Assets/_Game/Config/Generated/gameplay_config.json";
        public const string CanonicalRegistry = "Assets/_Game/Config/Registry/AssetRegistry.asset";
        public const string PlaceholderFolder = "Assets/_Game/Config/Registry/Placeholders";
        public const string PlaceholderSprite = PlaceholderFolder + "/PlaceholderSprite.asset";
        public const string PlaceholderAudio = PlaceholderFolder + "/PlaceholderAudio.asset";
        public const string PlaceholderPrefab = PlaceholderFolder + "/PlaceholderPrefab.prefab";
        public const string BattleSceneReference = PlaceholderFolder + "/BattleSceneReference.asset";
        public const string BattleScene = "Assets/_Game/Scenes/Battle.unity";
    }
}
