using System.Globalization;
using System.Text.RegularExpressions;
using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Validation;

internal static partial class SemanticValidator
{
    public static void Validate(ConfigValidationContext context)
    {
        ValidateContentVersion(context);
        ValidateFeedbackColors(context);
        ValidateStrokeTrailColors(context);
        ValidateGlobalTypedUnion(context);
        ValidateOrderedPairs(context);
        ValidateGroupOrders(context);
        ValidateLevelStarThresholds(context);
        ValidateRewardConditions(context);
        ValidateLevelWaveSpawnGraph(context);
        ValidateBossPhases(context);
    }

    private static void ValidateFeedbackColors(ConfigValidationContext context)
    {
        var table = context.Document.GetRequiredTable("FeedbackCues");
        foreach (var row in table.Rows)
        {
            foreach (var fieldName in new[] { "damageNumberColorHex", "vfxTintColorHex" })
            {
                var value = ConfigValues.String(row, fieldName);
                if (ColorHexRegex().IsMatch(value))
                {
                    continue;
                }

                throw Failure(
                    "CFG009",
                    $"Color '{value}' must use #RRGGBBAA hexadecimal format.",
                    table,
                    row,
                    fieldName);
            }
        }
    }

    // 画笔颜色与其他表现色共用#RRGGBBAA合同，避免运行时静默回退到错误颜色。
    private static void ValidateStrokeTrailColors(ConfigValidationContext context)
    {
        var table = context.Document.GetRequiredTable("StrokeTrailStyles");
        foreach (var row in table.Rows)
        {
            foreach (var fieldName in new[]
                     {
                         "outerColorHex",
                         "bodyColorHex",
                         "coreColorHex",
                         "branchColorHex",
                     })
            {
                var value = ConfigValues.String(row, fieldName);
                if (ColorHexRegex().IsMatch(value))
                {
                    continue;
                }

                throw Failure(
                    "CFG009",
                    $"Color '{value}' must use #RRGGBBAA hexadecimal format.",
                    table,
                    row,
                    fieldName);
            }
        }
    }

    private static void ValidateContentVersion(ConfigValidationContext context)
    {
        var table = context.Document.GetRequiredTable("Global");
        var row = table.Rows.Single(candidate => string.Equals(
            ConfigValues.String(candidate, "key"),
            "content_version",
            StringComparison.Ordinal));
        if (!ContentVersionRegex().IsMatch(context.Document.ContentVersion))
        {
            throw Failure(
                "CFG009",
                $"Content version '{context.Document.ContentVersion}' must be semantic version text.",
                table,
                row,
                "stringValue");
        }
    }

    private static void ValidateGlobalTypedUnion(ConfigValidationContext context)
    {
        var valueFields = new[] { "intValue", "floatValue", "stringValue", "boolValue" };
        foreach (var row in context.Document.GetRequiredTable("Global").Rows)
        {
            var valueType = ConfigValues.String(row, "valueType");
            var expectedField = valueType switch
            {
                "int" => "intValue",
                "float" => "floatValue",
                "string" => "stringValue",
                "bool" => "boolValue",
                _ => throw new ConfigExportException(
                    "CFG009",
                    $"Global valueType '{valueType}' has no matching value field.",
                    "Global",
                    row.ExcelRowNumber,
                    "valueType"),
            };
            var populatedFields = valueFields
                .Where(field => !ConfigValues.IsEmpty(row.GetValue(field)))
                .ToArray();
            if (populatedFields.Length != 1 || !string.Equals(
                    populatedFields[0],
                    expectedField,
                    StringComparison.Ordinal))
            {
                throw new ConfigExportException(
                    "CFG009",
                    $"Global valueType '{valueType}' requires only '{expectedField}' to be populated; " +
                    $"actual [{string.Join(", ", populatedFields)}].",
                    "Global",
                    row.ExcelRowNumber,
                    expectedField);
            }
        }
    }

    private static void ValidateOrderedPairs(ConfigValidationContext context)
    {
        var contracts = new[]
        {
            new OrderedPair("WeakpointRules", "windowStartSec", "windowEndSec"),
            new OrderedPair("EnemyAttacks", "interruptStartSec", "interruptEndSec"),
            new OrderedPair("AudioCues", "pitchMin", "pitchMax"),
        };
        foreach (var contract in contracts)
        {
            var table = context.Document.GetRequiredTable(contract.SheetName);
            foreach (var row in table.Rows)
            {
                var minimum = ConfigValues.Number(row, contract.MinimumField);
                var maximum = ConfigValues.Number(row, contract.MaximumField);
                if (minimum > maximum)
                {
                    throw Failure(
                        "CFG009",
                        $"{contract.MinimumField} ({minimum}) must not exceed " +
                        $"{contract.MaximumField} ({maximum}).",
                        table,
                        row,
                        contract.MaximumField);
                }
            }
        }
    }

    private static void ValidateGroupOrders(ConfigValidationContext context)
    {
        foreach (var contract in ConfigContract.GroupOrders)
        {
            var table = context.Document.GetRequiredTable(contract.SheetName);
            foreach (var group in table.Rows.GroupBy(
                         row => ConfigValues.String(row, contract.GroupField),
                         StringComparer.Ordinal))
            {
                var orderedRows = group
                    .OrderBy(row => ConfigValues.Integer(row, contract.OrderField))
                    .ThenBy(row => row.ExcelRowNumber)
                    .ToArray();
                for (var index = 0; index < orderedRows.Length; index += 1)
                {
                    var expectedOrder = index + 1L;
                    var actualOrder = ConfigValues.Integer(orderedRows[index], contract.OrderField);
                    if (actualOrder != expectedOrder)
                    {
                        throw Failure(
                            "CFG009",
                            $"Group '{group.Key}' order must be continuous from 1; " +
                            $"expected {expectedOrder}, actual {actualOrder}.",
                            table,
                            orderedRows[index],
                            contract.OrderField);
                    }
                }
            }
        }
    }

    private static void ValidateLevelStarThresholds(ConfigValidationContext context)
    {
        var table = context.Document.GetRequiredTable("Levels");
        foreach (var row in table.Rows)
        {
            var first = ConfigValues.Integer(row, "starScore1");
            var second = ConfigValues.Integer(row, "starScore2");
            var third = ConfigValues.Integer(row, "starScore3");
            if (first < second && second < third)
            {
                continue;
            }

            throw Failure(
                "CFG009",
                $"Star score thresholds must be strictly increasing, actual {first}, {second}, {third}.",
                table,
                row,
                first >= second ? "starScore2" : "starScore3");
        }
    }

    private static void ValidateRewardConditions(ConfigValidationContext context)
    {
        var table = context.Document.GetRequiredTable("Rewards");
        foreach (var row in table.Rows)
        {
            var conditionType = ConfigValues.String(row, "conditionType");
            var rawValue = ConfigValues.String(row, "conditionValue");
            var parsed = long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value);
            var valid = conditionType switch
            {
                "Clear" => parsed && value == 1,
                "StarAtLeast" => parsed && value is >= 1 and <= 3,
                "ScoreAtLeast" => parsed && value >= 0,
                _ => false,
            };
            if (!valid)
            {
                throw Failure(
                    "CFG009",
                    $"Reward conditionValue '{rawValue}' is invalid for conditionType '{conditionType}'.",
                    table,
                    row,
                    "conditionValue");
            }
        }
    }

    private static void ValidateLevelWaveSpawnGraph(ConfigValidationContext context)
    {
        var levels = context.Document.GetRequiredTable("Levels");
        var waves = context.Document.GetRequiredTable("Waves");
        var spawns = context.Document.GetRequiredTable("Spawns");
        var spawnPoints = context.Document.GetRequiredTable("SpawnPoints");
        var wavesByLevel = waves.Rows.ToLookup(
            row => ConfigValues.String(row, "levelId"),
            StringComparer.Ordinal);
        var spawnsByWave = spawns.Rows.ToLookup(
            row => ConfigValues.String(row, "waveId"),
            StringComparer.Ordinal);

        foreach (var level in levels.Rows)
        {
            var levelId = ConfigValues.String(level, "levelId");
            if (!wavesByLevel.Contains(levelId))
            {
                throw Failure(
                    "CFG010",
                    $"Level '{levelId}' has no Waves rows.",
                    levels,
                    level,
                    "levelId");
            }
        }

        foreach (var wave in waves.Rows)
        {
            var waveId = ConfigValues.String(wave, "waveId");
            if (!spawnsByWave.Contains(waveId))
            {
                throw Failure(
                    "CFG010",
                    $"Wave '{waveId}' has no Spawns rows.",
                    waves,
                    wave,
                    "waveId");
            }
        }

        var levelByWave = waves.Rows.ToDictionary(
            row => ConfigValues.String(row, "waveId"),
            row => ConfigValues.String(row, "levelId"),
            StringComparer.Ordinal);
        var scopeBySpawnPoint = spawnPoints.Rows.ToDictionary(
            row => ConfigValues.String(row, "spawnPointId"),
            row => ConfigValues.String(row, "levelId"),
            StringComparer.Ordinal);
        foreach (var spawn in spawns.Rows)
        {
            var waveId = ConfigValues.String(spawn, "waveId");
            var spawnPointId = ConfigValues.String(spawn, "spawnPointId");
            var levelId = levelByWave[waveId];
            var scope = scopeBySpawnPoint[spawnPointId];
            if (scope == "*" || string.Equals(scope, levelId, StringComparison.Ordinal))
            {
                continue;
            }

            throw Failure(
                "CFG010",
                $"Spawn point '{spawnPointId}' is scoped to level '{scope}', not wave level '{levelId}'.",
                spawns,
                spawn,
                "spawnPointId");
        }
    }

    private static void ValidateBossPhases(ConfigValidationContext context)
    {
        var enemies = context.Document.GetRequiredTable("Enemies");
        var phases = context.Document.GetRequiredTable("BossPhases");
        var levels = context.Document.GetRequiredTable("Levels");
        var waves = context.Document.GetRequiredTable("Waves");
        var spawns = context.Document.GetRequiredTable("Spawns");
        var enemyRows = enemies.Rows.ToDictionary(
            row => ConfigValues.String(row, "enemyId"),
            StringComparer.Ordinal);
        var phasesByEnemy = phases.Rows.ToLookup(
            row => ConfigValues.String(row, "enemyId"),
            StringComparer.Ordinal);

        foreach (var group in phasesByEnemy)
        {
            var enemy = enemyRows[group.Key];
            if (!string.Equals(ConfigValues.String(enemy, "tier"), "Boss", StringComparison.Ordinal))
            {
                throw Failure(
                    "CFG010",
                    $"BossPhases enemy '{group.Key}' is not tier Boss.",
                    phases,
                    group.First(),
                    "enemyId");
            }

            var ordered = group.OrderBy(row => ConfigValues.Integer(row, "order")).ToArray();
            if (ConfigValues.Number(ordered[0], "enterHpRatio") != decimal.One)
            {
                throw Failure(
                    "CFG010",
                    "First Boss phase must enter at HP ratio 1.",
                    phases,
                    ordered[0],
                    "enterHpRatio");
            }

            if (ConfigValues.Number(ordered[^1], "exitHpRatio") != decimal.Zero)
            {
                throw Failure(
                    "CFG010",
                    "Last Boss phase must exit at HP ratio 0.",
                    phases,
                    ordered[^1],
                    "exitHpRatio");
            }

            for (var index = 0; index < ordered.Length; index += 1)
            {
                var enter = ConfigValues.Number(ordered[index], "enterHpRatio");
                var exit = ConfigValues.Number(ordered[index], "exitHpRatio");
                if (enter <= exit)
                {
                    throw Failure(
                        "CFG010",
                        $"Boss phase enterHpRatio {enter} must be greater than exitHpRatio {exit}.",
                        phases,
                        ordered[index],
                        "exitHpRatio");
                }

                if (index == 0)
                {
                    continue;
                }

                var previousExit = ConfigValues.Number(ordered[index - 1], "exitHpRatio");
                if (enter != previousExit)
                {
                    throw Failure(
                        "CFG010",
                        $"Boss phase HP coverage has a gap or overlap: previous exit {previousExit}, " +
                        $"current enter {enter}.",
                        phases,
                        ordered[index],
                        "enterHpRatio");
                }
            }
        }

        foreach (var enemy in enemies.Rows.Where(row => string.Equals(
                     ConfigValues.String(row, "tier"),
                     "Boss",
                     StringComparison.Ordinal)))
        {
            var enemyId = ConfigValues.String(enemy, "enemyId");
            if (!phasesByEnemy.Contains(enemyId))
            {
                throw Failure(
                    "CFG010",
                    $"Boss enemy '{enemyId}' has no BossPhases rows.",
                    enemies,
                    enemy,
                    "enemyId");
            }
        }

        var wavesByLevel = waves.Rows.ToLookup(
            row => ConfigValues.String(row, "levelId"),
            StringComparer.Ordinal);
        foreach (var level in levels.Rows)
        {
            var bossEnemyId = ConfigValues.String(level, "bossEnemyId");
            if (bossEnemyId.Length == 0)
            {
                continue;
            }

            var enemy = enemyRows[bossEnemyId];
            if (!string.Equals(ConfigValues.String(enemy, "tier"), "Boss", StringComparison.Ordinal) ||
                !phasesByEnemy.Contains(bossEnemyId))
            {
                throw Failure(
                    "CFG010",
                    $"Level bossEnemyId '{bossEnemyId}' must reference a tier Boss enemy with phases.",
                    levels,
                    level,
                    "bossEnemyId");
            }

            var levelId = ConfigValues.String(level, "levelId");
            if (!wavesByLevel[levelId].Any(wave => string.Equals(
                    ConfigValues.String(wave, "endCondition"),
                    "BossDefeated",
                    StringComparison.Ordinal)))
            {
                throw Failure(
                    "CFG010",
                    $"Boss level '{levelId}' has no BossDefeated wave.",
                    levels,
                    level,
                    "bossEnemyId");
            }

            var levelWaveIds = new HashSet<string>(
                wavesByLevel[levelId].Select(wave => ConfigValues.String(wave, "waveId")),
                StringComparer.Ordinal);
            if (!spawns.Rows.Any(spawn =>
                    levelWaveIds.Contains(ConfigValues.String(spawn, "waveId")) &&
                    string.Equals(ConfigValues.String(spawn, "enemyId"), bossEnemyId, StringComparison.Ordinal)))
            {
                throw Failure(
                    "CFG010",
                    $"Boss level '{levelId}' never spawns boss '{bossEnemyId}'.",
                    levels,
                    level,
                    "bossEnemyId");
            }
        }
    }

    private static ConfigExportException Failure(
        string code,
        string message,
        ConfigTable table,
        ConfigRow row,
        string fieldName)
    {
        return new ConfigExportException(
            code,
            message,
            table.Contract.SheetName,
            row.ExcelRowNumber,
            fieldName);
    }

    private sealed record OrderedPair(
        string SheetName,
        string MinimumField,
        string MaximumField);

    [GeneratedRegex(
        "^[0-9]+\\.[0-9]+\\.[0-9]+(?:-[a-z0-9][a-z0-9.-]*)?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ContentVersionRegex();

    [GeneratedRegex("^#[0-9A-Fa-f]{8}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorHexRegex();
}
