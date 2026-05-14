namespace Cis.Domain.Investors;

internal static class InvestorValidation
{
    public static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters.", parameterName);
        }

        return trimmed;
    }

    public static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.");
        }

        return trimmed;
    }

    public static string Currency(string value, string parameterName)
    {
        var currency = Required(value, parameterName, 3).ToUpperInvariant();
        if (currency.Length != 3 || currency.Any(character => character < 'A' || character > 'Z'))
        {
            throw new ArgumentException("Currency must be a three-letter ISO 4217 code.", parameterName);
        }

        return currency;
    }

    public static decimal Percentage(decimal value, string parameterName)
    {
        if (value < 0m || value > 100m)
        {
            throw new ArgumentException($"{parameterName} must be between 0 and 100.", parameterName);
        }

        return value;
    }

    public static long NonNegative(long value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentException($"{parameterName} cannot be negative.", parameterName);
        }

        return value;
    }

    public static void EnsureUtc(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
