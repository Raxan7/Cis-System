namespace Cis.Infrastructure.Portal;

public sealed class PortalRegistrationOptions
{
    public const string SectionName = "Portal:SelfRegistration";

    public int OtpLength { get; init; } = 6;

    public int OtpExpiryMinutes { get; init; } = 10;

    public int MaxFailedOtpAttempts { get; init; } = 5;

    public string? FixedOtpCode { get; init; }
}
