using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts.Identity;
using Cis.Domain.Identity;
using Cis.Infrastructure.Persistence;
using Cis.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cis.Infrastructure.Identity;

internal sealed class IdentityService : IIdentityService
{
    private const string ModuleName = "Identity";

    private readonly CisDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IPasswordPolicyValidator _passwordPolicyValidator;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISegregationOfDutiesService _segregationOfDutiesService;
    private readonly PasswordPolicyOptions _passwordPolicyOptions;
    private readonly MfaEnforcementOptions _mfaEnforcementOptions;

    public IdentityService(
        CisDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IPasswordPolicyValidator passwordPolicyValidator,
        IJwtTokenService jwtTokenService,
        IAuditLogWriter auditLogWriter,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserContext currentUserContext,
        ISegregationOfDutiesService segregationOfDutiesService,
        IOptions<PasswordPolicyOptions> passwordPolicyOptions,
        IOptions<MfaEnforcementOptions> mfaEnforcementOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _passwordPolicyValidator = passwordPolicyValidator;
        _jwtTokenService = jwtTokenService;
        _auditLogWriter = auditLogWriter;
        _dateTimeProvider = dateTimeProvider;
        _currentUserContext = currentUserContext;
        _segregationOfDutiesService = segregationOfDutiesService;
        _passwordPolicyOptions = passwordPolicyOptions.Value;
        _mfaEnforcementOptions = mfaEnforcementOptions.Value;
    }

    public async Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var now = _dateTimeProvider.UtcNow;
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            await WriteAuditAsync("FailedLogin", "User", null, request.Email, "Failed login for unknown user.", cancellationToken);
            throw new AuthenticationFailedException();
        }

        if (!CanSignInForLogin(user, now))
        {
            await WriteAuditAsync("FailedLogin", "User", user.Id.ToString(), user.Email, "Failed login for inactive or locked user.", cancellationToken);
            throw new AuthenticationFailedException();
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            await RecordFailedLoginAsync(user, now, cancellationToken);
            await WriteAuditAsync("FailedLogin", "User", user.Id.ToString(), user.Email, "Failed login due to invalid password.", cancellationToken);
            throw new AuthenticationFailedException();
        }

        if (await RequiresPrivilegedMfaAsync(user.Id, cancellationToken))
        {
            var mfaProvided = !string.IsNullOrWhiteSpace(request.MfaCode);
            if (!user.MfaEnabled || !mfaProvided)
            {
                await WriteAuditAsync("FailedLogin", "User", user.Id.ToString(), user.Email, "Failed login because MFA is required for privileged access.", cancellationToken);
                throw new AuthenticationFailedException();
            }
        }

        string? updatedPasswordHash = null;
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            updatedPasswordHash = _passwordHasher.HashPassword(user, request.Password);
        }

        var response = await _jwtTokenService.IssueTokensAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await RecordSuccessfulLoginAsync(user.Id, now, updatedPasswordHash, user.PasswordResetRequired, cancellationToken);
        await WriteAuditAsync("Login", "User", user.Id.ToString(), user.Id.ToString(), "Successful login.", cancellationToken);

        return response;
    }

    public async Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var now = _dateTimeProvider.UtcNow;
        var refreshToken = await _dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (refreshToken?.User is null || !refreshToken.IsActive(now) || !refreshToken.User.CanSignIn(now))
        {
            await WriteAuditAsync("FailedRefreshToken", "RefreshToken", null, null, "Invalid or expired refresh token.", cancellationToken);
            throw new AuthenticationFailedException();
        }

        var response = await _jwtTokenService.IssueTokensAsync(refreshToken.User, cancellationToken);
        refreshToken.Revoke(now, _currentUserContext.IpAddress, _jwtTokenService.HashRefreshToken(response.RefreshToken));
        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("RefreshToken", "User", refreshToken.UserId.ToString(), refreshToken.UserId.ToString(), "Refresh token rotated.", cancellationToken);

        return response;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash = _jwtTokenService.HashRefreshToken(refreshToken);
            var storedToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
            storedToken?.Revoke(_dateTimeProvider.UtcNow, _currentUserContext.IpAddress);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await WriteAuditAsync("Logout", "User", _currentUserContext.UserId, _currentUserContext.UserId, "User logged out.", cancellationToken);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        _passwordPolicyValidator.Validate(request.TemporaryPassword, request.Email);
        var normalizedEmail = NormalizeEmail(request.Email);
        if (await _dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A user with the same email address already exists.");
        }

        var now = _dateTimeProvider.UtcNow;
        var user = User.Create(request.Email, request.DisplayName, "pending", now);
        user.SetPasswordHash(_passwordHasher.HashPassword(user, request.TemporaryPassword), requirePasswordChange: true);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("UserCreated", "User", user.Id.ToString(), null, $"User {user.Email} created.", cancellationToken);

        return await MapUserAsync(user.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var userIds = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        var users = new List<UserDto>();
        foreach (var userId in userIds)
        {
            users.Add(await MapUserAsync(userId, cancellationToken));
        }

        return users;
    }

    public Task<UserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return MapUserAsync(id, cancellationToken);
    }

    public Task<AccessChangeRequestDto> RequestRoleAssignmentAsync(Guid userId, AssignRolesRequest request, CancellationToken cancellationToken = default)
    {
        return CreateAccessChangeRequestAsync(new CreateAccessChangeRequest(
            userId,
            AccessChangeType.RoleAssignment.ToString(),
            request.RoleNames,
            request.Reason), cancellationToken);
    }

    public Task<AccessChangeRequestDto> RequestUserDeactivationAsync(Guid userId, DeactivateUserRequest request, CancellationToken cancellationToken = default)
    {
        return CreateAccessChangeRequestAsync(new CreateAccessChangeRequest(
            userId,
            AccessChangeType.UserDeactivation.ToString(),
            null,
            request.Reason), cancellationToken);
    }

    public async Task<AccessChangeRequestDto> CreateAccessChangeRequestAsync(
        CreateAccessChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var requester = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var targetUser = await GetUserAggregateAsync(request.TargetUserId, cancellationToken);

        if (!Enum.TryParse<AccessChangeType>(request.ChangeType, ignoreCase: true, out var changeType))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["changeType"] = ["Unsupported access change type."]
            });
        }

        AccessChangeRequest accessChangeRequest;
        if (changeType == AccessChangeType.RoleAssignment)
        {
            var roleNames = request.RoleNames ?? [];
            var roles = await GetRolesByNameAsync(roleNames, cancellationToken);
            accessChangeRequest = AccessChangeRequest.CreateRoleAssignment(
                NewRequestNumber(now),
                targetUser.Id,
                roles.Select(role => role.Id),
                requester,
                now,
                request.Reason);
        }
        else if (changeType == AccessChangeType.UserDeactivation)
        {
            accessChangeRequest = AccessChangeRequest.CreateUserDeactivation(NewRequestNumber(now), targetUser.Id, requester, now, request.Reason);
        }
        else
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["changeType"] = ["Unsupported access change type."]
            });
        }

        _dbContext.AccessChangeRequests.Add(accessChangeRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(
            "AccessChangeRequested",
            "AccessChangeRequest",
            accessChangeRequest.Id.ToString(),
            null,
            $"{changeType} requested for {targetUser.Email}.",
            cancellationToken);

        if (changeType == AccessChangeType.RoleAssignment && await RequestContainsPrivilegedRoleAsync(accessChangeRequest.Id, cancellationToken))
        {
            await WriteAuditAsync(
                "PrivilegeEscalationRequested",
                "AccessChangeRequest",
                accessChangeRequest.Id.ToString(),
                null,
                $"Privileged role assignment requested for {targetUser.Email}.",
                cancellationToken);
        }

        return await MapAccessChangeRequestAsync(accessChangeRequest.Id, cancellationToken);
    }

    public async Task<AccessChangeRequestDto> ApproveAccessChangeRequestAsync(
        Guid id,
        AccessChangeDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var approver = CurrentUserIdOrThrow();
        var accessChangeRequest = await GetAccessChangeRequestAggregateAsync(id, cancellationToken);
        _segregationOfDutiesService.EnsureCanDecide("access change", accessChangeRequest.RequestedByUserId, approver);

        var now = _dateTimeProvider.UtcNow;
        accessChangeRequest.Approve(approver, now, request.Reason);

        if (accessChangeRequest.ChangeType == AccessChangeType.RoleAssignment)
        {
            foreach (var requestRole in accessChangeRequest.Roles)
            {
                var exists = await _dbContext.UserRoles.AnyAsync(
                    userRole => userRole.UserId == accessChangeRequest.TargetUserId && userRole.RoleId == requestRole.RoleId,
                    cancellationToken);
                if (!exists)
                {
                    _dbContext.UserRoles.Add(UserRole.Create(
                        accessChangeRequest.TargetUserId,
                        requestRole.RoleId,
                        accessChangeRequest.RequestedByUserId,
                        approver,
                        now));
                }
            }
        }
        else if (accessChangeRequest.ChangeType == AccessChangeType.UserDeactivation)
        {
            var targetUser = await GetUserAggregateAsync(accessChangeRequest.TargetUserId, cancellationToken);
            targetUser.Deactivate(accessChangeRequest.Reason, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(
            "AccessChangeApproved",
            "AccessChangeRequest",
            id.ToString(),
            null,
            $"Access change request {accessChangeRequest.RequestNumber} approved.",
            cancellationToken);

        if (accessChangeRequest.ChangeType == AccessChangeType.RoleAssignment)
        {
            await WriteAuditAsync(
                "RoleAssigned",
                "User",
                accessChangeRequest.TargetUserId.ToString(),
                null,
                $"Approved role assignment request {accessChangeRequest.RequestNumber}.",
                cancellationToken);

            if (await RequestContainsPrivilegedRoleAsync(accessChangeRequest.Id, cancellationToken))
            {
                await WriteAuditAsync(
                    "PrivilegeEscalationApproved",
                    "User",
                    accessChangeRequest.TargetUserId.ToString(),
                    null,
                    $"Approved privileged role assignment request {accessChangeRequest.RequestNumber}.",
                    cancellationToken);
            }
        }

        if (accessChangeRequest.ChangeType == AccessChangeType.UserDeactivation)
        {
            await WriteAuditAsync(
                "UserDeactivated",
                "User",
                accessChangeRequest.TargetUserId.ToString(),
                null,
                $"Approved deactivation request {accessChangeRequest.RequestNumber}.",
                cancellationToken);
        }

        return await MapAccessChangeRequestAsync(id, cancellationToken);
    }

    public async Task<AccessChangeRequestDto> RejectAccessChangeRequestAsync(
        Guid id,
        AccessChangeDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var rejector = CurrentUserIdOrThrow();
        var accessChangeRequest = await GetAccessChangeRequestAggregateAsync(id, cancellationToken);
        _segregationOfDutiesService.EnsureCanDecide("access change", accessChangeRequest.RequestedByUserId, rejector);

        accessChangeRequest.Reject(rejector, _dateTimeProvider.UtcNow, request.Reason);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(
            "AccessChangeRejected",
            "AccessChangeRequest",
            id.ToString(),
            null,
            $"Access change request {accessChangeRequest.RequestNumber} rejected.",
            cancellationToken);

        return await MapAccessChangeRequestAsync(id, cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid userId, PasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        _passwordPolicyValidator.Validate(request.TemporaryPassword);
        var user = await GetUserAggregateAsync(userId, cancellationToken);
        user.SetPasswordHash(_passwordHasher.HashPassword(user, request.TemporaryPassword), request.RequirePasswordChange);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("PasswordReset", "User", user.Id.ToString(), null, $"Password reset for {user.Email}.", cancellationToken);
    }

    public async Task UpdateMfaAsync(Guid userId, MfaChangeRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAggregateAsync(userId, cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        user.SetMfa(request.Enabled, now);
        if (request.Enabled && !string.IsNullOrWhiteSpace(request.Secret))
        {
            user.AddMfaFactor(request.FactorType ?? "Totp", TokenGenerator.Sha256(request.Secret), now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("MfaChanged", "User", user.Id.ToString(), null, $"MFA setting changed for {user.Email}.", cancellationToken);
    }

    private async Task<User> GetUserAggregateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");
    }

    private async Task<AccessChangeRequest> GetAccessChangeRequestAggregateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.AccessChangeRequests
            .Include(request => request.Roles)
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken)
            ?? throw new NotFoundException("Access change request was not found.");
    }

    private async Task<IReadOnlyCollection<Role>> GetRolesByNameAsync(
        IReadOnlyCollection<string> roleNames,
        CancellationToken cancellationToken)
    {
        var requestedNames = roleNames
            .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
            .Select(roleName => roleName.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (requestedNames.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["roleNames"] = ["At least one role name is required."]
            });
        }

        var roles = await _dbContext.Roles
            .Where(role => requestedNames.Contains(role.NormalizedName))
            .ToListAsync(cancellationToken);

        var foundNames = roles.Select(role => role.NormalizedName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = requestedNames.Where(roleName => !foundNames.Contains(roleName)).ToArray();
        if (missing.Length > 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["roleNames"] = [$"Unknown roles: {string.Join(", ", missing)}."]
            });
        }

        return roles;
    }

    private async Task<UserDto> MapUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        var roleNames = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .Select(userRole => userRole.Role!.Name)
            .OrderBy(roleName => roleName)
            .ToListAsync(cancellationToken);

        return new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Status.ToString(),
            user.MfaEnabled,
            user.PasswordResetRequired,
            user.LastLoginAtUtc,
            user.Audit.CreatedAtUtc,
            roleNames);
    }

    private async Task<AccessChangeRequestDto> MapAccessChangeRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        var accessChangeRequest = await _dbContext.AccessChangeRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken)
            ?? throw new NotFoundException("Access change request was not found.");

        var targetUserEmail = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == accessChangeRequest.TargetUserId)
            .Select(user => user.Email)
            .SingleAsync(cancellationToken);

        var roleNames = await _dbContext.AccessChangeRequestRoles
            .AsNoTracking()
            .Where(requestRole => requestRole.AccessChangeRequestId == id)
            .Select(requestRole => requestRole.Role!.Name)
            .OrderBy(roleName => roleName)
            .ToListAsync(cancellationToken);

        return new AccessChangeRequestDto(
            accessChangeRequest.Id,
            accessChangeRequest.RequestNumber,
            accessChangeRequest.TargetUserId,
            targetUserEmail,
            accessChangeRequest.ChangeType.ToString(),
            accessChangeRequest.Status.ToString(),
            roleNames,
            accessChangeRequest.RequestedByUserId,
            accessChangeRequest.RequestedAtUtc,
            accessChangeRequest.Reason,
            accessChangeRequest.DecisionByUserId,
            accessChangeRequest.DecisionAtUtc,
            accessChangeRequest.DecisionReason);
    }

    private async Task<bool> RequestContainsPrivilegedRoleAsync(Guid accessChangeRequestId, CancellationToken cancellationToken)
    {
        return await _dbContext.AccessChangeRequestRoles
            .AsNoTracking()
            .AnyAsync(requestRole => requestRole.AccessChangeRequestId == accessChangeRequestId && requestRole.Role!.IsPrivileged, cancellationToken);
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is required.");
    }

    private static string NormalizeEmail(string email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim().ToUpperInvariant();
    }

    private static string NewRequestNumber(DateTime now)
    {
        return $"ACR-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..36];
    }

    private static bool CanSignInForLogin(User user, DateTime now)
    {
        return (user.Status == UserStatus.Active && (!user.LockoutEndUtc.HasValue || user.LockoutEndUtc <= now))
            || (user.Status == UserStatus.Locked && user.LockoutEndUtc.HasValue && user.LockoutEndUtc <= now);
    }

    private async Task<bool> RequiresPrivilegedMfaAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!_mfaEnforcementOptions.RequireForPrivilegedRoles || _mfaEnforcementOptions.PrivilegedRoles.Length == 0)
        {
            return false;
        }

        var normalizedPrivilegedRoles = _mfaEnforcementOptions.PrivilegedRoles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();

        return await _dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .AnyAsync(userRole => normalizedPrivilegedRoles.Contains(userRole.Role!.NormalizedName), cancellationToken);
    }

    private Task<int> RecordSuccessfulLoginAsync(
        Guid userId,
        DateTime occurredAtUtc,
        string? updatedPasswordHash,
        bool passwordResetRequired,
        CancellationToken cancellationToken)
    {
        var actor = userId.ToString();
        var rowVersion = Guid.NewGuid();
        var activeStatus = UserStatus.Active.ToString();

        if (updatedPasswordHash is null)
        {
            return _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE identity.users
SET failed_login_count = 0,
    lockout_end_utc = NULL,
    last_login_at_utc = {occurredAtUtc},
    status = {activeStatus},
    row_version = {rowVersion},
    last_modified_at_utc = {occurredAtUtc},
    last_modified_by = {actor}
WHERE id = {userId}", cancellationToken);
        }

        return _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE identity.users
SET failed_login_count = 0,
    lockout_end_utc = NULL,
    last_login_at_utc = {occurredAtUtc},
    password_hash = {updatedPasswordHash},
    password_reset_required = {passwordResetRequired},
    status = {activeStatus},
    row_version = {rowVersion},
    last_modified_at_utc = {occurredAtUtc},
    last_modified_by = {actor}
WHERE id = {userId}", cancellationToken);
    }

    private Task<int> RecordFailedLoginAsync(User user, DateTime occurredAtUtc, CancellationToken cancellationToken)
    {
        var failedLoginCount = user.FailedLoginCount + 1;
        var shouldLock = failedLoginCount >= _passwordPolicyOptions.LockoutThreshold;
        DateTime? lockoutEndUtc = shouldLock
            ? occurredAtUtc.AddMinutes(_passwordPolicyOptions.LockoutMinutes)
            : user.LockoutEndUtc;
        var status = (shouldLock ? UserStatus.Locked : user.Status).ToString();
        var rowVersion = Guid.NewGuid();

        return _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE identity.users
SET failed_login_count = {failedLoginCount},
    lockout_end_utc = {lockoutEndUtc},
    status = {status},
    row_version = {rowVersion},
    last_modified_at_utc = {occurredAtUtc},
    last_modified_by = {user.Id.ToString()}
WHERE id = {user.Id}", cancellationToken);
    }

    private Task WriteAuditAsync(
        string action,
        string entityName,
        string? entityId,
        string? actorId,
        string summary,
        CancellationToken cancellationToken)
    {
        return _auditLogWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            action,
            entityName,
            entityId,
            ActorId: actorId ?? _currentUserContext.UserId,
            ActorDisplayName: _currentUserContext.DisplayName,
            CorrelationId: _currentUserContext.CorrelationId,
            IpAddress: _currentUserContext.IpAddress,
            UserAgent: _currentUserContext.UserAgent,
            Summary: summary), cancellationToken);
    }
}
