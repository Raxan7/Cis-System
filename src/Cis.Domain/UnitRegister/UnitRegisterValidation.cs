namespace Cis.Domain.UnitRegister;

internal static class UnitRegisterValidation
{
    public static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    public static DateTime EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return value.ToUniversalTime();
    }

    public static int Precision(int value, string parameterName)
    {
        if (value is < 4 or > 6)
        {
            throw new ArgumentException("Unit precision must be between 4 and 6 decimal places.", parameterName);
        }

        return value;
    }

    public static decimal Units(decimal value, int precision, string parameterName, bool allowNegative = true)
    {
        if (value == 0m)
        {
            throw new ArgumentException($"{parameterName} cannot be zero.", parameterName);
        }

        if (!allowNegative && value < 0m)
        {
            throw new ArgumentException($"{parameterName} must be positive.", parameterName);
        }

        var rounded = Math.Round(value, precision, MidpointRounding.AwayFromZero);
        if (rounded == 0m)
        {
            throw new ArgumentException($"{parameterName} rounds to zero at the configured precision.", parameterName);
        }

        return rounded;
    }
}
