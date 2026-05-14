using Cis.Domain.Common;

namespace Cis.Domain.Identity;

public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    private RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime createdAtUtc, string? createdByIpAddress)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        UserId = userId;
        TokenHash = Required(tokenHash, nameof(tokenHash), 200);
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        CreatedByIpAddress = Optional(createdByIpAddress, 100);
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public string? CreatedByIpAddress { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public string? RevokedByIpAddress { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public User? User { get; private set; }

    public bool IsActive(DateTime utcNow)
    {
        return RevokedAtUtc is null && ExpiresAtUtc > utcNow;
    }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime createdAtUtc, string? createdByIpAddress)
    {
        return new RefreshToken(userId, tokenHash, expiresAtUtc, createdAtUtc, createdByIpAddress);
    }

    public void Revoke(DateTime revokedAtUtc, string? revokedByIpAddress, string? replacedByTokenHash = null)
    {
        EnsureUtc(revokedAtUtc, nameof(revokedAtUtc));
        if (RevokedAtUtc.HasValue)
        {
            return;
        }

        RevokedAtUtc = revokedAtUtc;
        RevokedByIpAddress = Optional(revokedByIpAddress, 100);
        ReplacedByTokenHash = Optional(replacedByTokenHash, 200);
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

    private static string? Optional(string? value, int maxLength)
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

    private static void EnsureUtc(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
