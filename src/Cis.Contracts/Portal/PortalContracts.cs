using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Portal;

public sealed record CreatePortalSelfRegistrationRequest(
    [Required, StringLength(200, MinimumLength = 3)] string DisplayName,
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(50, MinimumLength = 5)] string PhoneNumber,
    [Required, StringLength(200, MinimumLength = 12)] string Password);

public sealed record VerifyPortalSelfRegistrationOtpRequest(
    Guid RegistrationId,
    [Required, StringLength(10, MinimumLength = 4)] string OtpCode);

public sealed record PortalSelfRegistrationInitiatedDto(
    Guid RegistrationId,
    string DisplayName,
    string Email,
    string MaskedPhoneNumber,
    string Status,
    DateTime OtpSentAtUtc,
    DateTime OtpExpiresAtUtc);

public sealed record PortalSelfRegistrationActivationDto(
    Guid RegistrationId,
    Guid UserId,
    Guid InvestorId,
    string InvestorNumber,
    string InvestorStatus,
    string PortalProfileStatus,
    string Email);

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

public sealed record PortalFundNavDto(
    Guid SchemeId,
    string SchemeCode,
    string SchemeName,
    Guid SchemeClassId,
    string SchemeClassCode,
    string SchemeClassName,
    string Currency,
    string SchemeStatus,
    decimal PublishedNav,
    decimal PublishedUnitPrice,
    DateOnly ValuationDate,
    DateTime PublishedAtUtc,
    string ValuationFrequency,
    string DealingFrequency);

public sealed record PortalHoldingDto(
    Guid SchemeId,
    string SchemeCode,
    string SchemeName,
    Guid SchemeClassId,
    string SchemeClassCode,
    string SchemeClassName,
    string Currency,
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

public sealed record PortalPortfolioPositionDto(
    Guid SchemeId,
    string SchemeCode,
    string SchemeName,
    Guid SchemeClassId,
    string SchemeClassCode,
    string SchemeClassName,
    string Currency,
    decimal Units,
    decimal LienedUnits,
    decimal RedeemableUnits,
    decimal? UnitPrice,
    decimal? MarketValue,
    decimal? RedeemableAmount,
    int UnitPrecision,
    DateOnly? LatestValuationDate);

public sealed record PortalPortfolioSummaryDto(
    Guid InvestorId,
    string InvestorNumber,
    string InvestorStatus,
    decimal TotalUnits,
    decimal TotalLienedUnits,
    decimal TotalRedeemableUnits,
    decimal TotalMarketValue,
    decimal TotalRedeemableAmount,
    decimal TotalNetContribution,
    decimal EstimatedCapitalGain,
    DateOnly? LatestValuationDate,
    IReadOnlyCollection<PortalPortfolioPositionDto> Positions);

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

public sealed record PortalInvestorBankAccountDto(
    Guid Id,
    string BankName,
    string AccountNumber,
    string AccountName,
    string Currency,
    string? SwiftCode,
    bool IsActive,
    bool HighRiskFlag);

public sealed record PortalKycRequirementStatusDto(
    Guid Id,
    string DocumentType,
    bool IsMandatory,
    string Status,
    Guid? SatisfiedByDocumentId);

public sealed record PortalKycDocumentStatusDto(
    Guid Id,
    string DocumentType,
    string FileName,
    DateOnly? ExpiryDate,
    string Status,
    DateTime UploadedAtUtc);

public sealed record PortalKycProfileDto(
    Guid InvestorId,
    string InvestorNumber,
    string InvestorStatus,
    string InvestorType,
    string DisplayName,
    string Email,
    string PhoneNumber,
    string? IdentityNumber,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? Nationality,
    string? TaxNumber,
    string? CountryOfTaxResidence,
    string? AddressLine1,
    string? AlternatePhoneNumber,
    string? NextOfKinName,
    string? NextOfKinPhoneNumber,
    string? NextOfKinRelationship,
    bool CanRedeem,
    string? RedemptionBlockedReason,
    IReadOnlyCollection<PortalInvestorBankAccountDto> BankAccounts,
    IReadOnlyCollection<PortalKycRequirementStatusDto> KycRequirements,
    IReadOnlyCollection<PortalKycDocumentStatusDto> KycDocuments);

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

public sealed record CreatePortalTransferRequest(
    Guid SchemeId,
    Guid SchemeClassId,
    [Required, StringLength(50)] string TargetInvestorNumber,
    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")] decimal Units,
    [Required, StringLength(1000)] string Reason);

public sealed record CreatePortalProfileUpdateRequest(
    [Required, StringLength(200)] string DisplayName,
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(50)] string PhoneNumber,
    [StringLength(100)] string? IdentityNumber,
    [StringLength(100)] string? FirstName,
    [StringLength(100)] string? LastName,
    DateOnly? DateOfBirth,
    [StringLength(100)] string? Nationality,
    [StringLength(100)] string? TaxNumber,
    [StringLength(100)] string? CountryOfTaxResidence,
    [StringLength(500)] string? AddressLine1,
    [StringLength(50)] string? AlternatePhoneNumber,
    [StringLength(200)] string? NextOfKinName,
    [StringLength(50)] string? NextOfKinPhoneNumber,
    [StringLength(100)] string? NextOfKinRelationship,
    [StringLength(200)] string? BankName,
    [StringLength(100)] string? BankAccountNumber,
    [StringLength(200)] string? BankAccountName,
    [StringLength(3, MinimumLength = 3)] string? BankCurrency,
    [StringLength(20)] string? BankSwiftCode,
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
