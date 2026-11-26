using System.Net;
using System.Text.Json;
using Shouldly;

namespace TalepYonetim.IntegrationTests;

/// <summary>Spec 0002, AC-2 / AC-3 (R-3): e-mail + password login.</summary>
[Trait("Category", "Integration")]
public sealed class LoginTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private const string Email = "talep.sahibi@test.local";

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsUserAccessTokenAndASecureRefreshCookie()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        await host.SeedUserAsync(Email, "DepartmentManager");
        using var client = host.CreateHttpsClient();
        var before = DateTimeOffset.UtcNow;

        using var response = await AuthClient.LoginAsync(client, Email, AuthTestHost.Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        body.RootElement.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        var expiresAt = body.RootElement.GetProperty("expiresAt").GetDateTimeOffset();
        expiresAt.ShouldBeInRange(before.AddMinutes(15).AddSeconds(-5), DateTimeOffset.UtcNow.AddMinutes(15).AddSeconds(5));
        var user = body.RootElement.GetProperty("user");
        user.GetProperty("fullName").GetString().ShouldBe("Test " + Email);
        user.GetProperty("email").GetString().ShouldBe(Email);
        user.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ShouldBe(["DepartmentManager"]);
        user.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ShouldBe(["approvals.view"]);
        body.RootElement.TryGetProperty("refreshToken", out _).ShouldBeFalse("the refresh token travels only in the cookie");

        var cookie = AuthClient.SetCookieHeader(response).ShouldNotBeNull().ToLowerInvariant();
        cookie.ShouldContain("httponly");
        cookie.ShouldContain("secure");
        cookie.ShouldContain("samesite=strict");
        cookie.ShouldContain("path=/api/auth");
        cookie.ShouldContain("expires=");
    }

    [Fact]
    public async Task Login_AccessToken_IdentifiesTheUserOnTheMeEndpoint()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        await host.SeedUserAsync(Email, "Admin");
        using var client = host.CreateHttpsClient();
        var (accessToken, _) = await AuthClient.SignInAsync(client, Email);

        using var me = await AuthClient.GetWithTokenAsync(client, "/api/auth/me", accessToken);

        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await me.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        body.RootElement.GetProperty("fullName").GetString().ShouldBe("Test " + Email);
        body.RootElement.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ShouldBe(["Admin"]);
        body.RootElement.GetProperty("permissions").GetArrayLength().ShouldBe(8);
    }

    [Theory]
    [InlineData("TALEP.SAHIBI@TEST.LOCAL")]
    [InlineData("  Talep.Sahibi@Test.Local  ")]
    public async Task Login_EmailCaseAndSurroundingSpaces_AreIgnored(string typed)
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        await host.SeedUserAsync(Email, "Requester");
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.LoginAsync(client, typed, AuthTestHost.Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_GiveTheSameTurkishAnswer()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        await host.SeedUserAsync(Email, "Requester");
        using var client = host.CreateHttpsClient();

        using var wrongPassword = await AuthClient.LoginAsync(client, Email, "Yanlis.Parola1!");
        using var unknownEmail = await AuthClient.LoginAsync(client, "kimse@test.local", "Yanlis.Parola1!");

        var a = await Describe(wrongPassword);
        var b = await Describe(unknownEmail);
        a.ShouldBe(b);
        a.ShouldBe((HttpStatusCode.Unauthorized, "Giriş başarısız", "E-posta ya da parola hatalı.", "invalid_credentials", false));
    }

    [Fact]
    public async Task Login_WithBlankFields_ReturnsTurkishFieldErrors()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());
        using var client = host.CreateHttpsClient();

        using var response = await AuthClient.LoginAsync(client, "   ", "");

        var raw = await ProblemDetailsLocalizationTests.AssertTurkishProblemAsync(
            response, HttpStatusCode.BadRequest, "Geçersiz istek", "İstekteki bilgiler doğrulanamadı.");
        using var body = JsonDocument.Parse(raw);
        var errors = body.RootElement.GetProperty("errors");
        errors.GetProperty("email")[0].GetString().ShouldBe("Bu alan zorunludur.");
        errors.GetProperty("password")[0].GetString().ShouldBe("Bu alan zorunludur.");
    }

    private static async Task<(HttpStatusCode, string?, string?, string?, bool)> Describe(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = body.RootElement;
        return (response.StatusCode, root.GetProperty("title").GetString(), root.GetProperty("detail").GetString(),
            root.GetProperty("code").GetString(), AuthClient.SetCookieHeader(response) is not null);
    }
}
