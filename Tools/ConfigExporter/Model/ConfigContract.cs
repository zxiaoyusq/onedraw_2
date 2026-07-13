namespace OneStrokeDemon.ConfigExporter.Model;

internal sealed record SheetContract(
    string SheetName,
    string JsonPropertyName,
    IReadOnlyList<string> SortFields)
{
    public string SchemaDefinitionName => $"{SheetName}Row";
}

internal sealed record GroupOrderContract(
    string SheetName,
    string GroupField,
    string OrderField);

internal static class ConfigContract
{
    public const string ReadmeSheetName = "README";
    public const string FieldDictionarySheetName = "FieldDictionary";

    public static readonly IReadOnlyList<SheetContract> DataSheets = new[]
    {
        Sheet("Global", "global", "key"),
        Sheet("Players", "players", "playerId"),
        Sheet("Stances", "stances", "stanceId"),
        Sheet("StrokeRules", "strokeRules", "ruleId"),
        Sheet("DamageFormulas", "damageFormulas", "formulaId"),
        Sheet("DefenseRules", "defenseRules", "defenseRuleId"),
        Sheet("WeakpointRules", "weakpointRules", "weakpointRuleId"),
        Sheet("MovePatterns", "movePatterns", "movePatternId"),
        Sheet("Enemies", "enemies", "enemyId"),
        Sheet("EnemyAttacks", "enemyAttacks", "attackSetId", "order", "attackId"),
        Sheet("Projectiles", "projectiles", "projectileId"),
        Sheet("Buffs", "buffs", "buffId"),
        Sheet("Skills", "skills", "skillId"),
        Sheet("SkillEffects", "skillEffects", "effectGroupId", "order"),
        Sheet("Levels", "levels", "levelId"),
        Sheet("Waves", "waves", "levelId", "order", "waveId"),
        Sheet("SpawnPoints", "spawnPoints", "spawnPointId"),
        Sheet("EnemyModifiers", "enemyModifiers", "modifierId"),
        Sheet("Spawns", "spawns", "spawnId"),
        Sheet("BossPhases", "bossPhases", "enemyId", "order", "bossPhaseId"),
        Sheet("Rewards", "rewards", "rewardTableId", "order"),
        Sheet("Tutorials", "tutorials", "tutorialId", "order"),
        Sheet("Texts", "texts", "textKey"),
        Sheet("AudioCues", "audioCues", "audioKey"),
        Sheet("VfxCues", "vfxCues", "vfxKey"),
        Sheet("AssetManifest", "assetManifest", "assetKey"),
        Sheet("Enums", "enums", "enumType", "value"),
        Sheet(FieldDictionarySheetName, "fieldDictionary"),
    };

    public static readonly IReadOnlyList<string> WorkbookSheetOrder = new[] { ReadmeSheetName }
        .Concat(DataSheets.Select(sheet => sheet.SheetName))
        .ToArray();

    public static readonly IReadOnlyList<string> TopLevelPropertyOrder = new[]
        {
            "schemaVersion",
            "contentVersion",
            "contentHash",
        }
        .Concat(DataSheets.Select(sheet => sheet.JsonPropertyName))
        .ToArray();

    public static readonly IReadOnlyList<FieldDefinition> FieldDictionaryFields = new[]
    {
        DictionaryField("sheet", ConfigValueKind.String, required: true, order: 0),
        DictionaryField("field", ConfigValueKind.String, required: true, order: 1),
        DictionaryField("type", ConfigValueKind.String, required: true, order: 2),
        DictionaryField("required", ConfigValueKind.String, required: true, order: 3),
        DictionaryField("default", ConfigValueKind.String, required: true, order: 4),
        DictionaryField("min", ConfigValueKind.Float, required: false, order: 5),
        DictionaryField("max", ConfigValueKind.Float, required: false, order: 6),
        DictionaryField("enumType", ConfigValueKind.String, required: true, order: 7),
        DictionaryField("foreignKey", ConfigValueKind.String, required: true, order: 8),
        DictionaryField("description", ConfigValueKind.String, required: true, order: 9),
    };

    public static readonly IReadOnlyList<GroupOrderContract> GroupOrders = new[]
    {
        new GroupOrderContract("EnemyAttacks", "attackSetId", "order"),
        new GroupOrderContract("SkillEffects", "effectGroupId", "order"),
        new GroupOrderContract("Waves", "levelId", "order"),
        new GroupOrderContract("BossPhases", "enemyId", "order"),
        new GroupOrderContract("Rewards", "rewardTableId", "order"),
        new GroupOrderContract("Tutorials", "tutorialId", "order"),
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> RegisteredStrategyEnums =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["MovePatternType"] = new HashSet<string>(
                new[] { "Boss", "Dive", "Hover", "Linear", "Sine" },
                StringComparer.Ordinal),
            ["AttackTriggerType"] = new HashSet<string>(
                new[] { "Cooldown", "Distance", "HpThreshold", "Support" },
                StringComparer.Ordinal),
            ["EffectType"] = new HashSet<string>(
                new[]
                {
                    "ApplyBuff", "ClearProjectiles", "Damage", "DamageMultiplier",
                    "ExecuteBelowHpRatio", "Heal", "IncrementCounter", "Knockback",
                    "PlayVfx", "RemoveArmor", "RepeatStroke", "TimeScale",
                },
                StringComparer.Ordinal),
            ["TargetType"] = new HashSet<string>(
                new[]
                {
                    "AllEnemies", "Battle", "Boss", "EnemiesInRadius",
                    "EnemiesInsideGesture", "LastStrokeTargets", "NextStroke",
                    "NormalEnemies", "Target",
                },
                StringComparer.Ordinal),
        };

    public static SheetContract GetDataSheet(string sheetName)
    {
        return DataSheets.FirstOrDefault(sheet => string.Equals(sheet.SheetName, sheetName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Unknown data sheet '{sheetName}'.");
    }

    public static IReadOnlyList<string> GetPrimaryKeyFields(string sheetName)
    {
        return sheetName switch
        {
            "EnemyAttacks" => new[] { "attackId" },
            "Waves" => new[] { "waveId" },
            "BossPhases" => new[] { "bossPhaseId" },
            FieldDictionarySheetName => new[] { "sheet", "field" },
            _ => GetDataSheet(sheetName).SortFields,
        };
    }

    private static SheetContract Sheet(string sheetName, string jsonPropertyName, params string[] sortFields)
    {
        return new SheetContract(sheetName, jsonPropertyName, sortFields);
    }

    private static FieldDefinition DictionaryField(
        string fieldName,
        ConfigValueKind kind,
        bool required,
        int order)
    {
        return new FieldDefinition(
            FieldDictionarySheetName,
            fieldName,
            kind,
            required,
            order,
            DictionaryRowNumber: 0);
    }
}
