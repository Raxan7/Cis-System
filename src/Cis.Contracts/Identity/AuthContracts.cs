using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Identity;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    string? MfaCode = null);

public sealed record RefreshTokenRequest(
    [Required] string RefreshToken);

public sealed record AuthTokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    UserSummaryDto User);

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Status,
    bool MfaEnabled,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
