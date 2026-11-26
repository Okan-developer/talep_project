using Microsoft.AspNetCore.Identity;
using TalepYonetim.Identity.Domain;

namespace TalepYonetim.Identity.Infrastructure;

/// <summary>The six roles, seeded in every environment with fixed ids so the seed is idempotent.</summary>
internal static class SeedRoles
{
    public static readonly AppRole[] All = Roles.All
        .Select((name, index) => new AppRole
        {
            Id = Guid.Parse($"019ab000-0000-7000-8000-00000000000{index + 1}"),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ConcurrencyStamp = $"seed-role-{index + 1}",
        })
        .ToArray();
}

/// <summary>A demo user of spec 0002, R-2.</summary>
public sealed record DemoUser(string FullName, string Email, Guid DepartmentId, string Role);

/// <summary>
/// The demo users of spec 0002, R-2 — created only in the Development environment, idempotently.
/// The demo password is not a secret and is valid only there (docs/security.md, Secrets).
/// </summary>
public static class DemoUsers
{
    public const string Password = "Demo.2026!";

    private static readonly Guid Accounting = Guid.Parse("019aa000-0000-7000-8000-000000000002");
    private static readonly Guid ItSoftware = Guid.Parse("019aa000-0000-7000-8000-000000000005");
    private static readonly Guid ItSystem = Guid.Parse("019aa000-0000-7000-8000-000000000006");

    public static readonly IReadOnlyList<DemoUser> All =
    [
        new("Talep Sahibi", "talep.sahibi@demo.local", Accounting, Roles.Requester),
        new("Birim Yöneticisi", "birim.yoneticisi@demo.local", Accounting, Roles.DepartmentManager),
        new("BT Yazılım Personeli", "bt.yazilim@demo.local", ItSoftware, Roles.SoftwareStaff),
        new("BT Sistem Personeli", "bt.sistem@demo.local", ItSystem, Roles.SystemStaff),
        new("BT Yazılım Birim Yöneticisi", "bt.yazilim.yonetici@demo.local", ItSoftware, Roles.ItUnitManager),
        new("BT Sistem Birim Yöneticisi", "bt.sistem.yonetici@demo.local", ItSystem, Roles.ItUnitManager),
        new("Sistem Yöneticisi", "admin@demo.local", ItSystem, Roles.Admin),
    ];
}

internal sealed class DemoUserSeeder(UserManager<AppUser> users)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var demo in DemoUsers.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await users.FindByEmailAsync(demo.Email) is not null)
            {
                continue;
            }

            var user = new AppUser
            {
                Id = Guid.CreateVersion7(),
                UserName = demo.Email,
                Email = demo.Email,
                EmailConfirmed = true,
                FullName = demo.FullName,
                DepartmentId = demo.DepartmentId,
            };
            Ensure(await users.CreateAsync(user, DemoUsers.Password));
            Ensure(await users.AddToRoleAsync(user, demo.Role));
        }
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Demo user seed failed: " + string.Join(", ", result.Errors.Select(e => e.Code)));
        }
    }
}
