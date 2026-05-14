using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts.Identity;
using Cis.Domain.Identity;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cis.Infrastructure.Identity;

internal sealed class JwtTokenService : IJwtTokenService
{
    private readonly CisDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly JwtOptions _options;

    public JwtTokenService(
        CisDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserContext currentUserContext,
        IOptions<JwtOptions> options)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _currentUserContext = currentUserContext;
        _options = options.Value;
    }

    public async Task<AuthTokenResponse> IssueTokensAsync(User user, CancellationToken cancellationToken = default)
    {
        var rolesAndPermissions = await GetRoleAndPermissionNamesAsync(user.Id, cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        var accessTokenExpiresAtUtc = now.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new("name", user.DisplayName)
        };

        claims.AddRange(rolesAndPermissions.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(rolesAndPermissions.Permissions.Select(permission => new Claim(PermissionClaimTypes.Permission, permission)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            now,
            accessTokenExpiresAtUtc,
            credentials);

        var refreshToken = TokenGenerator.NewRefreshToken();
        var refreshTokenExpiresAtUtc = now.AddDays(_options.RefreshTokenDays);
        _dbContext.RefreshTokens.Add(RefreshToken.Create(
            user.Id,
            HashRefreshToken(refreshToken),
            refreshTokenExpiresAtUtc,
            now,
            _currentUserContext.IpAddress));

        return new AuthTokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            accessTokenExpiresAtUtc,
            refreshToken,
            refreshTokenExpiresAtUtc,
            new UserSummaryDto(
                user.Id,
                user.Email,
                user.DisplayName,
                user.Status.ToString(),
                user.MfaEnabled,
                rolesAndPermissions.Roles,
                rolesAndPermissions.Permissions));
    }

    public string HashRefreshToken(string refreshToken)
    {
        return TokenGenerator.Sha256(refreshToken);
    }

    private async Task<(IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions)> GetRoleAndPermissionNamesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var roles = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .Select(userRole => userRole.Role!.Name)
            .ToListAsync(cancellationToken);

        var permissions = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .SelectMany(userRole => userRole.Role!.Permissions)
            .Select(rolePermission => rolePermission.Permission!.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        return (roles, permissions);
    }
}
