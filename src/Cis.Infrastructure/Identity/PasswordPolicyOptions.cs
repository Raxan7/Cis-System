namespace Cis.Infrastructure.Identity;

public sealed class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    public int RequiredLength { get; init; } = 12;

    public bool RequireDigit { get; init; } = true;

    public bool RequireUppercase { get; init; } = true;

    public bool RequireLowercase { get; init; } = true;

    public bool RequireNonAlphanumeric { get; init; } = true;

    public int LockoutThreshold { get; init; } = 5;

    public int LockoutMinutes { get; init; } = 15;
}
