using Cis.Domain.Common;

namespace Cis.Domain.Portal;

public sealed class PortalUserProfile : AuditableAggregateRoot
{
    private PortalUserProfile()
    {
    }

    private PortalUserProfile(Guid userId, Guid investorId, string displayName, string email, string createdByUserId, DateTime createdAtUtc)
    {
        UserId = userId != Guid.Empty ? userId : throw new ArgumentException("User id cannot be empty.", nameof(userId));
        InvestorId = investorId != Guid.Empty ? investorId : throw new ArgumentException("Investor id cannot be empty.", nameof(investorId));
        DisplayName = PortalValidation.Required(displayName, nameof(displayName), 200);
        Email = PortalValidation.Required(email, nameof(email), 320).ToLowerInvariant();
        Status = PortalProfileStatus.Active;
        CreatedByUserId = PortalValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = PortalValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public Guid UserId { get; private set; }
    public Guid InvestorId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public PortalProfileStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static PortalUserProfile Create(Guid userId, Guid investorId, string displayName, string email, string createdByUserId, DateTime createdAtUtc)
    {
        return new PortalUserProfile(userId, investorId, displayName, email, createdByUserId, createdAtUtc);
    }
}

public sealed class PortalSession : AuditableAggregateRoot
{
    private PortalSession()
    {
    }

    private PortalSession(Guid userId, Guid investorId, string? ipAddress, string? userAgent, bool mfaSatisfied, DateTime startedAtUtc)
    {
        UserId = userId;
        InvestorId = investorId;
        IpAddress = PortalValidation.Optional(ipAddress, 100);
        UserAgent = PortalValidation.Optional(userAgent, 500);
        MfaSatisfied = mfaSatisfied;
        StartedAtUtc = PortalValidation.EnsureUtc(startedAtUtc, nameof(startedAtUtc));
        LastSeenAtUtc = StartedAtUtc;
        Status = PortalSessionStatus.Active;
        MarkCreated(userId.ToString(), StartedAtUtc);
    }

    public Guid UserId { get; private set; }
    public Guid InvestorId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public bool MfaSatisfied { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime LastSeenAtUtc { get; private set; }
    public PortalSessionStatus Status { get; private set; }

    public static PortalSession Start(Guid userId, Guid investorId, string? ipAddress, string? userAgent, bool mfaSatisfied, DateTime startedAtUtc)
    {
        return new PortalSession(userId, investorId, ipAddress, userAgent, mfaSatisfied, startedAtUtc);
    }

    public void Touch(DateTime seenAtUtc)
    {
        LastSeenAtUtc = PortalValidation.EnsureUtc(seenAtUtc, nameof(seenAtUtc));
    }
}

public sealed class PortalActivityLog : AuditableAggregateRoot
{
    private PortalActivityLog()
    {
    }

    private PortalActivityLog(Guid userId, Guid investorId, Guid? portalSessionId, PortalActivityType activityType, string summary, string? entityType, string? entityId, string? ipAddress, string? correlationId, DateTime occurredAtUtc)
    {
        UserId = userId;
        InvestorId = investorId;
        PortalSessionId = portalSessionId;
        ActivityType = activityType;
        Summary = PortalValidation.Required(summary, nameof(summary), 500);
        EntityType = PortalValidation.Optional(entityType, 100);
        EntityId = PortalValidation.Optional(entityId, 100);
        IpAddress = PortalValidation.Optional(ipAddress, 100);
        CorrelationId = PortalValidation.Optional(correlationId, 100);
        OccurredAtUtc = PortalValidation.EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        MarkCreated(userId.ToString(), OccurredAtUtc);
    }

    public Guid UserId { get; private set; }
    public Guid InvestorId { get; private set; }
    public Guid? PortalSessionId { get; private set; }
    public PortalActivityType ActivityType { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    public static PortalActivityLog Create(Guid userId, Guid investorId, Guid? portalSessionId, PortalActivityType activityType, string summary, string? entityType, string? entityId, string? ipAddress, string? correlationId, DateTime occurredAtUtc)
    {
        return new PortalActivityLog(userId, investorId, portalSessionId, activityType, summary, entityType, entityId, ipAddress, correlationId, occurredAtUtc);
    }
}

public sealed class DigitalServiceRequest : AuditableAggregateRoot
{
    private DigitalServiceRequest()
    {
    }

    private DigitalServiceRequest(Guid investorId, Guid userId, DigitalServiceRequestType requestType, string requestPayloadJson, string submittedByUserId, DateTime submittedAtUtc)
    {
        InvestorId = investorId;
        UserId = userId;
        RequestType = requestType;
        RequestPayloadJson = PortalValidation.Required(requestPayloadJson, nameof(requestPayloadJson), 12000);
        Status = DigitalServiceRequestStatus.Submitted;
        SubmittedByUserId = PortalValidation.Required(submittedByUserId, nameof(submittedByUserId), 200);
        SubmittedAtUtc = PortalValidation.EnsureUtc(submittedAtUtc, nameof(submittedAtUtc));
        MarkCreated(SubmittedByUserId, SubmittedAtUtc);
    }

    public Guid InvestorId { get; private set; }
    public Guid UserId { get; private set; }
    public DigitalServiceRequestType RequestType { get; private set; }
    public string RequestPayloadJson { get; private set; } = "{}";
    public DigitalServiceRequestStatus Status { get; private set; }
    public Guid? WorkflowId { get; private set; }
    public string SubmittedByUserId { get; private set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; private set; }

    public static DigitalServiceRequest Create(Guid investorId, Guid userId, DigitalServiceRequestType requestType, string requestPayloadJson, string submittedByUserId, DateTime submittedAtUtc)
    {
        return new DigitalServiceRequest(investorId, userId, requestType, requestPayloadJson, submittedByUserId, submittedAtUtc);
    }

    public void LinkWorkflow(Guid workflowId)
    {
        WorkflowId = workflowId != Guid.Empty ? workflowId : throw new ArgumentException("Workflow id cannot be empty.", nameof(workflowId));
        Status = DigitalServiceRequestStatus.WorkflowPending;
    }
}

public sealed class PortalDocumentDownload : AuditableAggregateRoot
{
    private PortalDocumentDownload()
    {
    }

    private PortalDocumentDownload(Guid investorId, Guid userId, string documentType, string documentReference, DateTime downloadedAtUtc, string? ipAddress)
    {
        InvestorId = investorId;
        UserId = userId;
        DocumentType = PortalValidation.Required(documentType, nameof(documentType), 100);
        DocumentReference = PortalValidation.Required(documentReference, nameof(documentReference), 200);
        DownloadedAtUtc = PortalValidation.EnsureUtc(downloadedAtUtc, nameof(downloadedAtUtc));
        IpAddress = PortalValidation.Optional(ipAddress, 100);
        MarkCreated(userId.ToString(), DownloadedAtUtc);
    }

    public Guid InvestorId { get; private set; }
    public Guid UserId { get; private set; }
    public string DocumentType { get; private set; } = string.Empty;
    public string DocumentReference { get; private set; } = string.Empty;
    public DateTime DownloadedAtUtc { get; private set; }
    public string? IpAddress { get; private set; }

    public static PortalDocumentDownload Create(Guid investorId, Guid userId, string documentType, string documentReference, DateTime downloadedAtUtc, string? ipAddress)
    {
        return new PortalDocumentDownload(investorId, userId, documentType, documentReference, downloadedAtUtc, ipAddress);
    }
}

public sealed class InvestorNotice : AuditableAggregateRoot
{
    private InvestorNotice()
    {
    }

    private InvestorNotice(Guid? investorId, string title, string body, BusinessDate publishedDate, DateTime publishedAtUtc, string publishedByUserId)
    {
        InvestorId = investorId;
        Title = PortalValidation.Required(title, nameof(title), 200);
        Body = PortalValidation.Required(body, nameof(body), 4000);
        PublishedDate = publishedDate;
        PublishedAtUtc = PortalValidation.EnsureUtc(publishedAtUtc, nameof(publishedAtUtc));
        PublishedByUserId = PortalValidation.Required(publishedByUserId, nameof(publishedByUserId), 200);
        Status = InvestorNoticeStatus.Published;
        MarkCreated(PublishedByUserId, PublishedAtUtc);
    }

    public Guid? InvestorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public BusinessDate PublishedDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public DateTime PublishedAtUtc { get; private set; }
    public string PublishedByUserId { get; private set; } = string.Empty;
    public InvestorNoticeStatus Status { get; private set; }

    public static InvestorNotice Create(Guid? investorId, string title, string body, BusinessDate publishedDate, DateTime publishedAtUtc, string publishedByUserId)
    {
        return new InvestorNotice(investorId, title, body, publishedDate, publishedAtUtc, publishedByUserId);
    }
}

public sealed class PortalMfaSetting : AuditableAggregateRoot
{
    private PortalMfaSetting()
    {
    }

    private PortalMfaSetting(Guid userId, bool mfaRequired, bool mfaVerified, string updatedByUserId, DateTime updatedAtUtc)
    {
        UserId = userId != Guid.Empty ? userId : throw new ArgumentException("User id cannot be empty.", nameof(userId));
        MfaRequired = mfaRequired;
        MfaVerified = mfaVerified;
        UpdatedByUserId = PortalValidation.Required(updatedByUserId, nameof(updatedByUserId), 200);
        UpdatedAtUtc = PortalValidation.EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        MarkCreated(UpdatedByUserId, UpdatedAtUtc);
    }

    public Guid UserId { get; private set; }
    public bool MfaRequired { get; private set; }
    public bool MfaVerified { get; private set; }
    public string UpdatedByUserId { get; private set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; private set; }

    public static PortalMfaSetting Create(Guid userId, bool mfaRequired, bool mfaVerified, string updatedByUserId, DateTime updatedAtUtc)
    {
        return new PortalMfaSetting(userId, mfaRequired, mfaVerified, updatedByUserId, updatedAtUtc);
    }
}
