namespace Cis.Domain.Accounting;

public static class AccountingValidation
{
    public static string Required(string? value, string paramName, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        }

        var trimmed = value.Trim();
        if (maxLength.HasValue && trimmed.Length > maxLength.Value)
        {
            throw new ArgumentException($"{paramName} cannot exceed {maxLength.Value} characters.", paramName);
        }

        return trimmed;
    }

    public static string? Optional(string? value, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (maxLength.HasValue && trimmed.Length > maxLength.Value)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength.Value} characters.");
        }

        return trimmed;
    }

    public static string Currency(string? value, string paramName = "currency")
    {
        var currency = Required(value, paramName, 3);
        if (currency.Length != 3 || !currency.All(char.IsLetter))
        {
            throw new ArgumentException($"{paramName} must be a valid 3-letter ISO currency code.", paramName);
        }

        return currency.ToUpperInvariant();
    }

    public static decimal NonNegative(decimal value, string paramName = "value")
    {
        if (value < 0m)
        {
            throw new ArgumentException($"{paramName} cannot be negative.", paramName);
        }

        return value;
    }

    public static decimal Positive(decimal value, string paramName = "value")
    {
        if (value <= 0m)
        {
            throw new ArgumentException($"{paramName} must be positive.", paramName);
        }

        return value;
    }

    public static void EnsureUtc(DateTime value, string paramName = "dateTime")
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"{paramName} must be in UTC.", paramName);
        }
    }
}
