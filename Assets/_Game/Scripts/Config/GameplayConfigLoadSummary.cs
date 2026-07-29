namespace OneStrokeDemon.Config
{
    /// <summary>
    /// 记录玩法配置成功装载后的版本、哈希、记录数和索引统计。
    /// </summary>
    public sealed class GameplayConfigLoadSummary
    {
        /// <summary>创建一次成功装载的不可变摘要。</summary>
        internal GameplayConfigLoadSummary(
            string source,
            long schemaVersion,
            string contentVersion,
            string contentHash,
            int recordCount,
            int primaryIndexCount,
            int groupIndexCount)
        {
            Source = source;
            SchemaVersion = schemaVersion;
            ContentVersion = contentVersion;
            ContentHash = contentHash;
            RecordCount = recordCount;
            PrimaryIndexCount = primaryIndexCount;
            GroupIndexCount = groupIndexCount;
        }

        /// <summary>获取配置来源。</summary>
        public string Source { get; }

        /// <summary>获取结构版本。</summary>
        public long SchemaVersion { get; }

        /// <summary>获取内容版本。</summary>
        public string ContentVersion { get; }

        /// <summary>获取规范化内容哈希。</summary>
        public string ContentHash { get; }

        /// <summary>获取当前固定配置表数量。</summary>
        public int TableCount => 30;

        /// <summary>获取全部配置记录数量。</summary>
        public int RecordCount { get; }

        /// <summary>获取按主键建立的索引数量。</summary>
        public int PrimaryIndexCount { get; }

        /// <summary>获取按外键分组建立的索引数量。</summary>
        public int GroupIndexCount { get; }

        /// <summary>生成一行适合 Unity Console 和自动化日志检索的就绪消息。</summary>
        public string ToLogMessage()
        {
            return $"CONFIG_RUNTIME_READY source={Source} schema={SchemaVersion} content={ContentVersion} " +
                $"hash={ContentHash} tables={TableCount} records={RecordCount} " +
                $"primaryIndexes={PrimaryIndexCount} groupIndexes={GroupIndexCount}";
        }
    }
}
