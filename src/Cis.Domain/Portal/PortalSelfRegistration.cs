using Cis.Domain.Common;

namespace Cis.Domain.Portal;

public sealed class PortalSelfRegistration : AuditableAggregateRoot
{
    private PortalSelfRegistration()
    {
    }

    private PortalSelfRegistration(
        string displayName,
        string email,
        string normalizedEmail,
        string phoneNumber,
        string normalizedPhoneNumber,
        string passwordHash,
        string otpCodeHash,
        DateTime otpSentAtUtc,
        DateTime otpExpiresAtUtc,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        DisplayName = PortalValidation.Required(displayName, nameof(displayName), 200);
        Email = PortalValidation.Required(email, nameof(email), 320).ToLowerInvariant();
        NormalizedEmail = PortalValidation.Required(normalizedEmail, nameof(normalizedEmail), 320).ToUpperInvariant();
        PhoneNumber = PortalValidation.Required(phoneNumber, nameof(phoneNumber), 50);
        NormalizedPhoneNumber = PortalValidation.Required(normalizedPhoneNumber, nameof(normalizedPhoneNumber), 50);
        PasswordHash = PortalValidation.Required(passwordHash, nameof(passwordHash), 1000);
        OtpCodeHash = PortalValidation.Required(otpCodeHash, nameof(otpCodeHash), 128);
        OtpSentAtUtc = PortalValidation.EnsureUtc(otpSentAtUtc, nameof(otpSentAtUtc));
        OtpExpiresAtUtc = PortalValidation.EnsureUtc(otpExpiresAtUtc, nameof(otpExpiresAtUtc));
        Status = PortalSelfRegistrationStatus.PendingVerification;
        MarkCreated(createdByUserId, PortalValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc)));
    }

    public string DisplayName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string PhoneNumber { get; private set; } = string.Empty;

    public string NormalizedPhoneNumber { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string OtpCodeHash { get; private set; } = string.Empty;

    public DateTime OtpSentAtUtc { get; private set; }

    public DateTime OtpExpiresAtUtc { get; private set; }

    public int FailedOtpAttemptCount { get; private set; }

    public PortalSelfRegistrationStatus Status { get; private set; }

    public DateTime? ActivatedAtUtc { get; private set; }

    public Guid? UserId { get; private set; }

    public Guid? InvestorId { get; private set; }

    public static PortalSelfRegistration Create(
        string displayName,
        string email,
        string normalizedEmail,
        string phoneNumber,
        string normalizedPhoneNumber,
        string passwordHash,
        string otpCodeHash,
        DateTime otpSentAtUtc,
        DateTime otpExpiresAtUtc,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new PortalSelfRegistration(
            displayName,
            email,
            normalizedEmail,
            phoneNumber,
            normalizedPhoneNumber,
            passwordHash,
            otpCodeHash,
            otpSentAtUtc,
            otpExpiresAtUtc,
            createdByUserId,
            createdAtUtc);
    }

    public void RefreshPendingVerification(
        string displayName,
        string email,
        string normalizedEmail,
        string phoneNumber,
        string normalizedPhoneNumber,
        string passwordHash,
        string otpCodeHash,
        DateTime otpSentAtUtc,
        DateTime otpExpiresAtUtc)
    {
        if (Status == PortalSelfRegistrationStatus.Activated)
        {
            throw new InvalidOperationException("Activated self-registration records cannot be refreshed.");
        }

        DisplayName = PortalValidation.Required(displayName, nameof(displayName), 200);
        Email = PortalValidation.Required(email, nameof(email), 320).ToLowerInvariant();
        NormalizedEmail = PortalValidation.Required(normalizedEmail, nameof(normalizedEmail), 320).ToUpperInvariant();
        PhoneNumber = PortalValidation.Required(phoneNumber, nameof(phoneNumber), 50);
        NormalizedPhoneNumber = PortalValidation.Required(normalizedPhoneNumber, nameof(normalizedPhoneNumber), 50);
        PasswordHash = PortalValidation.Required(passwordHash, nameof(passwordHash), 1000);
        OtpCodeHash = PortalValidation.Required(otpCodeHash, nameof(otpCodeHash), 128);
        OtpSentAtUtc = PortalValidation.EnsureUtc(otpSentAtUtc, nameof(otpSentAtUtc));
        OtpExpiresAtUtc = PortalValidation.EnsureUtc(otpExpiresAtUtc, nameof(otpExpiresAtUtc));
        FailedOtpAttemptCount = 0;
        Status = PortalSelfRegistrationStatus.PendingVerification;
        ActivatedAtUtc = null;
        UserId = null;
        InvestorId = null;
    }

    public void VerifyOtp(string otpCodeHash, DateTime verifiedAtUtc, int maxFailedAttempts)
    {
        var occurredAtUtc = PortalValidation.EnsureUtc(verifiedAtUtc, nameof(verifiedAtUtc));
        if (Status == PortalSelfRegistrationStatus.Activated)
        {
            return;
        }

        if (Status == PortalSelfRegistrationStatus.Expired || occurredAtUtc > OtpExpiresAtUtc)
        {
            Status = PortalSelfRegistrationStatus.Expired;
            throw new InvalidOperationException("The OTP has expired. Request a new code.");
        }

        if (!string.Equals(OtpCodeHash, PortalValidation.Required(otpCodeHash, nameof(otpCodeHash), 128), StringComparison.Ordinal))
        {
            FailedOtpAttemptCount++;
            if (FailedOtpAttemptCount >= maxFailedAttempts)
            {
                Status = PortalSelfRegistrationStatus.Expired;
            }

            throw new InvalidOperationException("The OTP is invalid.");
        }
    }

    public void Activate(Guid userId, Guid investorId, DateTime activatedAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (investorId == Guid.Empty)
        {
            throw new ArgumentException("Investor id cannot be empty.", nameof(investorId));
        }

        UserId = userId;
        InvestorId = investorId;
        ActivatedAtUtc = PortalValidation.EnsureUtc(activatedAtUtc, nameof(activatedAtUtc));
        Status = PortalSelfRegistrationStatus.Activated;
    }
}
