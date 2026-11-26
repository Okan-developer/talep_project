using Microsoft.AspNetCore.Authorization;
using TalepYonetim.Identity.Contracts;
using TalepYonetim.Identity.Domain;

namespace TalepYonetim.Identity.Infrastructure;

/// <summary>Grants a <see cref="PermissionRequirement"/> when one of the user's roles maps to the permission (spec 0002, R-10).</summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var roles = context.User.FindAll(TokenService.RoleClaim).Select(c => c.Value);
        if (RolePermissions.For(roles).Contains(requirement.Permission, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
