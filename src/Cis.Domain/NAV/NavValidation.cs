namespace Cis.Domain.NAV;

public static class NavValidation
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

    public static int Positive(int value, string paramName = "value")
    {
        if (value <= 0)
        {
            throw new ArgumentException($"{paramName} must be positive.", paramName);
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

    public static void EnsureUtc(DateTime value, string paramName = "dateTime")
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"{paramName} must be in UTC.", paramName);
        }
    }
}
