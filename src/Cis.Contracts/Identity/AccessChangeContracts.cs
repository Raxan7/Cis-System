using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Identity;

public sealed record CreateAccessChangeRequest(
    [Required] Guid TargetUserId,
    [Required] string ChangeType,
    IReadOnlyCollection<string>? RoleNames,
    [Required, MinLength(10), MaxLength(1000)] string Reason);

public sealed record AccessChangeDecisionRequest(
    [Required, MinLength(10), MaxLength(1000)] string Reason);

public sealed record AccessChangeRequestDto(
    Guid Id,
    string RequestNumber,
    Guid TargetUserId,
    string TargetUserEmail,
    string ChangeType,
    string Status,
    IReadOnlyCollection<string> RoleNames,
    string RequestedByUserId,
    DateTime RequestedAtUtc,
    string Reason,
    string? DecisionByUserId,
    DateTime? DecisionAtUtc,
    string? DecisionReason);
