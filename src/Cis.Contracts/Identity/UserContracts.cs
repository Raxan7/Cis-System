using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Identity;

public sealed record CreateUserRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(2), MaxLength(200)] string DisplayName,
    [Required] string TemporaryPassword);

public sealed record AssignRolesRequest(
    [Required, MinLength(1)] IReadOnlyCollection<string> RoleNames,
    [Required, MinLength(10), MaxLength(1000)] string Reason);

public sealed record DeactivateUserRequest(
    [Required, MinLength(10), MaxLength(1000)] string Reason);

public sealed record PasswordResetRequest(
    [Required] string TemporaryPassword,
    bool RequirePasswordChange = true);

public sealed record MfaChangeRequest(
    bool Enabled,
    [MaxLength(50)] string? FactorType = null,
    [MaxLength(500)] string? Secret = null);

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Status,
    bool MfaEnabled,
    bool PasswordResetRequired,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<string> Roles);
