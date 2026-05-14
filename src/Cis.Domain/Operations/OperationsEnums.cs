namespace Cis.Domain.Operations;

public enum OperationalEnvironment
{
    Development = 1,
    Test = 2,
    UAT = 3,
    Production = 4
}

public enum OperationalRecordStatus
{
    Scheduled = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

public enum BackupType
{
    Logical = 1,
    Physical = 2,
    Snapshot = 3
}
