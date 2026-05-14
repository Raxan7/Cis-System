using Cis.Domain.Common;

namespace Cis.Domain.Identity;

public sealed class AccessChangeRequestRole : Entity
{
    private AccessChangeRequestRole()
    {
    }

    private AccessChangeRequestRole(Guid accessChangeRequestId, Guid roleId)
    {
        AccessChangeRequestId = accessChangeRequestId == Guid.Empty
            ? throw new ArgumentException("Access change request id cannot be empty.", nameof(accessChangeRequestId))
            : accessChangeRequestId;
        RoleId = roleId == Guid.Empty ? throw new ArgumentException("Role id cannot be empty.", nameof(roleId)) : roleId;
    }

    public Guid AccessChangeRequestId { get; private set; }

    public Guid RoleId { get; private set; }

    public AccessChangeRequest? AccessChangeRequest { get; private set; }

    public Role? Role { get; private set; }

    public static AccessChangeRequestRole Create(Guid accessChangeRequestId, Guid roleId)
    {
        return new AccessChangeRequestRole(accessChangeRequestId, roleId);
    }
}
