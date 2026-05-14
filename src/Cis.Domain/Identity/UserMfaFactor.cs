using Cis.Domain.Common;

namespace Cis.Domain.Identity;

public sealed class UserMfaFactor : Entity
{
    private UserMfaFactor()
    {
    }

    private UserMfaFactor(Guid userId, string factorType, string secretHash, DateTime createdAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Created timestamp must be UTC.", nameof(createdAtUtc));
        }

        UserId = userId;
        FactorType = Required(factorType, nameof(factorType), 50);
        SecretHash = Required(secretHash, nameof(secretHash), 500);
        Status = MfaFactorStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }

    public string FactorType { get; private set; } = string.Empty;

    public string SecretHash { get; private set; } = string.Empty;

    public MfaFactorStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? EnabledAtUtc { get; private set; }

    public DateTime? DisabledAtUtc { get; private set; }

    public User? User { get; private set; }

    public static UserMfaFactor Create(Guid userId, string factorType, string secretHash, DateTime createdAtUtc)
    {
        return new UserMfaFactor(userId, factorType, secretHash, createdAtUtc);
    }

    public void MarkEnabled(DateTime enabledAtUtc)
    {
        if (enabledAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Enabled timestamp must be UTC.", nameof(enabledAtUtc));
        }

        Status = MfaFactorStatus.Enabled;
        EnabledAtUtc = enabledAtUtc;
    }

    public void MarkDisabled(DateTime disabledAtUtc)
    {
        if (disabledAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Disabled timestamp must be UTC.", nameof(disabledAtUtc));
        }

        Status = MfaFactorStatus.Disabled;
        DisabledAtUtc = disabledAtUtc;
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
}
