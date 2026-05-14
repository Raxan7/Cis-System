using Cis.Application.Common.Security;
using Cis.Application.Common.Interfaces;
using Cis.Domain.Identity;
using Cis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cis.Infrastructure.Identity;

internal sealed class IdentityReferenceDataSeeder
{
    private readonly CisDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IPasswordPolicyValidator _passwordPolicyValidator;
    private readonly BootstrapAdminOptions _bootstrapAdminOptions;

    public IdentityReferenceDataSeeder(
        CisDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IPasswordPolicyValidator passwordPolicyValidator,
        IOptions<BootstrapAdminOptions> bootstrapAdminOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _passwordPolicyValidator = passwordPolicyValidator;
        _bootstrapAdminOptions = bootstrapAdminOptions.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedRolePermissionsAsync(cancellationToken);
        await SeedBootstrapAdminAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        foreach (var permissionCode in Permissions.AllKnown)
        {
            if (await _dbContext.Permissions.AnyAsync(permission => permission.Code == permissionCode, cancellationToken))
            {
                continue;
            }

            _dbContext.Permissions.Add(Permission.Create(
                StableId("permission", permissionCode),
                permissionCode,
                ModuleFromPermission(permissionCode),
                DescriptionFromPermission(permissionCode)));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in RoleNames.All)
        {
            var normalizedName = roleName.ToUpperInvariant();
            if (await _dbContext.Roles.AnyAsync(role => role.NormalizedName == normalizedName, cancellationToken))
            {
                continue;
            }

            _dbContext.Roles.Add(Role.Create(
                StableId("role", roleName),
                roleName,
                $"{roleName} role",
                IsPrivilegedRole(roleName)));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolePermissionsAsync(CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles.ToListAsync(cancellationToken);
        var permissions = await _dbContext.Permissions.ToDictionaryAsync(permission => permission.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var existingMappings = await _dbContext.RolePermissions
            .Select(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId })
            .ToListAsync(cancellationToken);
        var existing = existingMappings
            .Select(mapping => (mapping.RoleId, mapping.PermissionId))
            .ToHashSet();

        foreach (var role in roles)
        {
            if (!RolePermissionCatalog.RolePermissions.TryGetValue(role.Name, out var permissionCodes))
            {
                continue;
            }

            foreach (var permissionCode in permissionCodes)
            {
                var permissionId = permissions[permissionCode].Id;
                if (existing.Contains((role.Id, permissionId)))
                {
                    continue;
                }

                _dbContext.RolePermissions.Add(RolePermission.Create(role.Id, permissionId));
                existing.Add((role.Id, permissionId));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedBootstrapAdminAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Email)
            || string.IsNullOrWhiteSpace(_bootstrapAdminOptions.Password)
            || string.IsNullOrWhiteSpace(_bootstrapAdminOptions.DisplayName))
        {
            return;
        }

        var normalizedEmail = _bootstrapAdminOptions.Email.Trim().ToUpperInvariant();
        if (await _dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return;
        }

        _passwordPolicyValidator.Validate(_bootstrapAdminOptions.Password, _bootstrapAdminOptions.Email);
        var now = DateTime.UtcNow;
        var user = User.Create(_bootstrapAdminOptions.Email, _bootstrapAdminOptions.DisplayName, "pending", now);
        user.SetPasswordHash(_passwordHasher.HashPassword(user, _bootstrapAdminOptions.Password), requirePasswordChange: false);

        var systemAdminRole = await _dbContext.Roles.SingleAsync(role => role.Name == RoleNames.SystemAdmin, cancellationToken);
        user.AssignRole(systemAdminRole.Id, "bootstrap", "bootstrap", now);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsPrivilegedRole(string roleName)
    {
        return roleName is RoleNames.SystemAdmin
            or RoleNames.SchemeAdministrator
            or RoleNames.ComplianceRiskOfficer
            or RoleNames.InternalAuditor
            or RoleNames.ExternalAuditor
            or RoleNames.ExecutiveManagement
            or RoleNames.BoardUser
            or RoleNames.RegulatorReadOnlyUser
            or RoleNames.TrusteeReadOnlyUser;
    }

    private static string ModuleFromPermission(string permissionCode)
    {
        return permissionCode == Permissions.All ? "Platform" : permissionCode.Split('.')[0];
    }

    private static string DescriptionFromPermission(string permissionCode)
    {
        return permissionCode == Permissions.All ? "All platform permissions" : $"Allows {permissionCode}";
    }

    private static Guid StableId(string prefix, string value)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"{prefix}:{value}"));
        return new Guid(bytes);
    }
}
