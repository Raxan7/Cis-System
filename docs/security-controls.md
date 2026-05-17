# Security Controls

This document records the platform security controls implemented for the CIS Management System backend and maps them to the current SRS non-functional requirements.

| SRS Requirement ID | Control Area | Implemented Controls | Evidence |
| --- | --- | --- | --- |
| NF-001 | Authentication, authorization, least privilege | JWT bearer authentication with issuer/audience/signing-key/lifetime validation; fallback authenticated policy; permission-based endpoint authorization; role/permission mapping; portal object-level authorization; CORS allow-list; HTTPS redirection and HSTS outside local test execution; public endpoint rate limiting for login/refresh/health | `src/Cis.Api/Program.cs`, `src/Cis.Api/Security/RequirePermissionAttribute.cs`, `src/Cis.Api/Security/PermissionAuthorizationHandler.cs`, `src/Cis.Infrastructure/Portal/PortalService.cs`, `tests/Cis.Tests.Integration/Security/SecurityHardeningTests.cs`, `tests/Cis.Tests.Integration/Identity/IdentityAccessControlTests.cs` |
| NF-002 | Security audit logging | Audit logs for login, failed login, logout, refresh-token rotation, user creation, access change request, role assignment, privilege escalation, MFA changes, password reset, authentication challenges, and authorization denials; correlation id and IP capture; no request-body/token logging in the API pipeline | `src/Cis.Infrastructure/Identity/IdentityService.cs`, `src/Cis.Api/Security/HttpSecurityAuditExtensions.cs`, `src/Cis.Infrastructure/Audit/AuditLogWriter.cs`, `tests/Cis.Tests.Integration/Security/SecurityHardeningTests.cs`, `tests/Cis.Tests.Integration/Identity/IdentityAccessControlTests.cs` |
| NF-003 | Consistent request validation and ProblemDetails responses | ASP.NET Core model validation and the global exception handler normalize validation, authorization, concurrency, and unexpected failures into ProblemDetails responses with trace identifiers and correlation identifiers | `src/Cis.Api/Program.cs`, `src/Cis.Api/Errors/GlobalExceptionHandler.cs`, `tests/Cis.Tests.Integration/Security/SecurityHardeningTests.cs`, `tests/Cis.Tests.Integration/Schemes/SchemesModuleTests.cs` |
| NF-004 | Segregation of duties | Same-user maker/checker and requester/approver blocking for access change workflows and other controlled workflows; privileged MFA enforcement can be required for privileged roles; portal MFA enforcement for investor-facing access | `src/Cis.Application/Identity/SegregationOfDutiesService.cs`, `src/Cis.Infrastructure/Identity/IdentityService.cs`, `src/Cis.Infrastructure/Portal/PortalService.cs`, `tests/Cis.Tests.Integration/Security/SecurityHardeningTests.cs`, `tests/Cis.Tests.Unit/Identity/SegregationOfDutiesServiceTests.cs` |
| NF-005 | Database integrity and concurrency controls | EF Core configurations and migrations apply foreign keys, unique keys, lookup indexes, and row-version concurrency tokens across regulated aggregates and ingestion records | `src/Cis.Infrastructure/Persistence/Configurations`, `src/Cis.Domain/Common/RowVersion.cs`, `src/Cis.Infrastructure/Persistence/Migrations`, `tests/Cis.Tests.Integration/Performance/PerformanceReadinessTests.cs`, `tests/Cis.Tests.Integration/CustodyReconciliation/CustodyReconciliationModuleTests.cs` |
| NF-006 | UTC and business-date discipline | Business dates are modeled separately from UTC execution timestamps and core workflow/domain services guard against non-UTC timestamps | `src/Cis.Domain/Common/BusinessDate.cs`, `src/Cis.Domain/Workflows/WorkflowInstance.cs`, `src/Cis.Domain/Workflows/WorkflowStep.cs`, `tests/Cis.Tests.Unit/Common/BusinessDateTests.cs`, `tests/Cis.Tests.Unit/Workflow/WorkflowInstanceTests.cs` |
| NF-007 | Idempotency controls for replayable ingestion and adjustment endpoints | Idempotency keys are normalized, matched against stored payloads/source hashes, and conflicting replays are rejected for adjustment and ingestion surfaces | `src/Cis.Contracts/StandardHeaders.cs`, `src/Cis.Infrastructure/UnitRegister/UnitRegisterService.cs`, `src/Cis.Infrastructure/Cash/CashService.cs`, `src/Cis.Infrastructure/CustodyReconciliation/CustodyReconciliationService.cs`, `src/Cis.Infrastructure/Integrations/IntegrationService.cs`, `tests/Cis.Tests.Integration/UnitRegister/UnitRegisterModuleTests.cs`, `tests/Cis.Tests.Integration/CustodyReconciliation/CustodyReconciliationModuleTests.cs`, `tests/Cis.Tests.Integration/Integrations/IntegrationsModuleTests.cs` |
| NF-008 | Performance and read-path control baseline | Shared pagination, DB-side filtering/sorting, stable-reference caching, and index-backed hot paths reduce load on list, report, and import scenarios | `src/Cis.Api/Common/PaginationResponseExtensions.cs`, `src/Cis.Infrastructure/Reports/ReportService.cs`, `src/Cis.Infrastructure/Workflows/ApprovalPolicyService.cs`, `docs/performance-plan.md`, `tests/Cis.Tests.Integration/Performance/PerformanceReadinessTests.cs` |
| NF-009 | Operational resilience and DR controls | Startup configuration validation, health/readiness/liveness/deep health checks, backup/restore scripts, migration runner, metrics, and DR test record persistence are implemented | `src/Cis.Api/Configuration/OperationalReadinessConfigurationValidator.cs`, `src/Cis.Api/Program.cs`, `src/Cis.Infrastructure/Operations`, `docs/operations-runbook.md`, `docs/backup-restore-runbook.md`, `tests/Cis.Tests.Integration/Operations/OperationsReadinessTests.cs` |
| NF-010 | File and document intake validation | Shared file security validation enforces size, extension, MIME, path safety, and malware-scan hooks before KYC, portal, or integration metadata is accepted | `src/Cis.Application/Common/Interfaces/IFileSecurityValidator.cs`, `src/Cis.Infrastructure/Security/FileSecurityValidator.cs`, `src/Cis.Infrastructure/Investors/InvestorService.cs`, `src/Cis.Infrastructure/Portal/PortalService.cs`, `tests/Cis.Tests.Integration/Security/SecurityHardeningTests.cs` |
| NF-011 | Background workload execution control | Heavy reports, imports, reconciliation runs, and NAV work are funneled through a bounded in-process queue with operational visibility, but durable persisted scheduling is still pending | `src/Cis.Infrastructure/Operations/BackgroundJobDispatcher.cs`, `src/Cis.Infrastructure/Reports/ReportService.cs`, `src/Cis.Api/Program.cs`, `docs/performance-plan.md`, `tests/Cis.Tests.Integration/Performance/PerformanceReadinessTests.cs` |
| NF-012 | Append-only audit and immutable security records | Audit logs remain append-only at the application layer; refresh tokens rotate instead of being reused; immutable archive records remain protected from update/delete mutation; security-sensitive entities have supporting indexes for lookup and verification | `src/Cis.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs`, `src/Cis.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`, `src/Cis.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, `tests/Cis.Tests.Integration/Platform/AuditWorkflowArchiveTests.cs` |

## Request Validation and Upload Controls

- Request DTO validation is enforced with ASP.NET Core model validation and `ProblemDetails` responses.
- KYC and portal document metadata uploads are validated through a shared `IFileSecurityValidator`.
- File controls include:
  - maximum size
  - extension allow-list
  - MIME-type allow-list
  - blocked executable/script extension list
  - path traversal and rooted-path rejection for storage references
  - malware-scan interface with a development/testing stub

Relevant implementation:

- `src/Cis.Application/Common/Interfaces/IFileSecurityValidator.cs`
- `src/Cis.Infrastructure/Security/FileSecurityValidator.cs`
- `src/Cis.Infrastructure/Investors/InvestorService.cs`
- `src/Cis.Infrastructure/Portal/PortalService.cs`

## Token and Credential Controls

- Access tokens are short-lived JWTs with issuer, audience, signing-key, and expiration validation.
- Refresh tokens are hashed at rest and rotated on use.
- Password hashing uses ASP.NET Core Identity `PasswordHasher<TUser>`.
- Failed login attempts increment lockout state and block further sign-in until lockout expiry.
- Production configuration validation rejects placeholder or default secrets and requires explicit CORS origins plus privileged-role MFA enforcement.

Relevant implementation:

- `src/Cis.Infrastructure/Identity/JwtTokenService.cs`
- `src/Cis.Infrastructure/Identity/IdentityService.cs`
- `src/Cis.Infrastructure/Identity/PasswordPolicyValidator.cs`
- `src/Cis.Api/Configuration/OperationalReadinessConfigurationValidator.cs`

## Publicly Accessible Endpoints

The only intentionally anonymous API endpoints are:

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `GET /health`
- `GET /health/live`
- `GET /health/ready`
- Swagger endpoints in Development

All other controller endpoints require authentication through the fallback authorization policy and, where applicable, explicit permission policies.
