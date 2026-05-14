using Cis.Domain.Common;

namespace Cis.Domain.Investors;

public sealed class KycDocument : Entity
{
    private KycDocument()
    {
    }

    private KycDocument(
        Guid investorId,
        string documentType,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageReference,
        BusinessDate? issueDate,
        BusinessDate? expiryDate,
        string uploadedByUserId,
        DateTime uploadedAtUtc)
    {
        InvestorValidation.EnsureUtc(uploadedAtUtc, nameof(uploadedAtUtc));
        InvestorId = investorId;
        DocumentType = InvestorValidation.Required(documentType, nameof(documentType), 100);
        FileName = InvestorValidation.Required(fileName, nameof(fileName), 255);
        ContentType = InvestorValidation.Required(contentType, nameof(contentType), 100);
        SizeBytes = InvestorValidation.NonNegative(sizeBytes, nameof(sizeBytes));
        StorageReference = InvestorValidation.Required(storageReference, nameof(storageReference), 500);
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        Status = KycDocumentStatus.Uploaded;
        UploadedByUserId = InvestorValidation.Required(uploadedByUserId, nameof(uploadedByUserId), 200);
        UploadedAtUtc = uploadedAtUtc;
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string DocumentType { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string StorageReference { get; private set; } = string.Empty;

    public BusinessDate? IssueDate { get; private set; }

    public BusinessDate? ExpiryDate { get; private set; }

    public KycDocumentStatus Status { get; private set; }

    public string UploadedByUserId { get; private set; } = string.Empty;

    public DateTime UploadedAtUtc { get; private set; }

    public static KycDocument Create(
        Guid investorId,
        string documentType,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageReference,
        BusinessDate? issueDate,
        BusinessDate? expiryDate,
        string uploadedByUserId,
        DateTime uploadedAtUtc)
    {
        return new KycDocument(investorId, documentType, fileName, contentType, sizeBytes, storageReference, issueDate, expiryDate, uploadedByUserId, uploadedAtUtc);
    }

    public bool IsExpiredOn(BusinessDate asOf)
    {
        return Status == KycDocumentStatus.Expired || (ExpiryDate is not null && ExpiryDate.CompareTo(asOf) < 0);
    }
}

public sealed class KycRequirement : Entity
{
    private KycRequirement()
    {
    }

    private KycRequirement(Guid investorId, string documentType, bool isMandatory)
    {
        InvestorId = investorId;
        DocumentType = InvestorValidation.Required(documentType, nameof(documentType), 100);
        IsMandatory = isMandatory;
        Status = KycRequirementStatus.Missing;
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string DocumentType { get; private set; } = string.Empty;

    public bool IsMandatory { get; private set; }

    public KycRequirementStatus Status { get; private set; }

    public Guid? SatisfiedByDocumentId { get; private set; }

    public static KycRequirement Create(Guid investorId, string documentType, bool isMandatory)
    {
        return new KycRequirement(investorId, documentType, isMandatory);
    }

    public void MarkSatisfied(Guid documentId)
    {
        SatisfiedByDocumentId = documentId;
        Status = KycRequirementStatus.Satisfied;
    }

    public void MarkExpired()
    {
        Status = KycRequirementStatus.Expired;
    }
}

public sealed class KycReview : Entity
{
    private KycReview()
    {
    }

    private KycReview(Guid investorId, KycReviewStatus status, string performedByUserId, DateTime performedAtUtc, string? comment)
    {
        InvestorValidation.EnsureUtc(performedAtUtc, nameof(performedAtUtc));
        InvestorId = investorId;
        Status = status;
        PerformedByUserId = InvestorValidation.Required(performedByUserId, nameof(performedByUserId), 200);
        PerformedAtUtc = performedAtUtc;
        Comment = InvestorValidation.Optional(comment, 1000);
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public KycReviewStatus Status { get; private set; }

    public string PerformedByUserId { get; private set; } = string.Empty;

    public DateTime PerformedAtUtc { get; private set; }

    public string? Comment { get; private set; }

    public static KycReview Create(Guid investorId, KycReviewStatus status, string performedByUserId, DateTime performedAtUtc, string? comment)
    {
        return new KycReview(investorId, status, performedByUserId, performedAtUtc, comment);
    }
}

public sealed class AmlScreeningCase : Entity
{
    private readonly List<AmlScreeningHit> _hits = [];

    private AmlScreeningCase()
    {
    }

    private AmlScreeningCase(Guid investorId, string providerName, string screeningReference, DateTime screenedAtUtc)
    {
        InvestorValidation.EnsureUtc(screenedAtUtc, nameof(screenedAtUtc));
        InvestorId = investorId;
        ProviderName = InvestorValidation.Required(providerName, nameof(providerName), 100);
        ScreeningReference = InvestorValidation.Required(screeningReference, nameof(screeningReference), 100);
        ScreenedAtUtc = screenedAtUtc;
        Status = AmlScreeningStatus.Cleared;
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string ProviderName { get; private set; } = string.Empty;

    public string ScreeningReference { get; private set; } = string.Empty;

    public DateTime ScreenedAtUtc { get; private set; }

    public AmlScreeningStatus Status { get; private set; }

    public IReadOnlyCollection<AmlScreeningHit> Hits => _hits.AsReadOnly();

    public static AmlScreeningCase Create(Guid investorId, string providerName, string screeningReference, DateTime screenedAtUtc)
    {
        return new AmlScreeningCase(investorId, providerName, screeningReference, screenedAtUtc);
    }

    public void AddHit(string listName, string matchedName, AmlHitRiskLevel riskLevel, string? notes)
    {
        _hits.Add(AmlScreeningHit.Create(Id, listName, matchedName, riskLevel, notes));
        if (riskLevel == AmlHitRiskLevel.High)
        {
            Status = AmlScreeningStatus.Exception;
        }
    }

    public bool HasUnresolvedHighRiskHit()
    {
        return Status == AmlScreeningStatus.Exception && _hits.Any(hit => hit.RiskLevel == AmlHitRiskLevel.High && !hit.IsResolved);
    }
}

public sealed class AmlScreeningHit : Entity
{
    private AmlScreeningHit()
    {
    }

    private AmlScreeningHit(Guid amlScreeningCaseId, string listName, string matchedName, AmlHitRiskLevel riskLevel, string? notes)
    {
        AmlScreeningCaseId = amlScreeningCaseId;
        ListName = InvestorValidation.Required(listName, nameof(listName), 100);
        MatchedName = InvestorValidation.Required(matchedName, nameof(matchedName), 200);
        RiskLevel = riskLevel;
        Notes = InvestorValidation.Optional(notes, 1000);
        IsResolved = false;
    }

    public Guid AmlScreeningCaseId { get; private set; }

    public AmlScreeningCase AmlScreeningCase { get; private set; } = null!;

    public string ListName { get; private set; } = string.Empty;

    public string MatchedName { get; private set; } = string.Empty;

    public AmlHitRiskLevel RiskLevel { get; private set; }

    public string? Notes { get; private set; }

    public bool IsResolved { get; private set; }

    public static AmlScreeningHit Create(Guid amlScreeningCaseId, string listName, string matchedName, AmlHitRiskLevel riskLevel, string? notes)
    {
        return new AmlScreeningHit(amlScreeningCaseId, listName, matchedName, riskLevel, notes);
    }
}

public sealed class InvestorRiskClassification : Entity
{
    private InvestorRiskClassification()
    {
    }

    private InvestorRiskClassification(Guid investorId, InvestorRiskCategory riskCategory, string reason, string assignedByUserId, DateTime assignedAtUtc)
    {
        InvestorValidation.EnsureUtc(assignedAtUtc, nameof(assignedAtUtc));
        InvestorId = investorId;
        RiskCategory = riskCategory;
        Reason = InvestorValidation.Required(reason, nameof(reason), 1000);
        AssignedByUserId = InvestorValidation.Required(assignedByUserId, nameof(assignedByUserId), 200);
        AssignedAtUtc = assignedAtUtc;
        Status = RiskClassificationStatus.PendingApproval;
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public InvestorRiskCategory RiskCategory { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public RiskClassificationStatus Status { get; private set; }

    public string AssignedByUserId { get; private set; } = string.Empty;

    public DateTime AssignedAtUtc { get; private set; }

    public string? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public static InvestorRiskClassification Create(Guid investorId, InvestorRiskCategory riskCategory, string reason, string assignedByUserId, DateTime assignedAtUtc)
    {
        return new InvestorRiskClassification(investorId, riskCategory, reason, assignedByUserId, assignedAtUtc);
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc)
    {
        InvestorValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        if (string.Equals(AssignedByUserId, approvedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: risk classification assigner cannot approve the same classification.");
        }

        Status = RiskClassificationStatus.Approved;
        ApprovedByUserId = InvestorValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        ApprovedAtUtc = approvedAtUtc;
    }
}

public sealed class DuplicateDetectionResult : Entity
{
    private DuplicateDetectionResult()
    {
    }

    private DuplicateDetectionResult(Guid investorId, string matchType, string matchedValue, Guid matchedInvestorId, string matchedInvestorNumber, DateTime detectedAtUtc)
    {
        InvestorValidation.EnsureUtc(detectedAtUtc, nameof(detectedAtUtc));
        InvestorId = investorId;
        MatchType = InvestorValidation.Required(matchType, nameof(matchType), 100);
        MatchedValue = InvestorValidation.Required(matchedValue, nameof(matchedValue), 300);
        MatchedInvestorId = matchedInvestorId;
        MatchedInvestorNumber = InvestorValidation.Required(matchedInvestorNumber, nameof(matchedInvestorNumber), 100);
        Status = DuplicateDetectionStatus.Warning;
        DetectedAtUtc = detectedAtUtc;
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string MatchType { get; private set; } = string.Empty;

    public string MatchedValue { get; private set; } = string.Empty;

    public Guid MatchedInvestorId { get; private set; }

    public string MatchedInvestorNumber { get; private set; } = string.Empty;

    public DuplicateDetectionStatus Status { get; private set; }

    public DateTime DetectedAtUtc { get; private set; }

    public static DuplicateDetectionResult Create(Guid investorId, string matchType, string matchedValue, Guid matchedInvestorId, string matchedInvestorNumber, DateTime detectedAtUtc)
    {
        return new DuplicateDetectionResult(investorId, matchType, matchedValue, matchedInvestorId, matchedInvestorNumber, detectedAtUtc);
    }
}
