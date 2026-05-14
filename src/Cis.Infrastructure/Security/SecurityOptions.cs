using Cis.Application.Common.Security;

namespace Cis.Infrastructure.Security;

public sealed class ApiCorsOptions
{
    public const string SectionName = "Security:Cors";

    public string[] AllowedOrigins { get; init; } =
    [
        "http://localhost:3000",
        "http://localhost:4200",
        "http://localhost:5173",
        "http://127.0.0.1:3000",
        "http://127.0.0.1:4200",
        "http://127.0.0.1:5173"
    ];
}

public sealed class SecurityRateLimitingOptions
{
    public const string SectionName = "Security:RateLimiting";

    public int GlobalPermitLimit { get; init; } = 120;

    public int GlobalWindowSeconds { get; init; } = 60;

    public int PublicAuthPermitLimit { get; init; } = 5;

    public int PublicAuthWindowSeconds { get; init; } = 60;

    public int PublicEndpointPermitLimit { get; init; } = 30;

    public int PublicEndpointWindowSeconds { get; init; } = 60;
}

public sealed class FileUploadSecurityOptions
{
    public const string SectionName = "Security:FileUploads";

    public long MaxSizeBytes { get; init; } = 10 * 1024 * 1024;

    public string[] AllowedExtensions { get; init; } =
    [
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png",
        ".doc",
        ".docx"
    ];

    public string[] AllowedContentTypes { get; init; } =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    public string[] BlockedExtensions { get; init; } =
    [
        ".exe",
        ".dll",
        ".bat",
        ".cmd",
        ".js",
        ".ps1",
        ".sh",
        ".jar",
        ".vbs",
        ".msi",
        ".scr",
        ".com"
    ];
}

public sealed class MfaEnforcementOptions
{
    public const string SectionName = "Security:Mfa";

    public bool RequireForPrivilegedRoles { get; init; } = false;

    public string[] PrivilegedRoles { get; init; } =
    [
        RoleNames.SystemAdmin,
        RoleNames.SchemeAdministrator,
        RoleNames.FundOperationsOfficer,
        RoleNames.TreasuryInvestmentOfficer,
        RoleNames.ComplianceRiskOfficer,
        RoleNames.FundAccountantFinanceOfficer,
        RoleNames.CustodyReconciliationOfficer,
        RoleNames.InternalAuditor,
        RoleNames.ExternalAuditor,
        RoleNames.ExecutiveManagement,
        RoleNames.BoardUser,
        RoleNames.RegulatorReadOnlyUser,
        RoleNames.TrusteeReadOnlyUser
    ];
}
