using Microsoft.AspNetCore.Authorization;

namespace Cis.Api.Security;

internal sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
