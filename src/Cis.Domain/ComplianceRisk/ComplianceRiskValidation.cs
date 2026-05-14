namespace Cis.Domain.ComplianceRisk;

internal static class ComplianceRiskValidation
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

    public static int NonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentException($"{parameterName} cannot be negative.", parameterName);
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
