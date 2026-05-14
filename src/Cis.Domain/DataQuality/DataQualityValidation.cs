namespace Cis.Domain.DataQuality;

internal static class DataQualityValidation
{
    public static string Required(string value, string fieldName, int maxLength = 1000)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{fieldName} cannot exceed {maxLength} characters.", fieldName);
        }

        return trimmed;
    }

    public static string? Optional(string? value, int maxLength = 1000)
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

    public static DateTime EnsureUtc(DateTime timestamp, string fieldName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"{fieldName} must be UTC.", fieldName);
        }

        return timestamp;
    }
}
