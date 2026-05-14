using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Reports;

public sealed record ReportOwnerMatrixDto(Guid Id, string OwnerRole, string Responsibility);

public sealed record ReportDefinitionDto(
    Guid Id,
    string Code,
    string Name,
    string Category,
    string Frequency,
    string PrimaryUsers,
    string RequiredPermission,
    bool IsActive,
    IReadOnlyCollection<ReportOwnerMatrixDto> Owners);

public sealed record CreateReportScheduleRequest(
    [Required, StringLength(50)] string ReportCode,
    [Required] string Frequency,
    [Required, StringLength(100)] string CronExpression,
    DateTime NextRunAtUtc);

public sealed record ReportScheduleDto(
    Guid Id,
    Guid ReportDefinitionId,
    string ReportCode,
    string Frequency,
    string CronExpression,
    DateTime NextRunAtUtc,
    string Status);

public sealed record ReportParameterRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(1000)] string Value);

public sealed record ReportOutputRequest(
    [Required] string Format);

public sealed record RunReportRequest(
    DateOnly BusinessDate,
    IReadOnlyCollection<ReportParameterRequest> Parameters,
    IReadOnlyCollection<ReportOutputRequest> Outputs);

public sealed record ReportActionRequest(
    [StringLength(1000)] string? Comment);

public sealed record ReportParameterDto(Guid Id, string Name, string Value);

public sealed record ReportOutputDto(Guid Id, string Format, string StorageReference, string ContentHash, long SizeBytes, DateTime CreatedAtUtc);

public sealed record ReportApprovalDto(Guid Id, string Decision, string DecidedByUserId, DateTime DecidedAtUtc, string? Comment);

public sealed record ReportDistributionDto(Guid Id, string Channel, string Recipient, string DistributedByUserId, DateTime DistributedAtUtc, string? Comment);

public sealed record ReportRunDto(
    Guid Id,
    Guid ReportDefinitionId,
    string ReportCode,
    DateOnly BusinessDate,
    DateTime SourceDataTimestampUtc,
    string Status,
    int VersionNumber,
    string GeneratedByUserId,
    DateTime GeneratedAtUtc,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? PublishedByUserId,
    DateTime? PublishedAtUtc,
    IReadOnlyCollection<ReportParameterDto> Parameters,
    IReadOnlyCollection<ReportOutputDto> Outputs,
    IReadOnlyCollection<ReportApprovalDto> Approvals,
    IReadOnlyCollection<ReportDistributionDto> Distributions);

public sealed record ReportDownloadDto(
    Guid ReportRunId,
    string ReportCode,
    string Status,
    string Format,
    string StorageReference,
    string ContentHash,
    long SizeBytes);

public sealed record ReportVersionArchiveDto(Guid Id, Guid ReportRunId, int VersionNumber, string PayloadHash, DateTime ArchivedAtUtc);

public sealed record CreateReportBundleRequest(
    [Required, StringLength(50)] string BundleCode,
    [Required, StringLength(200)] string Name,
    DateOnly BusinessDate,
    IReadOnlyCollection<Guid> ReportRunIds);

public sealed record ReportBundleDto(
    Guid Id,
    string BundleCode,
    string Name,
    DateOnly BusinessDate,
    IReadOnlyCollection<Guid> ReportRunIds,
    string Status,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    string? PublishedByUserId,
    DateTime? PublishedAtUtc);
