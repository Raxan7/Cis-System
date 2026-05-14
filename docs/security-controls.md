# Security Controls

This document records the platform security controls implemented for the CIS Management System backend and maps them to the current SRS non-functional requirements.

| SRS Requirement ID | Control Area | Implemented Controls | Evidence |
| --- | --- | --- | --- |
| NF-001 | Authentication, authorization, least privilege | JWT bearer authentication with issuer/audience/signing-key/lifetime validation; fallback authenticated policy; permission-based endpoint authorization; role/permission mapping; portal object-level authorization; CORS allow-list; HTTPS redirection and HSTS outside local test execution; public endpoint rate limiting for login/refresh/health | `src/Cis.Api/Program.cs`, `src/Cis.Api/Security/RequirePermissionAttribute.cs`, `src/Cis.Api/Security/PermissionAuthorizationHandler.cs`, `src/Cis.Infrastructure/Portal/PortalService.cs`, `tests/Cis.Tests.Integration/Security/SecurityHardeningTests.cs`, `tests/Cis.Tests.Integration/Identity/IdentityAccessControlTests.cs` |
| NF-002 | Security audit logging | Audit logs for login, failed login, logout, refresh-token rotation, user creation, access change request, role assignment, privilege escalation, MFA changes, password reset, authentication challenges, and authorization denials; correlation id and IP capture; no request-body/token logging in the API pipeline | `src/Cis.Infrastructure/Identity/IdentityService.cs`, `src/Cis.Api/Security/HttpSecurityAuditExtensions.cs`, `src/Cis.Infrastructure/Audit/AuditLogWriter.cs`, `tests/Cis.Tests.Integration/Security/SecurityHardeningTests.cs`, `tests/Cis.Tests.Integration/Identity/IdentityAccessControlTests.cs` |
| NF-004 | Segregation of duties | Same-user maker/checker and requester/approver blocking for access change workflows and other controlled workflows; privileged MFA enforcement can be required for privileged roles; portal MFA enforcement for investor-facing access | `src/Cis.Application/Identity/SegregationOfDutiesService.cs`, `src/Cis.Infrastructure/Identity/IdentityService.cs`, `src/Cis.Infrastructure/Portal/PortalService.cs`, `tests/Cis.Tests.Integration/Security/SecurityHardeningTests.cs`, `tests/Cis.Tests.Unit/Identity/SegregationOfDutiesServiceTests.cs` |
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
