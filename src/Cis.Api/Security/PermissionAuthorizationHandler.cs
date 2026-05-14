using Cis.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;

namespace Cis.Api.Security;

internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var permissions = context.User.FindAll(PermissionClaimTypes.Permission).Select(claim => claim.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (permissions.Contains(Permissions.All) || permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
