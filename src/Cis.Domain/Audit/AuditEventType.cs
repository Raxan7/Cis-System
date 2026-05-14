namespace Cis.Domain.Audit;

public enum AuditEventType
{
    Created = 1,
    Updated = 2,
    DeletedSoft = 3,
    Submitted = 4,
    Checked = 5,
    Approved = 6,
    Rejected = 7,
    Reversed = 8,
    Published = 9,
    Exported = 10,
    Login = 11,
    FailedLogin = 12,
    RoleChanged = 13,
    OverrideApplied = 14
}
