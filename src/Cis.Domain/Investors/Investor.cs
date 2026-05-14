using System.Text.Json;
using Cis.Domain.Common;

namespace Cis.Domain.Investors;

public sealed class Investor : AuditableAggregateRoot
{
    private readonly List<InvestorProfileIndividual> _individualProfiles = [];
    private readonly List<InvestorProfileCorporate> _corporateProfiles = [];
    private readonly List<InvestorProfileJoint> _jointProfiles = [];
    private readonly List<InvestorProfileGroup> _groupProfiles = [];
    private readonly List<BeneficialOwner> _beneficialOwners = [];
    private readonly List<InvestorBankAccount> _bankAccounts = [];
    private readonly List<InvestorTaxProfile> _taxProfiles = [];
    private readonly List<InvestorContact> _contacts = [];
    private readonly List<InvestorMandate> _mandates = [];
    private readonly List<KycDocument> _kycDocuments = [];
    private readonly List<KycRequirement> _kycRequirements = [];
    private readonly List<KycReview> _kycReviews = [];
    private readonly List<AmlScreeningCase> _amlScreeningCases = [];
    private readonly List<InvestorRiskClassification> _riskClassifications = [];
    private readonly List<DuplicateDetectionResult> _duplicateDetectionResults = [];
    private readonly List<InvestorChangeLog> _changeLogs = [];

    private Investor()
    {
    }

    private Investor(
        string investorNumber,
        InvestorType investorType,
        string displayName,
        string email,
        string phoneNumber,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        InvestorValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        InvestorNumber = InvestorValidation.Required(investorNumber, nameof(investorNumber), 50).ToUpperInvariant();
        InvestorType = investorType;
        DisplayName = InvestorValidation.Required(displayName, nameof(displayName), 200);
        Email = InvestorValidation.Required(email, nameof(email), 320).ToLowerInvariant();
        PhoneNumber = InvestorValidation.Required(phoneNumber, nameof(phoneNumber), 50);
        Status = InvestorStatus.Draft;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string InvestorNumber { get; private set; } = string.Empty;

    public InvestorType InvestorType { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PhoneNumber { get; private set; } = string.Empty;

    public InvestorStatus Status { get; private set; }

    public InvestorRiskCategory? RiskCategory { get; private set; }

    public string? SubmittedByUserId { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public string? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public string? RejectedByUserId { get; private set; }

    public DateTime? RejectedAtUtc { get; private set; }

    public string? DecisionComment { get; private set; }

    public IReadOnlyCollection<InvestorProfileIndividual> IndividualProfiles => _individualProfiles.AsReadOnly();

    public IReadOnlyCollection<InvestorProfileCorporate> CorporateProfiles => _corporateProfiles.AsReadOnly();

    public IReadOnlyCollection<InvestorProfileJoint> JointProfiles => _jointProfiles.AsReadOnly();

    public IReadOnlyCollection<InvestorProfileGroup> GroupProfiles => _groupProfiles.AsReadOnly();

    public IReadOnlyCollection<BeneficialOwner> BeneficialOwners => _beneficialOwners.AsReadOnly();

    public IReadOnlyCollection<InvestorBankAccount> BankAccounts => _bankAccounts.AsReadOnly();

    public IReadOnlyCollection<InvestorTaxProfile> TaxProfiles => _taxProfiles.AsReadOnly();

    public IReadOnlyCollection<InvestorContact> Contacts => _contacts.AsReadOnly();

    public IReadOnlyCollection<InvestorMandate> Mandates => _mandates.AsReadOnly();

    public IReadOnlyCollection<KycDocument> KycDocuments => _kycDocuments.AsReadOnly();

    public IReadOnlyCollection<KycRequirement> KycRequirements => _kycRequirements.AsReadOnly();

    public IReadOnlyCollection<KycReview> KycReviews => _kycReviews.AsReadOnly();

    public IReadOnlyCollection<AmlScreeningCase> AmlScreeningCases => _amlScreeningCases.AsReadOnly();

    public IReadOnlyCollection<InvestorRiskClassification> RiskClassifications => _riskClassifications.AsReadOnly();

    public IReadOnlyCollection<DuplicateDetectionResult> DuplicateDetectionResults => _duplicateDetectionResults.AsReadOnly();

    public IReadOnlyCollection<InvestorChangeLog> ChangeLogs => _changeLogs.AsReadOnly();

    public static Investor Create(
        string investorNumber,
        InvestorType investorType,
        string displayName,
        string email,
        string phoneNumber,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new Investor(investorNumber, investorType, displayName, email, phoneNumber, createdByUserId, createdAtUtc);
    }

    public void AttachIndividualProfile(string firstName, string lastName, string identityNumber, DateOnly dateOfBirth, string nationality)
    {
        EnsureCanChange();
        EnsureType(InvestorType.Individual);
        _individualProfiles.Clear();
        _individualProfiles.Add(InvestorProfileIndividual.Create(Id, firstName, lastName, identityNumber, dateOfBirth, nationality));
    }

    public void AttachCorporateProfile(string registeredName, string registrationNumber, DateOnly incorporationDate)
    {
        EnsureCanChange();
        EnsureType(InvestorType.Corporate);
        _corporateProfiles.Clear();
        _corporateProfiles.Add(InvestorProfileCorporate.Create(Id, registeredName, registrationNumber, incorporationDate));
    }

    public void AttachJointProfile(string jointName, string primaryIdentityNumber, string secondaryIdentityNumber)
    {
        EnsureCanChange();
        EnsureType(InvestorType.Joint);
        _jointProfiles.Clear();
        _jointProfiles.Add(InvestorProfileJoint.Create(Id, jointName, primaryIdentityNumber, secondaryIdentityNumber));
    }

    public void AttachGroupProfile(string groupName, string registrationNumber, string contactPersonName)
    {
        EnsureCanChange();
        EnsureType(InvestorType.Group);
        _groupProfiles.Clear();
        _groupProfiles.Add(InvestorProfileGroup.Create(Id, groupName, registrationNumber, contactPersonName));
    }

    public void UpdateContactSummary(string displayName, string email, string phoneNumber, string changedByUserId, DateTime changedAtUtc, string? reason)
    {
        EnsureCanChange();
        var before = JsonSerializer.Serialize(new { DisplayName, Email, PhoneNumber });
        DisplayName = InvestorValidation.Required(displayName, nameof(displayName), 200);
        Email = InvestorValidation.Required(email, nameof(email), 320).ToLowerInvariant();
        PhoneNumber = InvestorValidation.Required(phoneNumber, nameof(phoneNumber), 50);
        var after = JsonSerializer.Serialize(new { DisplayName, Email, PhoneNumber });
        _changeLogs.Add(InvestorChangeLog.Create(Id, "ContactChanged", before, after, false, changedByUserId, changedAtUtc, reason));
    }

    public void AddDefaultKycRequirements(IEnumerable<string> documentTypes)
    {
        foreach (var documentType in documentTypes)
        {
            if (_kycRequirements.Any(requirement => string.Equals(requirement.DocumentType, documentType, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            _kycRequirements.Add(KycRequirement.Create(Id, documentType, true));
        }
    }

    public void AddBeneficialOwner(string fullName, string identityNumber, decimal ownershipPercentage, bool isPoliticallyExposed)
    {
        EnsureCanChange();
        _beneficialOwners.Add(BeneficialOwner.Create(Id, fullName, identityNumber, ownershipPercentage, isPoliticallyExposed));
    }

    public void SetTaxProfile(string taxNumber, string countryOfTaxResidence)
    {
        EnsureCanChange();
        _taxProfiles.Clear();
        _taxProfiles.Add(InvestorTaxProfile.Create(Id, taxNumber, countryOfTaxResidence));
    }

    public void AddContact(string contactType, string value, bool isPrimary, string changedByUserId, DateTime changedAtUtc)
    {
        EnsureCanChange();
        var contact = InvestorContact.Create(Id, contactType, value, isPrimary);
        _contacts.Add(contact);
        _changeLogs.Add(InvestorChangeLog.Create(Id, "ContactChanged", "{}", JsonSerializer.Serialize(contact), false, changedByUserId, changedAtUtc, "Investor contact captured."));
    }

    public void AddMandate(string mandateType, string signingAuthority, BusinessDate effectiveFrom, string changedByUserId, DateTime changedAtUtc)
    {
        EnsureCanChange();
        var mandate = InvestorMandate.Create(Id, mandateType, signingAuthority, effectiveFrom);
        _mandates.Add(mandate);
        _changeLogs.Add(InvestorChangeLog.Create(Id, "MandateChanged", "{}", JsonSerializer.Serialize(mandate), false, changedByUserId, changedAtUtc, "Investor mandate captured."));
    }

    public KycDocument AddDocument(
        string documentType,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageReference,
        BusinessDate? issueDate,
        BusinessDate? expiryDate,
        string uploadedByUserId,
        DateTime uploadedAtUtc,
        BusinessDate asOf)
    {
        EnsureCanChange();
        var document = KycDocument.Create(Id, documentType, fileName, contentType, sizeBytes, storageReference, issueDate, expiryDate, uploadedByUserId, uploadedAtUtc);
        _kycDocuments.Add(document);

        var requirement = _kycRequirements.FirstOrDefault(candidate => string.Equals(candidate.DocumentType, document.DocumentType, StringComparison.OrdinalIgnoreCase));
        if (requirement is not null)
        {
            if (document.IsExpiredOn(asOf))
            {
                requirement.MarkExpired();
            }
            else
            {
                requirement.MarkSatisfied(document.Id);
            }
        }

        return document;
    }

    public InvestorBankAccount AddBankAccount(
        string bankName,
        string accountNumber,
        string accountName,
        string currency,
        string? swiftCode,
        bool highRiskFlag,
        string changedByUserId,
        DateTime changedAtUtc,
        string? reason)
    {
        EnsureCanChange();
        if (_bankAccounts.Any(account =>
                account.IsActive
                && string.Equals(account.AccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase)
                && string.Equals(account.Currency, currency, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("A bank account with the same account number and currency already exists for this investor.");
        }

        var effectiveHighRiskFlag = highRiskFlag || RiskCategory == InvestorRiskCategory.High || HasUnresolvedHighRiskAmlHits();
        var account = InvestorBankAccount.Create(Id, bankName, accountNumber, accountName, currency, swiftCode, effectiveHighRiskFlag);
        _bankAccounts.Add(account);
        _changeLogs.Add(InvestorChangeLog.Create(Id, "BankAccountChanged", "{}", JsonSerializer.Serialize(account), effectiveHighRiskFlag, changedByUserId, changedAtUtc, reason));
        return account;
    }

    public void AssignRiskClassification(InvestorRiskCategory riskCategory, string reason, string assignedByUserId, DateTime assignedAtUtc)
    {
        EnsureCanChange();
        _riskClassifications.Add(InvestorRiskClassification.Create(Id, riskCategory, reason, assignedByUserId, assignedAtUtc));
        RiskCategory ??= riskCategory;
    }

    public AmlScreeningCase AddAmlScreeningCase(
        string providerName,
        string screeningReference,
        IEnumerable<(string ListName, string MatchedName, AmlHitRiskLevel RiskLevel, string? Notes)> hits,
        DateTime screenedAtUtc)
    {
        var screeningCase = AmlScreeningCase.Create(Id, providerName, screeningReference, screenedAtUtc);
        foreach (var hit in hits)
        {
            screeningCase.AddHit(hit.ListName, hit.MatchedName, hit.RiskLevel, hit.Notes);
        }

        _amlScreeningCases.Add(screeningCase);
        return screeningCase;
    }

    public void AddDuplicateWarning(string matchType, string matchedValue, Guid matchedInvestorId, string matchedInvestorNumber, DateTime detectedAtUtc)
    {
        if (matchedInvestorId == Id)
        {
            return;
        }

        if (_duplicateDetectionResults.Any(result =>
                result.MatchedInvestorId == matchedInvestorId
                && string.Equals(result.MatchType, matchType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(result.MatchedValue, matchedValue, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _duplicateDetectionResults.Add(DuplicateDetectionResult.Create(Id, matchType, matchedValue, matchedInvestorId, matchedInvestorNumber, detectedAtUtc));
    }

    public void SubmitKyc(string submittedByUserId, DateTime submittedAtUtc)
    {
        InvestorValidation.EnsureUtc(submittedAtUtc, nameof(submittedAtUtc));
        if (Status is not (InvestorStatus.Draft or InvestorStatus.Rejected))
        {
            throw new InvalidOperationException("Only draft or rejected investors can be submitted for KYC review.");
        }

        SubmittedByUserId = InvestorValidation.Required(submittedByUserId, nameof(submittedByUserId), 200);
        SubmittedAtUtc = submittedAtUtc;
        Status = InvestorStatus.PendingReview;
        _kycReviews.Add(KycReview.Create(Id, KycReviewStatus.Submitted, submittedByUserId, submittedAtUtc, "KYC submitted for review."));
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc, BusinessDate asOf, string? comment)
    {
        InvestorValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        if (Status != InvestorStatus.PendingReview)
        {
            throw new InvalidOperationException("Only investors pending review can be approved.");
        }

        var actor = InvestorValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, SubmittedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: submitter cannot approve the same investor workflow.");
        }

        var missingDocuments = MissingRequiredDocumentTypes(asOf);
        if (missingDocuments.Count > 0)
        {
            throw new InvalidOperationException($"Investor cannot be approved because required KYC documents are missing or expired: {string.Join(", ", missingDocuments)}.");
        }

        if (HasUnresolvedHighRiskAmlHits())
        {
            throw new InvalidOperationException("Investor cannot be approved with unresolved AML high-risk hits.");
        }

        var pendingClassification = _riskClassifications.LastOrDefault(classification => classification.Status == RiskClassificationStatus.PendingApproval);
        if (pendingClassification is not null)
        {
            pendingClassification.Approve(actor, approvedAtUtc);
            RiskCategory = pendingClassification.RiskCategory;
        }

        Status = InvestorStatus.Approved;
        ApprovedByUserId = actor;
        ApprovedAtUtc = approvedAtUtc;
        DecisionComment = InvestorValidation.Optional(comment, 1000);
        _kycReviews.Add(KycReview.Create(Id, KycReviewStatus.Approved, actor, approvedAtUtc, comment));
    }

    public void Reject(string rejectedByUserId, DateTime rejectedAtUtc, string? comment)
    {
        InvestorValidation.EnsureUtc(rejectedAtUtc, nameof(rejectedAtUtc));
        if (Status != InvestorStatus.PendingReview)
        {
            throw new InvalidOperationException("Only investors pending review can be rejected.");
        }

        RejectedByUserId = InvestorValidation.Required(rejectedByUserId, nameof(rejectedByUserId), 200);
        RejectedAtUtc = rejectedAtUtc;
        DecisionComment = InvestorValidation.Optional(comment, 1000);
        Status = InvestorStatus.Rejected;
        _kycReviews.Add(KycReview.Create(Id, KycReviewStatus.Rejected, rejectedByUserId, rejectedAtUtc, comment));
    }

    public void Suspend(string suspendedByUserId, DateTime suspendedAtUtc, string? comment)
    {
        InvestorValidation.EnsureUtc(suspendedAtUtc, nameof(suspendedAtUtc));
        if (Status != InvestorStatus.Approved)
        {
            throw new InvalidOperationException("Only approved investors can be suspended.");
        }

        Status = InvestorStatus.Suspended;
        DecisionComment = InvestorValidation.Optional(comment, 1000);
        _kycReviews.Add(KycReview.Create(Id, KycReviewStatus.Rejected, suspendedByUserId, suspendedAtUtc, comment));
    }

    public void Close(string closedByUserId, DateTime closedAtUtc, string? comment)
    {
        InvestorValidation.EnsureUtc(closedAtUtc, nameof(closedAtUtc));
        if (Status == InvestorStatus.Closed)
        {
            throw new InvalidOperationException("Investor is already closed.");
        }

        Status = InvestorStatus.Closed;
        DecisionComment = InvestorValidation.Optional(comment, 1000);
        _kycReviews.Add(KycReview.Create(Id, KycReviewStatus.Rejected, closedByUserId, closedAtUtc, comment));
    }

    public IReadOnlyCollection<string> MissingRequiredDocumentTypes(BusinessDate asOf)
    {
        var missing = new List<string>();
        foreach (var requirement in _kycRequirements.Where(requirement => requirement.IsMandatory))
        {
            var latest = _kycDocuments
                .Where(document => string.Equals(document.DocumentType, requirement.DocumentType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(document => document.UploadedAtUtc)
                .FirstOrDefault();
            if (latest is null || latest.IsExpiredOn(asOf))
            {
                requirement.MarkExpired();
                missing.Add(requirement.DocumentType);
            }
        }

        return missing;
    }

    public bool HasUnresolvedHighRiskAmlHits()
    {
        return _amlScreeningCases.Any(screeningCase => screeningCase.HasUnresolvedHighRiskHit());
    }

    private void EnsureCanChange()
    {
        if (Status is InvestorStatus.PendingReview or InvestorStatus.Closed)
        {
            throw new InvalidOperationException("Investors pending review or closed investors cannot be amended.");
        }
    }

    private void EnsureType(InvestorType expectedType)
    {
        if (InvestorType != expectedType)
        {
            throw new InvalidOperationException($"Investor type must be {expectedType}.");
        }
    }
}
