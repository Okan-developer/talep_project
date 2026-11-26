using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TalepYonetim.Identity.Infrastructure;

namespace TalepYonetim.IntegrationTests;

/// <summary>Spec 0002, AC-1 / R-2: demo users exist in Development only, idempotently.</summary>
[Trait("Category", "Integration")]
public sealed class DemoUserSeedTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static readonly (string Name, string Email, string Department, string Role)[] Expected =
    [
        ("Talep Sahibi", "talep.sahibi@demo.local", "Muhasebe", "Requester"),
        ("Birim Yöneticisi", "birim.yoneticisi@demo.local", "Muhasebe", "DepartmentManager"),
        ("BT Yazılım Personeli", "bt.yazilim@demo.local", "BT Yazılım", "SoftwareStaff"),
        ("BT Sistem Personeli", "bt.sistem@demo.local", "BT Sistem", "SystemStaff"),
        ("BT Yazılım Birim Yöneticisi", "bt.yazilim.yonetici@demo.local", "BT Yazılım", "ItUnitManager"),
        ("BT Sistem Birim Yöneticisi", "bt.sistem.yonetici@demo.local", "BT Sistem", "ItUnitManager"),
        ("Sistem Yöneticisi", "admin@demo.local", "BT Sistem", "Admin"),
    ];

    [Fact]
    public async Task Development_CreatesTheSevenDemoUsersWithDepartmentAndRole()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync(), "Development");
        host.CreateClient().Dispose(); // starts the host: migrate + seed

        var users = await ReadUsersAsync(host);

        users.ShouldBe(Expected, ignoreOrder: true);
    }

    [Fact]
    public async Task Development_SecondStartOnTheSameDatabase_KeepsSevenUsers()
    {
        var database = await postgres.CreateDatabaseAsync();
        await using (var first = new AuthTestHost(database, "Development"))
        {
            first.CreateClient().Dispose();
        }

        await using var second = new AuthTestHost(database, "Development");
        second.CreateClient().Dispose();

        (await ReadUsersAsync(second)).Count.ShouldBe(7);
    }

    [Fact]
    public async Task Production_CreatesNoDemoUserAndHidesTheDemoList()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync(), "Production");
        using var client = host.CreateClient();

        (await ReadUsersAsync(host)).ShouldBeEmpty();
        using var response = await client.GetAsync("/api/auth/demo-users", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<List<(string Name, string Email, string Department, string Role)>> ReadUsersAsync(AuthTestHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var departments = await db.Departments.ToDictionaryAsync(d => d.Id, d => d.Name, TestContext.Current.CancellationToken);
        var result = new List<(string, string, string, string)>();
        foreach (var user in await db.Users.ToListAsync(TestContext.Current.CancellationToken))
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add((user.FullName, user.Email!, departments[user.DepartmentId], string.Join(",", roles)));
        }

        return result;
    }
}
