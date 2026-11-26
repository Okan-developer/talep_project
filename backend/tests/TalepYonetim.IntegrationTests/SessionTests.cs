using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace TalepYonetim.IntegrationTests;

/// <summary>Spec 0002, AC-7 / AC-8 / AC-9 (R-6, R-7): session refresh, rotation, reuse, lifetime and logout.</summary>
[Trait("Category", "Integration")]
public sealed class SessionTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private const string Email = "oturum@test.local";
    private const string RefreshPath = "/api/auth/refresh";

    [Fact]
    public async Task Refresh_RotatesTheCookieAndIssuesAWorkingAccessToken()
    {
        await using var host = await StartAsync();
        using var client = host.CreateHttpsClient();
        var (_, first) = await AuthClient.SignInAsync(client, Email);

        using var refreshed = await AuthClient.PostWithCookieAsync(client, RefreshPath, first);

        refreshed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = AuthClient.RefreshCookieOf(refreshed);
        second.ShouldNotBeNullOrEmpty();
        second.ShouldNotBe(first);
        AuthClient.SetCookieHeader(refreshed)!.ToLowerInvariant().ShouldContain("httponly");
        using var me = await AuthClient.GetWithTokenAsync(client, "/api/auth/me", await AuthClient.AccessTokenOf(refreshed));
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_AfterFifteenMinutes_KeepsTheUserWorkingWithoutLogin()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await using var host = await StartAsync(time);
        using var client = host.CreateHttpsClient();
        var (access, refresh) = await AuthClient.SignInAsync(client, Email);

        time.Advance(TimeSpan.FromMinutes(15).Add(TimeSpan.FromSeconds(1)));
        using var expired = await AuthClient.GetWithTokenAsync(client, "/api/auth/me", access);
        using var refreshed = await AuthClient.PostWithCookieAsync(client, RefreshPath, refresh);
        using var me = await AuthClient.GetWithTokenAsync(client, "/api/auth/me", await AuthClient.AccessTokenOf(refreshed));

        expired.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        refreshed.StatusCode.ShouldBe(HttpStatusCode.OK);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_ReusingARotatedToken_IsRejectedAndEndsTheWholeChain()
    {
        await using var host = await StartAsync();
        using var client = host.CreateHttpsClient();
        var (_, first) = await AuthClient.SignInAsync(client, Email);
        using var rotated = await AuthClient.PostWithCookieAsync(client, RefreshPath, first);
        var second = AuthClient.RefreshCookieOf(rotated)!;

        using var reuse = await AuthClient.PostWithCookieAsync(client, RefreshPath, first);
        using var newest = await AuthClient.PostWithCookieAsync(client, RefreshPath, second);

        await AssertSessionExpiredAsync(reuse);
        await AssertSessionExpiredAsync(newest);
    }

    [Fact]
    public async Task Refresh_ReuseEndsOnlyThatChain_NotTheUsersOtherSession()
    {
        await using var host = await StartAsync();
        using var client = host.CreateHttpsClient();
        var (_, deviceA) = await AuthClient.SignInAsync(client, Email);
        var (_, deviceB) = await AuthClient.SignInAsync(client, Email);
        (await AuthClient.PostWithCookieAsync(client, RefreshPath, deviceA)).Dispose();

        (await AuthClient.PostWithCookieAsync(client, RefreshPath, deviceA)).Dispose(); // reuse on A
        using var other = await AuthClient.PostWithCookieAsync(client, RefreshPath, deviceB);

        other.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_OfATokenOlderThanSevenDays_IsRejected()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await using var host = await StartAsync(time);
        using var client = host.CreateHttpsClient();
        var (_, refresh) = await AuthClient.SignInAsync(client, Email);

        time.Advance(TimeSpan.FromDays(7));
        using var response = await AuthClient.PostWithCookieAsync(client, RefreshPath, refresh);

        await AssertSessionExpiredAsync(response);
    }

    [Fact]
    public async Task Refresh_WithoutOrWithAnUnknownCookie_IsRejected()
    {
        await using var host = await StartAsync();
        using var client = host.CreateHttpsClient();

        using var none = await AuthClient.PostWithCookieAsync(client, RefreshPath, null);
        using var unknown = await AuthClient.PostWithCookieAsync(client, RefreshPath, "bilinmeyen-deger");

        await AssertSessionExpiredAsync(none);
        await AssertSessionExpiredAsync(unknown);
    }

    [Fact]
    public async Task Logout_EndsTheSessionAndDeletesTheCookie()
    {
        await using var host = await StartAsync();
        using var client = host.CreateHttpsClient();
        var (_, refresh) = await AuthClient.SignInAsync(client, Email);

        using var logout = await AuthClient.PostWithCookieAsync(client, "/api/auth/logout", refresh);
        using var afterwards = await AuthClient.PostWithCookieAsync(client, RefreshPath, refresh);

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        AuthClient.RefreshCookieOf(logout).ShouldBe(string.Empty);
        await AssertSessionExpiredAsync(afterwards);
    }

    [Fact]
    public async Task Logout_WithoutACookie_StillSucceeds()
    {
        await using var host = await StartAsync();
        using var client = host.CreateHttpsClient();

        using var logout = await AuthClient.PostWithCookieAsync(client, "/api/auth/logout", null);

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task<AuthTestHost> StartAsync(FakeTimeProvider? time = null)
    {
        var host = new AuthTestHost(await postgres.CreateDatabaseAsync(), time: time);
        await host.SeedUserAsync(Email, "SoftwareStaff");
        return host;
    }

    private static async Task AssertSessionExpiredAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        body.RootElement.GetProperty("code").GetString().ShouldBe("session_expired");
        body.RootElement.GetProperty("detail").GetString().ShouldBe("Oturumunuz sona erdi. Lütfen tekrar giriş yapın.");
    }
}
