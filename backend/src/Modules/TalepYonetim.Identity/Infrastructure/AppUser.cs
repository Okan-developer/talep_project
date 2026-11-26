using Microsoft.AspNetCore.Identity;

namespace TalepYonetim.Identity.Infrastructure;

/// <summary>A local user (docs/security.md, AuthN: ASP.NET Core Identity). Spec 0002, R-1.</summary>
public sealed class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }
}

// ASP.NET Core Identity's entity types, sub-classed so every mapped entity lives in this module's
// namespace (docs/architecture.md, Forbidden dependencies 2 — checked by ModuleBoundaryTests).
public sealed class AppRole : IdentityRole<Guid>;

public sealed class AppUserRole : IdentityUserRole<Guid>;

public sealed class AppUserClaim : IdentityUserClaim<Guid>;

public sealed class AppUserLogin : IdentityUserLogin<Guid>;

public sealed class AppRoleClaim : IdentityRoleClaim<Guid>;

public sealed class AppUserToken : IdentityUserToken<Guid>;

public sealed class AppUserPasskey : IdentityUserPasskey<Guid>;
