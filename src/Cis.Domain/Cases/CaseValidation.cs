namespace Cis.Domain.Cases;

internal static class CaseValidation
{
    public static string Required(string value, string fieldName, int maxLength)
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

    public static void EnsureUtc(DateTime timestamp, string fieldName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"{fieldName} must be UTC.", fieldName);
        }
    }
}
