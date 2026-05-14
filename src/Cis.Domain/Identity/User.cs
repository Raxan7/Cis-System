using System.Net.Mail;
using Cis.Domain.Common;

namespace Cis.Domain.Identity;

public sealed class User : AuditableAggregateRoot
{
    private readonly List<UserRole> _roles = [];
    private readonly List<RefreshToken> _refreshTokens = [];
    private readonly List<UserMfaFactor> _mfaFactors = [];

    private User()
    {
    }

    private User(string email, string displayName, string passwordHash, DateTime createdAtUtc)
    {
        SetEmail(email);
        SetDisplayName(displayName);
        PasswordHash = Required(passwordHash, nameof(passwordHash), 1000);
        Status = UserStatus.Active;
        PasswordResetRequired = true;
        MarkCreated("system", createdAtUtc);
    }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserStatus Status { get; private set; }

    public bool MfaEnabled { get; private set; }

    public bool PasswordResetRequired { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public DateTime? LockoutEndUtc { get; private set; }

    public DateTime? DeactivatedAtUtc { get; private set; }

    public string? DeactivatedReason { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public IReadOnlyCollection<UserMfaFactor> MfaFactors => _mfaFactors.AsReadOnly();

    public bool CanSignIn(DateTime utcNow)
    {
        return Status == UserStatus.Active && (!LockoutEndUtc.HasValue || LockoutEndUtc <= utcNow);
    }

    public static User Create(string email, string displayName, string passwordHash, DateTime createdAtUtc)
    {
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        return new User(email, displayName, passwordHash, createdAtUtc);
    }

    public void RecordSuccessfulLogin(DateTime occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        FailedLoginCount = 0;
        LockoutEndUtc = null;
        LastLoginAtUtc = occurredAtUtc;
    }

    public void RecordFailedLogin(DateTime occurredAtUtc, int lockoutThreshold, TimeSpan lockoutDuration)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        FailedLoginCount += 1;
        if (FailedLoginCount >= lockoutThreshold)
        {
            LockoutEndUtc = occurredAtUtc.Add(lockoutDuration);
            Status = UserStatus.Locked;
        }
    }

    public void UnlockIfLockoutElapsed(DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));
        if (Status == UserStatus.Locked && LockoutEndUtc.HasValue && LockoutEndUtc <= utcNow)
        {
            Status = UserStatus.Active;
            FailedLoginCount = 0;
            LockoutEndUtc = null;
        }
    }

    public void SetPasswordHash(string passwordHash, bool requirePasswordChange)
    {
        PasswordHash = Required(passwordHash, nameof(passwordHash), 1000);
        PasswordResetRequired = requirePasswordChange;
    }

    public void SetMfa(bool enabled, DateTime occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        MfaEnabled = enabled;
    }

    public void AddMfaFactor(string factorType, string secretHash, DateTime createdAtUtc)
    {
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        _mfaFactors.Add(UserMfaFactor.Create(Id, factorType, secretHash, createdAtUtc));
    }

    public void AddRefreshToken(RefreshToken refreshToken)
    {
        if (refreshToken.UserId != Id)
        {
            throw new InvalidOperationException("Refresh token belongs to a different user.");
        }

        _refreshTokens.Add(refreshToken);
    }

    public void AssignRole(Guid roleId, string requestedByUserId, string approvedByUserId, DateTime grantedAtUtc)
    {
        EnsureUtc(grantedAtUtc, nameof(grantedAtUtc));
        if (_roles.Any(userRole => userRole.RoleId == roleId))
        {
            return;
        }

        _roles.Add(UserRole.Create(Id, roleId, requestedByUserId, approvedByUserId, grantedAtUtc));
    }

    public void Deactivate(string reason, DateTime deactivatedAtUtc)
    {
        EnsureUtc(deactivatedAtUtc, nameof(deactivatedAtUtc));
        if (Status == UserStatus.Deactivated)
        {
            return;
        }

        Status = UserStatus.Deactivated;
        DeactivatedAtUtc = deactivatedAtUtc;
        DeactivatedReason = Required(reason, nameof(reason), 1000);
    }

    private void SetEmail(string email)
    {
        var trimmed = Required(email, nameof(email), 320);
        try
        {
            _ = new MailAddress(trimmed);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("Email address is invalid.", nameof(email), exception);
        }

        Email = trimmed;
        NormalizedEmail = trimmed.ToUpperInvariant();
    }

    private void SetDisplayName(string displayName)
    {
        DisplayName = Required(displayName, nameof(displayName), 200);
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
