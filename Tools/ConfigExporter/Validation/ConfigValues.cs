using System.Globalization;
using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Validation;

internal static class ConfigValues
{
    public static bool IsEmpty(object? value)
    {
        return value is null || value is string { Length: 0 };
    }

    public static string String(ConfigRow row, string fieldName)
    {
        return row.GetValue(fieldName) as string
            ?? throw new InvalidOperationException(
                $"Expected {fieldName} on Excel row {row.ExcelRowNumber} to be a string.");
    }

    public static long Integer(ConfigRow row, string fieldName)
    {
        return row.GetValue(fieldName) is long value
            ? value
            : throw new InvalidOperationException(
                $"Expected {fieldName} on Excel row {row.ExcelRowNumber} to be an integer.");
    }

    public static decimal Number(ConfigRow row, string fieldName)
    {
        return row.GetValue(fieldName) switch
        {
            long integer => integer,
            decimal number => number,
            _ => throw new InvalidOperationException(
                $"Expected {fieldName} on Excel row {row.ExcelRowNumber} to be numeric."),
        };
    }

    public static bool TryNumber(object? value, out decimal number)
    {
        switch (value)
        {
            case long integer:
                number = integer;
                return true;
            case decimal decimalValue:
                number = decimalValue;
                return true;
            default:
                number = default;
                return false;
        }
    }

    public static string KeyPart(object? value)
    {
        return value switch
        {
            null => "null:",
            string text => $"string:{text}",
            long integer => $"int:{integer.ToString(CultureInfo.InvariantCulture)}",
            decimal number => $"float:{number.ToString("G29", CultureInfo.InvariantCulture)}",
            bool boolean => boolean ? "bool:true" : "bool:false",
            _ => throw new InvalidOperationException($"Unsupported config value type {value.GetType().FullName}."),
        };
    }
}
