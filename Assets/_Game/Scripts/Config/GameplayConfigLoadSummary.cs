namespace OneStrokeDemon.Config
{
    public sealed class GameplayConfigLoadSummary
    {
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

        public string Source { get; }

        public long SchemaVersion { get; }

        public string ContentVersion { get; }

        public string ContentHash { get; }

        public int TableCount => 29;

        public int RecordCount { get; }

        public int PrimaryIndexCount { get; }

        public int GroupIndexCount { get; }

        public string ToLogMessage()
        {
            return $"CONFIG_RUNTIME_READY source={Source} schema={SchemaVersion} content={ContentVersion} " +
                $"hash={ContentHash} tables={TableCount} records={RecordCount} " +
                $"primaryIndexes={PrimaryIndexCount} groupIndexes={GroupIndexCount}";
        }
    }
}
