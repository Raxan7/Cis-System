using Cis.Domain.Common;

namespace Cis.Domain.Identity;

public sealed class RolePermission : Entity
{
    private RolePermission()
    {
    }

    private RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId == Guid.Empty ? throw new ArgumentException("Role id cannot be empty.", nameof(roleId)) : roleId;
        PermissionId = permissionId == Guid.Empty ? throw new ArgumentException("Permission id cannot be empty.", nameof(permissionId)) : permissionId;
    }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public Role? Role { get; private set; }

    public Permission? Permission { get; private set; }

    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission(roleId, permissionId);
    }
}
