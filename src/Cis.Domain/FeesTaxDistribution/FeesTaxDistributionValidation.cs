namespace Cis.Domain.FeesTaxDistribution;

internal static class FeesTaxDistributionValidation
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

    public static decimal NonNegative(decimal value, string parameterName)
    {
        if (value < 0m)
        {
            throw new ArgumentException($"{parameterName} cannot be negative.", parameterName);
        }

        return value;
    }

    public static decimal Positive(decimal value, string parameterName)
    {
        if (value <= 0m)
        {
            throw new ArgumentException($"{parameterName} must be greater than zero.", parameterName);
        }

        return value;
    }

    public static decimal Percentage(decimal value, string parameterName)
    {
        if (value < 0m || value > 100m)
        {
            throw new ArgumentException($"{parameterName} must be between 0 and 100.", parameterName);
        }

        return value;
    }

    public static int Positive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"{parameterName} must be greater than zero.", parameterName);
        }

        return value;
    }

    public static DateTime EnsureUtc(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }

        return timestamp;
    }
}
