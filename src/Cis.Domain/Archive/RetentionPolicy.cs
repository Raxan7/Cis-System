using Cis.Domain.Common;

namespace Cis.Domain.Archive;

public sealed class RetentionPolicy : AuditableAggregateRoot
{
    private RetentionPolicy()
    {
    }

    private RetentionPolicy(string name, string module, int retentionDays, bool legalHoldEnabled, DateTime createdAtUtc)
    {
        Name = Required(name, nameof(name), 200);
        Module = Required(module, nameof(module), 100);
        RetentionDays = retentionDays <= 0 ? throw new ArgumentException("Retention days must be positive.", nameof(retentionDays)) : retentionDays;
        LegalHoldEnabled = legalHoldEnabled;
        IsActive = true;
        MarkCreated("system", createdAtUtc);
    }

    public string Name { get; private set; } = string.Empty;

    public string Module { get; private set; } = string.Empty;

    public int RetentionDays { get; private set; }

    public bool LegalHoldEnabled { get; private set; }

    public bool IsActive { get; private set; }

    public static RetentionPolicy Create(string name, string module, int retentionDays, bool legalHoldEnabled, DateTime createdAtUtc)
    {
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        return new RetentionPolicy(name, module, retentionDays, legalHoldEnabled, createdAtUtc);
    }

    private static string Required(string value, string parameterName, int maxLength)
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

    private static void EnsureUtc(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
