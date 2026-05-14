namespace Cis.Domain.Portfolio;

public static class PortfolioValidation
{
    public static string Required(string? value, string paramName, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        }

        if (maxLength.HasValue && value.Length > maxLength.Value)
        {
            throw new ArgumentException($"{paramName} cannot exceed {maxLength.Value} characters.", paramName);
        }

        return value;
    }

    public static string? Optional(string? value, int? maxLength = null)
    {
        if (value is null)
        {
            return null;
        }

        if (maxLength.HasValue && value.Length > maxLength.Value)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength.Value} characters.");
        }

        return value;
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

    public static decimal Positive(decimal value, string paramName = "value")
    {
        if (value <= 0m)
        {
            throw new ArgumentException($"{paramName} must be positive.", paramName);
        }

        return value;
    }

    public static decimal NonNegative(decimal value, string paramName = "value")
    {
        if (value < 0m)
        {
            throw new ArgumentException($"{paramName} cannot be negative.", paramName);
        }

        return value;
    }

    public static decimal Percentage(decimal value, string paramName = "percentage")
    {
        if (value < 0m || value > 100m)
        {
            throw new ArgumentException($"{paramName} must be between 0 and 100.", paramName);
        }

        return value;
    }

    public static void EnsureUtc(DateTime dateTime, string paramName = "dateTime")
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"{paramName} must be in UTC.", paramName);
        }
    }

    public static void EnsureTenorLimit(int tenorDays, int limitDays, string paramName = "tenor")
    {
        if (tenorDays > limitDays)
        {
            throw new ArgumentException($"{paramName} of {tenorDays} days exceeds mandate limit of {limitDays} days.", paramName);
        }
    }

    public static void EnsureExposureWithinLimit(decimal exposure, decimal limitPercentage, string exposureType = "exposure")
    {
        if (exposure > limitPercentage)
        {
            throw new ArgumentException($"{exposureType} of {exposure:P2} exceeds mandate limit of {limitPercentage:P2}.", exposureType);
        }
    }
}
