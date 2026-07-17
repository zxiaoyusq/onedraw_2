namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 记录资源注册表成功装载后的配置哈希和各资源类型数量。
    /// </summary>
    public sealed class AssetRegistryLoadSummary
    {
        /// <summary>创建一次成功装载的不可变摘要。</summary>
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

        /// <summary>获取注册表来源。</summary>
        public string Source { get; }

        /// <summary>获取注册表所对应的玩法配置哈希。</summary>
        public string ConfigHash { get; }

        /// <summary>获取注册项总数。</summary>
        public int EntryCount { get; }

        /// <summary>获取预制体数量。</summary>
        public int PrefabCount { get; }

        /// <summary>获取精灵数量。</summary>
        public int SpriteCount { get; }

        /// <summary>获取音频片段数量。</summary>
        public int AudioClipCount { get; }

        /// <summary>获取场景引用数量。</summary>
        public int SceneCount { get; }

        /// <summary>生成一行适合 Unity Console 和自动化日志检索的就绪消息。</summary>
        public string ToLogMessage()
        {
            return $"ASSET_REGISTRY_READY source={Source} configHash={ConfigHash} entries={EntryCount} " +
                $"prefabs={PrefabCount} sprites={SpriteCount} audioClips={AudioClipCount} scenes={SceneCount}";
        }
    }
}
