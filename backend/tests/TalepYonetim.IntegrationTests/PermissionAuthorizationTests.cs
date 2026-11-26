using System.Net;
using System.Text.Json;
using Shouldly;

namespace TalepYonetim.IntegrationTests;

/// <summary>
/// Spec 0002, AC-13 (R-10): operations are authorised by permission; a user's permissions are the
/// union of their roles'. The permission-guarded endpoint exists only in the test host.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PermissionAuthorizationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task UserWithThePermission_IsAllowed()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.GetWithTokenAsync(client, AuthTestHost.AdminUsersPath, host.CreateAccessToken("Admin"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Requester")]
    [InlineData("DepartmentManager")]
    [InlineData("SoftwareStaff")]
    [InlineData("SystemStaff")]
    [InlineData("ItUnitManager")]
    public async Task SignedInUserWithoutThePermission_GetsTheTurkishForbiddenProblem(string role)
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.GetWithTokenAsync(client, AuthTestHost.AdminUsersPath, host.CreateAccessToken(role));

        await ProblemDetailsLocalizationTests.AssertTurkishProblemAsync(
            response, HttpStatusCode.Forbidden, "Yetki yok", "Bu işlem için yetkiniz yok.");
    }

    [Fact]
    public async Task AnonymousRequest_ToAPermissionGuardedEndpoint_Gets401()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.GetWithTokenAsync(client, AuthTestHost.AdminUsersPath, null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TwoRoleUser_GetsTheUnionOfPermissions()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        await host.SeedUserAsync("iki.rol@test.local", "DepartmentManager", "SoftwareStaff");
        using var client = host.CreateHttpsClient();
        var (accessToken, _) = await AuthClient.SignInAsync(client, "iki.rol@test.local");

        using var me = await AuthClient.GetWithTokenAsync(client, "/api/auth/me", accessToken);

        using var body = JsonDocument.Parse(await me.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        body.RootElement.GetProperty("roles").EnumerateArray().Select(r => r.GetString())
            .ShouldBe(["DepartmentManager", "SoftwareStaff"]);
        body.RootElement.GetProperty("permissions").EnumerateArray().Select(p => p.GetString())
            .ShouldBe(["approvals.view", "assignments.view_own"]);
    }
}
