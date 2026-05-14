namespace Cis.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "Victory.CIS";

    public string Audience { get; init; } = "Victory.CIS.Api";

    public string SigningKey { get; init; } = "development-signing-key-change-before-production-32bytes";

    public int AccessTokenMinutes { get; init; } = 30;

    public int RefreshTokenDays { get; init; } = 7;
}
