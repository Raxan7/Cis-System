using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Portal;

public sealed record PortalProfileDto(
    Guid UserId,
    Guid InvestorId,
    string InvestorNumber,
    string DisplayName,
    string Email,
    string PhoneNumber,
    string InvestorStatus,
    bool MfaRequired,
    bool MfaSatisfied);

public sealed record PortalHoldingDto(
    Guid SchemeId,
    Guid SchemeClassId,
    decimal Units,
    decimal LienedUnits,
    decimal RedeemableUnits,
    decimal? UnitPrice,
    decimal? MarketValue,
    decimal? RedeemableAmount,
    int UnitPrecision,
    DateOnly? LatestValuationDate,
    DateOnly? LastMovementDate,
    string? LastTransactionReference);

public sealed record PortalTransactionDto(
    Guid Id,
    string Source,
    string Type,
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly BusinessDate,
    decimal? Amount,
    decimal? Units,
    string? Status,
    string Reference);

public sealed record PortalStatementDto(
    Guid Id,
    Guid InvestorId,
    DateOnly StatementDate,
    string StatementReference,
    decimal TotalUnits,
    int HoldingCount);

public sealed record PortalTaxCertificateDto(
    Guid Id,
    Guid InvestorId,
    string TaxNumber,
    string CountryOfTaxResidence,
    DateOnly CertificateDate,
    string CertificateReference);

public sealed record InvestorNoticeDto(
    Guid Id,
    string Title,
    string Body,
    DateOnly PublishedDate,
    DateTime PublishedAtUtc);

public sealed record CreatePortalSubscriptionRequest(
    Guid SchemeId,
    Guid SchemeClassId,
    decimal Amount,
    [Required, StringLength(3, MinimumLength = 3)] string Currency);

public sealed record CreatePortalRedemptionRequest(
    Guid SchemeId,
    Guid SchemeClassId,
    decimal? Amount,
    decimal? Units,
    bool FullRedemption,
    [Required, StringLength(3, MinimumLength = 3)] string Currency);

public sealed record CreatePortalSwitchRequest(
    Guid SourceSchemeId,
    Guid SourceSchemeClassId,
    Guid TargetSchemeId,
    Guid TargetSchemeClassId,
    decimal? Amount,
    decimal? Units,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal FeeAmount);

public sealed record CreatePortalProfileUpdateRequest(
    [Required, StringLength(200)] string DisplayName,
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(50)] string PhoneNumber,
    [Required, StringLength(1000)] string Reason);

public sealed record UploadPortalDocumentRequest(
    [Required, StringLength(100)] string DocumentType,
    [Required, StringLength(200)] string FileName,
    [Required, StringLength(100)] string ContentType,
    long SizeBytes,
    [Required, StringLength(500)] string StorageReference);

public sealed record DigitalServiceRequestDto(
    Guid Id,
    Guid InvestorId,
    Guid UserId,
    string RequestType,
    string Status,
    Guid? WorkflowId,
    string RequestPayloadJson,
    DateTime SubmittedAtUtc);

public sealed record PortalActivityLogDto(
    Guid Id,
    string ActivityType,
    string Summary,
    string? EntityType,
    string? EntityId,
    DateTime OccurredAtUtc);
