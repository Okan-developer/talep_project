using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using TalepYonetim.Identity.Domain;
using TalepYonetim.Identity.Infrastructure;

namespace TalepYonetim.Identity.Application;

/// <summary>Spec 0002, R-10: the signed-in user's name, roles and permissions, read from the access token.</summary>
public static class CurrentUserQuery
{
    public static CurrentUser From(ClaimsPrincipal principal)
    {
        var roles = principal.FindAll(TokenService.RoleClaim).Select(c => c.Value).Order(StringComparer.Ordinal).ToArray();
        return new CurrentUser(
            Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!),
            principal.FindFirstValue(TokenService.FullNameClaim) ?? string.Empty,
            principal.FindFirstValue(TokenService.EmailClaim) ?? string.Empty,
            roles,
            RolePermissions.For(roles));
    }
}
