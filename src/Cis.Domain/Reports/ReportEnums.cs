namespace Cis.Domain.Reports;

public enum ReportFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4,
    Annually = 5,
    AdHoc = 6
}

public enum ReportScheduleStatus
{
    Active = 1,
    Paused = 2,
    Cancelled = 3
}

public enum ReportRunStatus
{
    Generated = 1,
    Approved = 2,
    Published = 3,
    Rejected = 4
}

public enum ReportOutputFormat
{
    PDF = 1,
    Excel = 2,
    CSV = 3,
    Word = 4,
    HTML = 5
}

public enum ReportDeliveryChannel
{
    SecurePortal = 1,
    Email = 2,
    InternalBundle = 3
}

public enum ReportApprovalDecision
{
    Approved = 1,
    Rejected = 2
}

public enum ReportBundleStatus
{
    Draft = 1,
    Published = 2
}
