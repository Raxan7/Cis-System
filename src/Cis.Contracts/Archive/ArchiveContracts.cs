using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Archive;

public sealed record ArchiveRecordDto(
    Guid Id,
    string Module,
    string EntityType,
    string EntityId,
    string PayloadJson,
    string PayloadHash,
    string ArchivedByUserId,
    DateTime ArchivedAtUtc,
    Guid? WorkflowId,
    Guid? RetentionPolicyId,
    string Reason);

public sealed record CreateArchiveRecordRequest(
    [Required, StringLength(100)] string Module,
    [Required, StringLength(200)] string EntityType,
    [Required, StringLength(100)] string EntityId,
    [Required] string PayloadJson,
    Guid? WorkflowId,
    Guid? RetentionPolicyId,
    [Required, StringLength(1000)] string Reason);

public sealed record RetentionPolicyDto(
    Guid Id,
    string Name,
    string Module,
    int RetentionDays,
    bool LegalHoldEnabled,
    bool IsActive);
