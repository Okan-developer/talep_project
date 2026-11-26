using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace TalepYonetim.Identity.Contracts;

/// <summary>An endpoint's need for one permission (docs/security.md, AuthZ).</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public static class EndpointAuthorizationExtensions
{
    /// <summary>
    /// Requires a signed-in user holding <paramref name="permission"/> through one of their roles.
    /// Endpoints ask for permissions, never for role names (docs/security.md, AuthZ).
    /// </summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission)));
}
