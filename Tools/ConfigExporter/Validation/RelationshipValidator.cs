using System.Text.RegularExpressions;
using OneStrokeDemon.ConfigExporter.Diagnostics;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Validation;

internal static partial class RelationshipValidator
{
    private static readonly IReadOnlyList<ExplicitForeignKey> ExplicitGroupForeignKeys = new[]
    {
        new ExplicitForeignKey("Enemies", "attackSetId", "EnemyAttacks", "attackSetId"),
        new ExplicitForeignKey("BossPhases", "attackSetId", "EnemyAttacks", "attackSetId"),
    };

    public static void Validate(ConfigValidationContext context)
    {
        ValidateDeclaredForeignKeys(context);
        ValidateExplicitGroupForeignKeys(context);
    }

    private static void ValidateDeclaredForeignKeys(ConfigValidationContext context)
    {
        foreach (var constraint in context.Fields.Values.Where(field => field.ForeignKey.Length > 0))
        {
            if (string.Equals(constraint.ForeignKey, "conditional", StringComparison.Ordinal))
            {
                if (!string.Equals(constraint.SheetName, "Rewards", StringComparison.Ordinal) ||
                    !string.Equals(constraint.FieldName, "rewardId", StringComparison.Ordinal))
                {
                    throw MetadataFailure(
                        "CFG002",
                        "The conditional foreign key is only allowed on Rewards.rewardId.",
                        constraint);
                }

                ValidateConditionalRewards(context, constraint);
                continue;
            }

            var separator = constraint.ForeignKey.IndexOf('.', StringComparison.Ordinal);
            if (separator <= 0 || separator != constraint.ForeignKey.LastIndexOf('.') ||
                separator == constraint.ForeignKey.Length - 1)
            {
                throw MetadataFailure(
                    "CFG002",
                    $"Foreign key '{constraint.ForeignKey}' must use Sheet.field syntax.",
                    constraint);
            }

            var targetSheetName = constraint.ForeignKey[..separator];
            var targetFieldName = constraint.ForeignKey[(separator + 1)..];
            ConfigTable targetTable;
            try
            {
                targetTable = context.Document.GetRequiredTable(targetSheetName);
            }
            catch (InvalidOperationException)
            {
                throw MetadataFailure(
                    "CFG002",
                    $"Foreign key target sheet '{targetSheetName}' does not exist.",
                    constraint);
            }

            if (!targetTable.FieldOrder.Contains(targetFieldName, StringComparer.Ordinal))
            {
                throw MetadataFailure(
                    "CFG002",
                    $"Foreign key target field '{targetSheetName}.{targetFieldName}' does not exist.",
                    constraint);
            }

            var targetConstraint = context.GetField(targetSheetName, targetFieldName);
            if (targetConstraint.Kind != constraint.Kind)
            {
                throw MetadataFailure(
                    "CFG002",
                    $"Foreign key source type {constraint.Kind} does not match target type {targetConstraint.Kind}.",
                    constraint);
            }

            ValidateForeignKeyValues(
                context,
                constraint.SheetName,
                constraint.FieldName,
                targetSheetName,
                targetFieldName);
        }
    }

    private static void ValidateConditionalRewards(
        ConfigValidationContext context,
        ConfigFieldConstraint constraint)
    {
        var levelIds = StringValues(context.Document.GetRequiredTable("Levels"), "levelId");
        foreach (var row in context.Document.GetRequiredTable("Rewards").Rows)
        {
            var rewardId = ConfigValues.String(row, "rewardId");
            if (rewardId.Length == 0)
            {
                continue;
            }

            var rewardType = ConfigValues.String(row, "rewardType");
            var isValid = rewardType switch
            {
                "UnlockLevel" => levelIds.Contains(rewardId),
                "UnlockFeature" => FeatureIdRegex().IsMatch(rewardId),
                "ScoreToken" => ScoreTokenIdRegex().IsMatch(rewardId),
                _ => false,
            };
            if (!isValid)
            {
                throw new ConfigExportException(
                    "CFG008",
                    $"Conditional reward reference '{rewardId}' is invalid for rewardType '{rewardType}'.",
                    constraint.SheetName,
                    row.ExcelRowNumber,
                    constraint.FieldName);
            }
        }
    }

    private static void ValidateExplicitGroupForeignKeys(ConfigValidationContext context)
    {
        foreach (var contract in ExplicitGroupForeignKeys)
        {
            ValidateForeignKeyValues(
                context,
                contract.SourceSheet,
                contract.SourceField,
                contract.TargetSheet,
                contract.TargetField);
        }
    }

    private static void ValidateForeignKeyValues(
        ConfigValidationContext context,
        string sourceSheetName,
        string sourceFieldName,
        string targetSheetName,
        string targetFieldName)
    {
        var targets = StringValues(context.Document.GetRequiredTable(targetSheetName), targetFieldName);
        var sourceTable = context.Document.GetRequiredTable(sourceSheetName);
        foreach (var row in sourceTable.Rows)
        {
            var value = ConfigValues.String(row, sourceFieldName);
            if (value.Length == 0)
            {
                continue;
            }

            if (string.Equals(sourceSheetName, "SpawnPoints", StringComparison.Ordinal) &&
                string.Equals(sourceFieldName, "levelId", StringComparison.Ordinal) &&
                value == "*")
            {
                continue;
            }

            if (!targets.Contains(value))
            {
                throw new ConfigExportException(
                    "CFG008",
                    $"Foreign key value '{value}' does not exist in {targetSheetName}.{targetFieldName}.",
                    sourceSheetName,
                    row.ExcelRowNumber,
                    sourceFieldName);
            }
        }
    }

    private static HashSet<string> StringValues(ConfigTable table, string fieldName)
    {
        return new HashSet<string>(
            table.Rows
                .Select(row => ConfigValues.String(row, fieldName))
                .Where(value => value.Length > 0),
            StringComparer.Ordinal);
    }

    private static ConfigExportException MetadataFailure(
        string code,
        string message,
        ConfigFieldConstraint constraint)
    {
        return new ConfigExportException(
            code,
            message,
            ConfigContract.FieldDictionarySheetName,
            constraint.DictionaryRowNumber,
            "foreignKey");
    }

    [GeneratedRegex("^feature_[a-z0-9_]+$", RegexOptions.CultureInvariant)]
    private static partial Regex FeatureIdRegex();

    [GeneratedRegex("^token_[a-z0-9_]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ScoreTokenIdRegex();

    private sealed record ExplicitForeignKey(
        string SourceSheet,
        string SourceField,
        string TargetSheet,
        string TargetField);
}
