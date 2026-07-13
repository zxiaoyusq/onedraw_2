namespace OneStrokeDemon.Config
{
    public sealed class AssetRegistryLoadSummary
    {
        internal AssetRegistryLoadSummary(
            string source,
            string configHash,
            int entryCount,
            int prefabCount,
            int spriteCount,
            int audioClipCount,
            int sceneCount)
        {
            Source = source;
            ConfigHash = configHash;
            EntryCount = entryCount;
            PrefabCount = prefabCount;
            SpriteCount = spriteCount;
            AudioClipCount = audioClipCount;
            SceneCount = sceneCount;
        }

        public string Source { get; }

        public string ConfigHash { get; }

        public int EntryCount { get; }

        public int PrefabCount { get; }

        public int SpriteCount { get; }

        public int AudioClipCount { get; }

        public int SceneCount { get; }

        public string ToLogMessage()
        {
            return $"ASSET_REGISTRY_READY source={Source} configHash={ConfigHash} entries={EntryCount} " +
                $"prefabs={PrefabCount} sprites={SpriteCount} audioClips={AudioClipCount} scenes={SceneCount}";
        }
    }
}
