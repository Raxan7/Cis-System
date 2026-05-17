namespace Cis.Domain.Portal;

public enum PortalProfileStatus
{
    Active = 1,
    Suspended = 2
}

public enum PortalSelfRegistrationStatus
{
    PendingVerification = 1,
    Activated = 2,
    Expired = 3
}

public enum PortalSessionStatus
{
    Active = 1,
    Ended = 2
}

public enum DigitalServiceRequestType
{
    SubscriptionRequest = 1,
    RedemptionRequest = 2,
    SwitchRequest = 3,
    TransferRequest = 4,
    ProfileUpdateRequest = 5,
    DocumentUploadRequest = 6
}

public enum DigitalServiceRequestStatus
{
    Submitted = 1,
    WorkflowPending = 2,
    Approved = 3,
    Rejected = 4
}

public enum PortalActivityType
{
    ViewedProfile = 1,
    ViewedHoldings = 2,
    ViewedTransactions = 3,
    DownloadedStatement = 4,
    DownloadedTaxCertificate = 5,
    ViewedNotices = 6,
    SubmittedDigitalRequest = 7,
    UploadedDocument = 8,
    ViewedFundNav = 9,
    ViewedPortfolio = 10,
    ViewedKycProfile = 11
}

public enum InvestorNoticeStatus
{
    Published = 1,
    Archived = 2
}
