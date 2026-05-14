using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Investors;

public sealed record CreateInvestorRequest(
    [Required] string InvestorType,
    [Required, StringLength(200)] string DisplayName,
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(50)] string PhoneNumber,
    [Required] string RiskCategory,
    [Required, StringLength(1000)] string RiskReason,
    [StringLength(100)] string? IdentityNumber,
    [StringLength(100)] string? FirstName,
    [StringLength(100)] string? LastName,
    DateOnly? DateOfBirth,
    [StringLength(100)] string? Nationality,
    [StringLength(200)] string? RegisteredName,
    [StringLength(100)] string? CorporateRegistrationNumber,
    DateOnly? IncorporationDate,
    [StringLength(200)] string? GroupName,
    [StringLength(200)] string? ContactPersonName,
    [StringLength(100)] string? JointPrimaryIdentityNumber,
    [StringLength(100)] string? JointSecondaryIdentityNumber,
    [StringLength(100)] string? TaxNumber,
    [StringLength(100)] string? CountryOfTaxResidence,
    [StringLength(500)] string? AddressLine1,
    [StringLength(100)] string? MandateType,
    [StringLength(200)] string? SigningAuthority,
    DateOnly? MandateEffectiveFrom,
    IReadOnlyCollection<CreateBeneficialOwnerRequest>? BeneficialOwners);

public sealed record CreateBeneficialOwnerRequest(
    [Required, StringLength(200)] string FullName,
    [Required, StringLength(100)] string IdentityNumber,
    [Range(typeof(decimal), "0", "100")] decimal OwnershipPercentage,
    bool IsPoliticallyExposed);

public sealed record UpdateInvestorRequest(
    [Required, StringLength(200)] string DisplayName,
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(50)] string PhoneNumber,
    [StringLength(100)] string? MandateType,
    [StringLength(200)] string? SigningAuthority,
    DateOnly? MandateEffectiveFrom,
    [StringLength(1000)] string? Reason);

public sealed record AddKycDocumentRequest(
    [Required, StringLength(100)] string DocumentType,
    [Required, StringLength(255)] string FileName,
    [Required, StringLength(100)] string ContentType,
    [Range(1, long.MaxValue)] long SizeBytes,
    [Required, StringLength(500)] string StorageReference,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate);

public sealed record AddInvestorBankAccountRequest(
    [Required, StringLength(200)] string BankName,
    [Required, StringLength(100)] string AccountNumber,
    [Required, StringLength(200)] string AccountName,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [StringLength(20)] string? SwiftCode,
    bool HighRiskFlag,
    [StringLength(1000)] string? Reason);

public sealed record InvestorWorkflowDecisionRequest(
    [StringLength(1000)] string? Comment);

public sealed record StartAmlScreeningRequest(
    [Required, StringLength(100)] string ProviderName,
    [StringLength(100)] string? ScreeningReference,
    IReadOnlyCollection<ManualAmlHitRequest>? ManualHits);

public sealed record ManualAmlHitRequest(
    [Required, StringLength(100)] string ListName,
    [Required, StringLength(200)] string MatchedName,
    [Required] string RiskLevel,
    [StringLength(1000)] string? Notes);

public sealed record InvestorDto(
    Guid Id,
    string InvestorNumber,
    string InvestorType,
    string DisplayName,
    string Email,
    string PhoneNumber,
    string Status,
    string? RiskCategory,
    string? SubmittedByUserId,
    string? ApprovedByUserId,
    IReadOnlyCollection<InvestorProfileIndividualDto> IndividualProfiles,
    IReadOnlyCollection<InvestorProfileCorporateDto> CorporateProfiles,
    IReadOnlyCollection<InvestorProfileJointDto> JointProfiles,
    IReadOnlyCollection<InvestorProfileGroupDto> GroupProfiles,
    IReadOnlyCollection<BeneficialOwnerDto> BeneficialOwners,
    IReadOnlyCollection<InvestorBankAccountDto> BankAccounts,
    IReadOnlyCollection<InvestorTaxProfileDto> TaxProfiles,
    IReadOnlyCollection<InvestorContactDto> Contacts,
    IReadOnlyCollection<InvestorMandateDto> Mandates,
    IReadOnlyCollection<KycDocumentDto> KycDocuments,
    IReadOnlyCollection<KycRequirementDto> KycRequirements,
    IReadOnlyCollection<KycReviewDto> KycReviews,
    IReadOnlyCollection<AmlScreeningCaseDto> AmlScreeningCases,
    IReadOnlyCollection<InvestorRiskClassificationDto> RiskClassifications,
    IReadOnlyCollection<DuplicateDetectionResultDto> DuplicateDetectionResults,
    IReadOnlyCollection<InvestorChangeLogDto> ChangeLogs);

public sealed record InvestorProfileIndividualDto(Guid Id, string FirstName, string LastName, string IdentityNumber, DateOnly DateOfBirth, string Nationality);

public sealed record InvestorProfileCorporateDto(Guid Id, string RegisteredName, string RegistrationNumber, DateOnly IncorporationDate);

public sealed record InvestorProfileJointDto(Guid Id, string JointName, string PrimaryIdentityNumber, string SecondaryIdentityNumber);

public sealed record InvestorProfileGroupDto(Guid Id, string GroupName, string RegistrationNumber, string ContactPersonName);

public sealed record BeneficialOwnerDto(Guid Id, string FullName, string IdentityNumber, decimal OwnershipPercentage, bool IsPoliticallyExposed);

public sealed record InvestorBankAccountDto(Guid Id, string BankName, string AccountNumber, string AccountName, string Currency, string? SwiftCode, bool IsActive, bool HighRiskFlag);

public sealed record InvestorTaxProfileDto(Guid Id, string TaxNumber, string CountryOfTaxResidence);

public sealed record InvestorContactDto(Guid Id, string ContactType, string Value, bool IsPrimary);

public sealed record InvestorMandateDto(Guid Id, string MandateType, string SigningAuthority, DateOnly EffectiveFrom, bool IsActive);

public sealed record KycDocumentDto(
    Guid Id,
    Guid InvestorId,
    string InvestorNumber,
    string DocumentType,
    string FileName,
    string ContentType,
    long SizeBytes,
    string StorageReference,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string Status,
    DateTime UploadedAtUtc);

public sealed record KycRequirementDto(Guid Id, string DocumentType, bool IsMandatory, string Status, Guid? SatisfiedByDocumentId);

public sealed record KycReviewDto(Guid Id, string Status, string PerformedByUserId, DateTime PerformedAtUtc, string? Comment);

public sealed record AmlScreeningCaseDto(Guid Id, string ProviderName, string ScreeningReference, DateTime ScreenedAtUtc, string Status, IReadOnlyCollection<AmlScreeningHitDto> Hits);

public sealed record AmlScreeningHitDto(Guid Id, string ListName, string MatchedName, string RiskLevel, string? Notes, bool IsResolved);

public sealed record AmlExceptionDto(Guid InvestorId, string InvestorNumber, Guid ScreeningCaseId, string ProviderName, string MatchedName, string RiskLevel, string? Notes);

public sealed record InvestorRiskClassificationDto(Guid Id, string RiskCategory, string Reason, string Status, string AssignedByUserId, DateTime AssignedAtUtc, string? ApprovedByUserId);

public sealed record DuplicateDetectionResultDto(Guid Id, string MatchType, string MatchedValue, Guid MatchedInvestorId, string MatchedInvestorNumber, string Status, DateTime DetectedAtUtc);

public sealed record InvestorChangeLogDto(Guid Id, string ChangeType, string BeforeJson, string AfterJson, bool HighRiskFlag, string ChangedByUserId, DateTime ChangedAtUtc, string? Reason);

public sealed record IncompleteKycInvestorDto(Guid InvestorId, string InvestorNumber, string DisplayName, string Status, IReadOnlyCollection<string> MissingOrExpiredDocumentTypes);
