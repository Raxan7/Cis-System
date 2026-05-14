namespace Cis.Domain.Integrations;

internal static class IntegrationValidation
{
    public static string Required(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        value = value.Trim();
        return value.Length <= maxLength ? value : throw new ArgumentException($"{fieldName} cannot exceed {maxLength} characters.", fieldName);
    }

    public static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        return value.Length <= maxLength ? value : throw new ArgumentException($"Value cannot exceed {maxLength} characters.");
    }

    public static DateTime Utc(DateTime value, string fieldName)
    {
        return value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException($"{fieldName} must be UTC.", fieldName);
    }
}
